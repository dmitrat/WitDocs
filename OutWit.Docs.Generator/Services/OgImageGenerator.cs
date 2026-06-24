using System.Text.RegularExpressions;
using Microsoft.Playwright;
using OutWit.Docs.Generator.Commands;
using OutWit.Docs.Framework.Content;
using OutWit.Docs.Framework.Configuration;

namespace OutWit.Docs.Generator.Services;

/// <summary>
/// Generates Open Graph images for content pages using Playwright.
/// Creates PNG screenshots of HTML templates for social media sharing.
/// </summary>
public partial class OgImageGenerator : IAsyncDisposable
{
    #region Constants

    private const int OG_IMAGE_WIDTH = 1200;
    private const int OG_IMAGE_HEIGHT = 630;
    private const int MAX_DESCRIPTION_LENGTH = 150;

    // Default colors (fallback if theme.css not found) — dark-theme defaults.
    private const string DEFAULT_ACCENT_COLOR = "#39FF14";
    private const string DEFAULT_BG_COLOR = "#0D1626";
    private const string DEFAULT_TEXT_COLOR = "#F5F7FA";
    private const string DEFAULT_DESC_COLOR = "#A9B4C2";
    private const string DEFAULT_URL_COLOR = "#6B7A8F";

    #endregion

    #region Fields

    private readonly GeneratorConfig m_config;
    private readonly SiteConfig? m_siteConfig;
    private readonly string m_siteUrl;
    private readonly string m_siteName;
    private string m_accentColor = DEFAULT_ACCENT_COLOR;
    private string m_bgColor = DEFAULT_BG_COLOR;
    private string m_textColor = DEFAULT_TEXT_COLOR;
    private string m_descColor = DEFAULT_DESC_COLOR;
    private string m_urlColor = DEFAULT_URL_COLOR;
    private IPlaywright? m_playwright;
    private IBrowser? m_browser;

    /// <summary>
    /// OG images render in the site's default theme: light → <c>:root</c> colors and
    /// light logo; dark (default) → <c>[data-theme="dark"]</c> colors and dark logo.
    /// </summary>
    private bool IsDarkTheme =>
        !string.Equals(m_siteConfig?.DefaultTheme, "light", StringComparison.OrdinalIgnoreCase);

    #endregion

    #region Constructors

    public OgImageGenerator(GeneratorConfig config, SiteConfig? siteConfig, string siteUrl, string siteName)
    {
        m_config = config;
        m_siteConfig = siteConfig;
        m_siteUrl = siteUrl.TrimEnd('/');
        m_siteName = siteName;
    }

    #endregion

    #region Functions

    /// <summary>
    /// Generate OG images for all content pages.
    /// </summary>
    public async Task GenerateAsync(ContentIndex contentIndex, CancellationToken cancellationToken = default)
    {
        var ogImagesDir = Path.Combine(m_config.OutputPath, "og-images");
        Directory.CreateDirectory(ogImagesDir);

        // Read colors from theme.css (like PS version)
        await LoadThemeColorsAsync(cancellationToken);

        try
        {
            await InitializeBrowserAsync();

            var stats = new OgImageStats();

            // Generate default OG image (for home page)
            var homeDescription = m_siteConfig?.Seo?.Description ?? "";
            await GenerateOgImageAsync("default", "", m_siteName, homeDescription, m_siteUrl, ogImagesDir, stats, cancellationToken);

            // Process blog posts
            await ProcessContentAsync("blog", "Blog", contentIndex.Blog, ogImagesDir, stats, cancellationToken);

            // Process projects
            await ProcessContentAsync("projects", "Project", contentIndex.Projects, ogImagesDir, stats, cancellationToken);

            // Process articles
            await ProcessContentAsync("articles", "Article", contentIndex.Articles, ogImagesDir, stats, cancellationToken);

            // Process docs
            await ProcessContentAsync("docs", "Documentation", contentIndex.Docs, ogImagesDir, stats, cancellationToken);

            // Process dynamic sections
            foreach (var (sectionName, files) in contentIndex.Sections)
            {
                // Capitalize section name for label
                var label = char.ToUpper(sectionName[0]) + sectionName[1..];
                await ProcessContentAsync(sectionName, label, files, ogImagesDir, stats, cancellationToken);
            }

            Console.WriteLine($"  Generated {stats.Generated} OG images (skipped {stats.Skipped} existing):");
            Console.WriteLine($"    Blog: {stats.Blog}, Projects: {stats.Projects}, Articles: {stats.Articles}, Docs: {stats.Docs}, Sections: {stats.SectionItems}");
        }
        catch (PlaywrightException ex)
        {
            Console.WriteLine($"  Warning: Playwright initialization failed: {ex.Message}");
            Console.WriteLine($"  Run 'playwright install chromium' to install browser binaries");
        }
    }

    #endregion

    #region Tools

    private async Task LoadThemeColorsAsync(CancellationToken cancellationToken)
    {
        var themeCssPath = Path.Combine(m_config.OutputPath, "css", "theme.css");
        if (!File.Exists(themeCssPath))
        {
            Console.WriteLine($"  Using default colors (theme.css not found)");
            return;
        }

        try
        {
            var css = await File.ReadAllTextAsync(themeCssPath, cancellationToken);

            // Pick the CSS scope matching the site's default theme. Dark → the
            // [data-theme="dark"] block; light → the whole file, whose first matches
            // are the :root (light) defaults.
            var cssToSearch = css;
            var themeLabel = "light :root";
            if (IsDarkTheme)
            {
                var darkThemeMatch = DarkThemeSectionRegex().Match(css);
                if (darkThemeMatch.Success)
                {
                    cssToSearch = darkThemeMatch.Groups[1].Value;
                    themeLabel = "dark";
                }
            }

            Assign(CssVariableRegex().Match(cssToSearch), ref m_accentColor);
            Assign(CssBackgroundVariableRegex().Match(cssToSearch), ref m_bgColor);
            Assign(CssTextHeadingRegex().Match(cssToSearch), ref m_textColor);
            Assign(CssTextBodyRegex().Match(cssToSearch), ref m_descColor);
            Assign(CssTextSecondaryRegex().Match(cssToSearch), ref m_urlColor);

            Console.WriteLine($"  Theme colors ({themeLabel}): accent={m_accentColor}, bg={m_bgColor}, text={m_textColor}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Warning: Failed to read theme.css: {ex.Message}");
        }
    }

    private static void Assign(Match match, ref string target)
    {
        if (match.Success)
            target = match.Groups[1].Value.Trim();
    }

    private async Task InitializeBrowserAsync()
    {
        m_playwright = await Playwright.CreateAsync();
        m_browser = await m_playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    private async Task ProcessContentAsync(
        string contentFolder,
        string contentTypeLabel,
        List<string> files,
        string outputDir,
        OgImageStats stats,
        CancellationToken cancellationToken)
    {
        var folderPath = Path.Combine(m_config.ContentPath, contentFolder);
        if (!Directory.Exists(folderPath))
            return;

        // Route prefix for URL (blog -> /blog, projects -> /project)
        var routePrefix = contentFolder.TrimEnd('s');
        if (routePrefix == "project") routePrefix = "project"; // projects -> project
        if (routePrefix == "doc") routePrefix = "docs"; // docs stays docs

        foreach (var file in files)
        {
            var filePath = Path.Combine(folderPath, file);
            if (!File.Exists(filePath))
                continue;

            try
            {
                var slug = ContentHelpers.GetSlugFromPath(file);
                var imageName = $"{contentFolder}-{slug}";
                var imagePath = Path.Combine(outputDir, $"{imageName}.png");

                // Skip if image exists and not forcing (like PS version -Force flag)
                if (!m_config.ForceOgImages && File.Exists(imagePath))
                {
                    stats.Skipped++;
                    continue;
                }

                var markdown = await File.ReadAllTextAsync(filePath, cancellationToken);
                var (frontmatter, _) = ContentHelpers.ExtractFrontmatter(markdown);

                var title = frontmatter?.Title ?? slug;
                var description = frontmatter?.Summary ?? frontmatter?.Description ?? "";
                var url = $"/{routePrefix}/{slug}";

                await GenerateOgImageAsync(imageName, contentTypeLabel, title, description, url, outputDir, stats, cancellationToken);
                IncrementStats(stats, contentFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    Warning: Failed to generate OG image for {file}: {ex.Message}");
            }
        }
    }

    private async Task GenerateOgImageAsync(
        string imageName,
        string contentType,
        string title,
        string description,
        string url,
        string outputDir,
        OgImageStats stats,
        CancellationToken cancellationToken)
    {
        if (m_browser == null)
            return;

        var imagePath = Path.Combine(outputDir, $"{imageName}.png");
        
        // Skip existing images unless forcing (except default)
        if (!m_config.ForceOgImages && File.Exists(imagePath) && imageName != "default")
        {
            stats.Skipped++;
            return;
        }

        var page = await m_browser.NewPageAsync();
        
        try
        {
            // Set viewport for OG image (1200x630 is the standard Facebook/LinkedIn size)
            await page.SetViewportSizeAsync(OG_IMAGE_WIDTH, OG_IMAGE_HEIGHT);

            // Create HTML template for OG image
            var html = CreateOgImageHtml(contentType, title, description, url);
            await page.SetContentAsync(html);

            // Wait for fonts to load
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Take screenshot
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = imagePath,
                Type = ScreenshotType.Png
            });

            stats.Generated++;
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// Create HTML template for OG image (uses external template file with logo support).
    /// </summary>
    protected internal string CreateOgImageHtml(string contentType, string title, string description, string url)
    {
        // Strip markdown from description and escape HTML
        var safeType = ContentHelpers.EscapeHtml(contentType);
        var safeTitle = ContentHelpers.EscapeHtml(title);
        var safeDescription = ContentHelpers.EscapeHtml(ContentHelpers.TruncateText(description, MAX_DESCRIPTION_LENGTH));
        var safeSiteName = ContentHelpers.EscapeHtml(m_siteName);
        var safeUrl = ContentHelpers.EscapeHtml(url);
        var bgColorDark = DarkenColor(m_bgColor);

        // Try to load external template, fallback to embedded
        var template = LoadTemplate();
        
        // Get logo as base64 data URL if exists
        var logoHtml = GetLogoHtml();

        // Replace placeholders
        var html = template
            .Replace("{{BG_COLOR}}", m_bgColor)
            .Replace("{{BG_COLOR_DARK}}", bgColorDark)
            .Replace("{{ACCENT_COLOR}}", m_accentColor)
            .Replace("{{TEXT_COLOR}}", m_textColor)
            .Replace("{{DESC_COLOR}}", m_descColor)
            .Replace("{{URL_COLOR}}", m_urlColor)
            .Replace("{{CONTENT_TYPE}}", string.IsNullOrEmpty(safeType) ? "" : $"<div class=\"type\">{safeType}</div>")
            .Replace("{{TITLE}}", safeTitle)
            .Replace("{{DESCRIPTION}}", string.IsNullOrEmpty(safeDescription) ? "" : $"<p class=\"description\">{safeDescription}</p>")
            .Replace("{{LOGO}}", logoHtml)
            .Replace("{{SITE_NAME}}", safeSiteName)
            .Replace("{{URL}}", safeUrl);

        return html;
    }

    private string LoadTemplate()
    {
        // Try to load from site's template directory first
        var siteTemplatePath = Path.Combine(m_config.OutputPath, "templates", "og-image.html");
        if (File.Exists(siteTemplatePath))
        {
            return File.ReadAllText(siteTemplatePath);
        }

        // Fallback to embedded template
        return GetEmbeddedTemplate();
    }

    private string GetLogoHtml()
    {
        // Pick the logo variant for the rendered theme: dark OG → light-on-dark
        // artwork (logo-dark); light OG → dark-on-light artwork (logo-light).
        string Img(string name) => Path.Combine(m_config.OutputPath, "images", name);
        var logoPaths = IsDarkTheme
            ? new[] { Img("logo-dark.png"), Img("logo-dark.svg"), Img("logo.png"), Img("logo.svg"), Img("logo-light.png"), Img("logo-light.svg") }
            : new[] { Img("logo-light.png"), Img("logo-light.svg"), Img("logo.png"), Img("logo.svg"), Img("logo-dark.png"), Img("logo-dark.svg") };

        foreach (var logoPath in logoPaths)
        {
            if (File.Exists(logoPath))
            {
                try
                {
                    var bytes = File.ReadAllBytes(logoPath);
                    var base64 = Convert.ToBase64String(bytes);
                    var mimeType = logoPath.EndsWith(".svg") ? "image/svg+xml" : "image/png";
                    return $"<img class=\"logo\" src=\"data:{mimeType};base64,{base64}\" alt=\"Logo\" />";
                }
                catch
                {
                    // Ignore errors, return empty
                }
            }
        }

        return ""; // No logo found
    }

    private static string GetEmbeddedTemplate()
    {
        var assembly = typeof(OgImageGenerator).Assembly;
        var resourceName = "OutWit.Docs.Generator.Templates.og-image.html";
        
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Embedded resource '{resourceName}' not found. Available resources: {string.Join(", ", assembly.GetManifestResourceNames())}");
        }
        
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Darken a hex color by subtracting from RGB values (like PS version).
    /// </summary>
    private static string DarkenColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length < 6) return "#000000";
        
        try
        {
            var r = Math.Max(0, Convert.ToInt32(hex[..2], 16) - 20);
            var g = Math.Max(0, Convert.ToInt32(hex.Substring(2, 2), 16) - 20);
            var b = Math.Max(0, Convert.ToInt32(hex.Substring(4, 2), 16) - 20);
            return $"#{r:X2}{g:X2}{b:X2}";
        }
        catch
        {
            return "#000000";
        }
    }

    private static void IncrementStats(OgImageStats stats, string contentType)
    {
        switch (contentType)
        {
            case "blog": stats.Blog++; break;
            case "projects": stats.Projects++; break;
            case "articles": stats.Articles++; break;
            case "docs": stats.Docs++; break;
            default: stats.SectionItems++; break; // Dynamic sections
        }
    }

    #endregion

    #region Regex

    [GeneratedRegex(@"--color-accent(?:-blue|-green)?:\s*([^;]+);")]
    private static partial Regex CssVariableRegex();

    [GeneratedRegex(@"--color-background:\s*([^;]+);")]
    private static partial Regex CssBackgroundVariableRegex();

    [GeneratedRegex(@"--color-text-heading:\s*([^;]+);")]
    private static partial Regex CssTextHeadingRegex();

    [GeneratedRegex(@"--color-text-body:\s*([^;]+);")]
    private static partial Regex CssTextBodyRegex();

    [GeneratedRegex(@"--color-text-secondary:\s*([^;]+);")]
    private static partial Regex CssTextSecondaryRegex();

    [GeneratedRegex(@"\[data-theme=""dark""\]\s*\{([^}]+)\}", RegexOptions.Singleline)]
    private static partial Regex DarkThemeSectionRegex();

    #endregion

    #region IAsyncDisposable

    public async ValueTask DisposeAsync()
    {
        if (m_browser != null)
        {
            await m_browser.CloseAsync();
            m_browser = null;
        }

        m_playwright?.Dispose();
        m_playwright = null;
    }

    #endregion
}

/// <summary>
/// Stats for OG image generation.
/// </summary>
internal class OgImageStats
{
    public int Blog { get; set; }
    public int Projects { get; set; }
    public int Articles { get; set; }
    public int Docs { get; set; }
    public int SectionItems { get; set; }
    public int Generated { get; set; }
    public int Skipped { get; set; }

    public int Total => Generated + 1; // +1 for default image
}
