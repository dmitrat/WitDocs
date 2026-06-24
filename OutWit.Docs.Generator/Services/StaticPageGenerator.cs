using System.Text;
using System.Text.RegularExpressions;
using OutWit.Docs.Generator.Commands;
using OutWit.Docs.Framework.Content;
using OutWit.Docs.Framework.Configuration;
using OutWit.Docs.Framework.Services;

namespace OutWit.Docs.Generator.Services;

/// <summary>
/// Generates static HTML pages from markdown content for SEO/SSG.
/// Pre-rendered HTML is visible to crawlers, Blazor hydrates after loading.
/// </summary>
public partial class StaticPageGenerator
{
    #region Constants

    private const int HOME_RECENT_POSTS = 5;
    private const int CARD_SUMMARY_LENGTH = 200;

    #endregion

    #region Fields

    private readonly GeneratorConfig m_config;
    private readonly SiteConfig? m_siteConfig;
    private readonly string m_siteUrl;
    private readonly string m_siteName;
    private readonly MarkdownService m_markdown;
    private readonly ContentParser m_contentParser = new();
    private string m_templateHtml = "";

    #endregion

    #region Constructors

    public StaticPageGenerator(GeneratorConfig config, SiteConfig? siteConfig, string siteUrl, string siteName)
    {
        m_config = config;
        m_siteConfig = siteConfig;
        m_siteUrl = siteUrl.TrimEnd('/');
        m_siteName = siteName;

        // Reuse the framework's markdown pipeline so SSG HTML matches the live app
        // (auto heading ids for anchors, task lists, emoji, frontmatter handling),
        // honoring the site's raw-HTML policy.
        m_markdown = new MarkdownService(siteConfig?.AllowRawHtml ?? true);
    }

    #endregion

    #region Functions

    /// <summary>
    /// Generate static HTML pages for all content.
    /// </summary>
    public async Task GenerateAsync(ContentIndex contentIndex, CancellationToken cancellationToken = default)
    {
        // Load template HTML
        var templatePath = Path.Combine(m_config.OutputPath, "index.html");
        if (!File.Exists(templatePath))
        {
            Console.WriteLine("  Warning: index.html template not found, skipping static page generation");
            return;
        }

        m_templateHtml = await File.ReadAllTextAsync(templatePath, cancellationToken);

        var stats = new GenerationStats();

        // Detail pages
        await ProcessContentFolderAsync("blog", "blog", contentIndex.Blog, stats, cancellationToken);
        await ProcessContentFolderAsync("projects", "project", contentIndex.Projects, stats, cancellationToken);
        await ProcessContentFolderAsync("articles", "article", contentIndex.Articles, stats, cancellationToken);
        await ProcessContentFolderAsync("docs", "docs", contentIndex.Docs, stats, cancellationToken);

        // Dynamic sections
        foreach (var (sectionName, files) in contentIndex.Sections)
        {
            var section = m_siteConfig?.ContentSections?.FirstOrDefault(s => s.Folder == sectionName);
            var route = string.IsNullOrEmpty(section?.Route) ? sectionName : section!.Route;

            if (section?.LandingPage == true && files.Count > 0)
            {
                // Lead page lives at the short section route itself (/{route}/);
                // the remaining pages keep /{route}/{slug}/.
                await RenderSectionFileAsync(sectionName, files[0], route, stats, cancellationToken);
                foreach (var file in files.Skip(1))
                {
                    var slug = ContentHelpers.GetSlugFromPath(file);
                    await RenderSectionFileAsync(sectionName, file, $"{route}/{slug}", stats, cancellationToken);
                }
            }
            else
            {
                foreach (var file in files)
                {
                    var slug = ContentHelpers.GetSlugFromPath(file);
                    await RenderSectionFileAsync(sectionName, file, $"{route}/{slug}", stats, cancellationToken);
                }
            }

            stats.Sections++;
        }

        // Home page (the root index.html) — most important page for crawlers
        await GenerateHomePageAsync(contentIndex, stats, cancellationToken);

        // List pages for main routes
        await GenerateListPagesAsync(contentIndex, stats, cancellationToken);

        Console.WriteLine($"  Generated {stats.Total} static HTML pages:");
        Console.WriteLine($"    Blog: {stats.Blog}, Projects: {stats.Projects}, Articles: {stats.Articles}, Docs: {stats.Docs}, Sections: {stats.SectionItems}, Pages: {stats.Pages}");
    }

    #endregion

    #region Detail Pages

    private async Task ProcessContentFolderAsync(
        string folderName,
        string routePrefix,
        List<string> files,
        GenerationStats stats,
        CancellationToken cancellationToken)
    {
        var folderPath = Path.Combine(m_config.ContentPath, folderName);
        if (!Directory.Exists(folderPath))
            return;

        foreach (var file in files)
        {
            var filePath = Path.Combine(folderPath, file);
            if (!File.Exists(filePath))
                continue;

            try
            {
                var slug = ContentHelpers.GetSlugFromPath(file);
                var markdown = await File.ReadAllTextAsync(filePath, cancellationToken);
                var (frontmatter, content) = ContentHelpers.ExtractFrontmatter(markdown);

                // Embedded [[Component]]s can't render to static HTML — degrade them
                // (block: keep inner content, self-closing: drop) so crawlers never
                // see raw [[...]] markup.
                content = m_contentParser.StripComponentsForStaticHtml(content);
                var htmlContent = m_markdown.ToHtml(content);
                var pageHtml = GenerateStaticPage(
                    title: frontmatter?.Title ?? slug,
                    description: frontmatter?.Description ?? frontmatter?.Summary ?? "",
                    htmlContent: htmlContent,
                    canonicalUrl: $"{m_siteUrl}/{routePrefix}/{slug}/",
                    ogType: "article",
                    publishDate: frontmatter?.PublishDate,
                    tags: frontmatter?.Tags);

                var outputDir = Path.Combine(m_config.OutputPath, routePrefix, slug);
                Directory.CreateDirectory(outputDir);
                await File.WriteAllTextAsync(Path.Combine(outputDir, "index.html"), pageHtml, cancellationToken);

                IncrementStats(stats, folderName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    Warning: Failed to process {file}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Render a single dynamic-section markdown file to <c>/{urlPath}/index.html</c>.
    /// Used so a landing section can place its lead page at the short route (<c>/{route}/</c>)
    /// while other pages keep <c>/{route}/{slug}/</c>.
    /// </summary>
    private async Task RenderSectionFileAsync(
        string folderName,
        string file,
        string urlPath,
        GenerationStats stats,
        CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(m_config.ContentPath, folderName, file);
        if (!File.Exists(filePath))
            return;

        try
        {
            var slug = ContentHelpers.GetSlugFromPath(file);
            var markdown = await File.ReadAllTextAsync(filePath, cancellationToken);
            var (frontmatter, content) = ContentHelpers.ExtractFrontmatter(markdown);

            content = m_contentParser.StripComponentsForStaticHtml(content);
            var htmlContent = m_markdown.ToHtml(content);
            var pageHtml = GenerateStaticPage(
                title: frontmatter?.Title ?? slug,
                description: frontmatter?.Description ?? frontmatter?.Summary ?? "",
                htmlContent: htmlContent,
                canonicalUrl: $"{m_siteUrl}/{urlPath}/",
                ogType: "article",
                publishDate: frontmatter?.PublishDate,
                tags: frontmatter?.Tags,
                // OG image is named {folder}-{slug} by OgImageGenerator; reference it
                // explicitly so a landing page (/{route}/) gets its own, not the default.
                ogImageUrlOverride: $"{m_siteUrl}/og-images/{folderName}-{slug}.png");

            var outputDir = Path.Combine(m_config.OutputPath, Path.Combine(urlPath.Split('/')));
            Directory.CreateDirectory(outputDir);
            await File.WriteAllTextAsync(Path.Combine(outputDir, "index.html"), pageHtml, cancellationToken);

            stats.SectionItems++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    Warning: Failed to process {file}: {ex.Message}");
        }
    }

    #endregion

    #region Home Page

    private async Task GenerateHomePageAsync(ContentIndex contentIndex, GenerationStats stats, CancellationToken cancellationToken)
    {
        var description = m_siteConfig?.Seo.Description ?? "";

        var body = new StringBuilder();
        body.Append("<div class=\"static-content container\">");
        body.Append($"<header class=\"page-header\"><h1 class=\"page-header__title\">{ContentHelpers.EscapeHtml(m_siteName)}</h1>");
        if (!string.IsNullOrWhiteSpace(description))
            body.Append($"<p class=\"page-header__lead\">{ContentHelpers.EscapeHtml(description)}</p>");
        body.Append("</header>");

        // Projects section (matches the live home page, which lists projects)
        var projects = await BuildCardListAsync("projects", "project", contentIndex.Projects, cancellationToken);
        if (projects.Count > 0)
            body.Append(RenderCardSection("projects", "Projects", projects));

        // Recent blog posts give crawlers fresh internal links
        var blog = await BuildCardListAsync("blog", "blog", contentIndex.Blog, cancellationToken);
        if (blog.Count > 0)
            body.Append(RenderCardSection("blog", "Latest posts", blog.Take(HOME_RECENT_POSTS).ToList()));

        body.Append("</div>");

        var pageHtml = GenerateStaticPage(
            title: "",                       // empty -> page title is the bare site name, no header duplication
            description: description,
            htmlContent: "",
            canonicalUrl: $"{m_siteUrl}/",
            ogType: "website",
            bodyOverride: body.ToString());

        await File.WriteAllTextAsync(Path.Combine(m_config.OutputPath, "index.html"), pageHtml, cancellationToken);
        stats.Pages++;
    }

    #endregion

    #region List Pages

    private async Task GenerateListPagesAsync(ContentIndex contentIndex, GenerationStats stats, CancellationToken cancellationToken)
    {
        await GenerateContentListPageAsync("Blog", "Articles, notes and updates.", "blog", "blog", "blog", contentIndex.Blog, stats, cancellationToken);
        await GenerateContentListPageAsync("Articles", "Long-form articles.", "articles", "articles", "article", contentIndex.Articles, stats, cancellationToken);
        await GenerateContentListPageAsync("Documentation", "Documentation and guides.", "docs", "docs", "docs", contentIndex.Docs, stats, cancellationToken);

        foreach (var (sectionName, files) in contentIndex.Sections)
        {
            // Landing sections render their lead page at the root, so there is no
            // separate listing page for them.
            var section = m_siteConfig?.ContentSections?.FirstOrDefault(s => s.Folder == sectionName);
            if (section?.LandingPage == true)
                continue;

            var title = char.ToUpperInvariant(sectionName[0]) + sectionName[1..];
            await GenerateContentListPageAsync(title, $"{title} content.", sectionName, sectionName, sectionName, files, stats, cancellationToken);
        }

        // Minimal informational pages (forms — no markdown content to pre-render)
        await GenerateSimplePageAsync("Contact", "Get in touch.", "contact", stats, cancellationToken);
        await GenerateSimplePageAsync("Search", "Search across all content.", "search", stats, cancellationToken);
    }

    private async Task GenerateContentListPageAsync(
        string title,
        string description,
        string route,
        string folderName,
        string detailRoutePrefix,
        List<string> files,
        GenerationStats stats,
        CancellationToken cancellationToken)
    {
        if (files.Count == 0)
            return;

        var cards = await BuildCardListAsync(folderName, detailRoutePrefix, files, cancellationToken);
        if (cards.Count == 0)
            return;

        var body = new StringBuilder();
        body.Append("<div class=\"static-content container\">");
        body.Append($"<header class=\"page-header\"><h1 class=\"page-header__title\">{ContentHelpers.EscapeHtml(title)}</h1></header>");
        body.Append(RenderCardSection(route, title, cards));
        body.Append("</div>");

        var pageHtml = GenerateStaticPage(
            title: title,
            description: description,
            htmlContent: "",
            canonicalUrl: $"{m_siteUrl}/{route}/",
            ogType: "website",
            bodyOverride: body.ToString());

        await WritePageAsync(route, pageHtml, cancellationToken);
        stats.Pages++;
    }

    private async Task GenerateSimplePageAsync(
        string title,
        string description,
        string route,
        GenerationStats stats,
        CancellationToken cancellationToken)
    {
        var pageHtml = GenerateStaticPage(
            title: title,
            description: description,
            htmlContent: $"<p>{ContentHelpers.EscapeHtml(description)}</p>",
            canonicalUrl: $"{m_siteUrl}/{route}/",
            ogType: "website");

        await WritePageAsync(route, pageHtml, cancellationToken);
        stats.Pages++;
    }

    private async Task WritePageAsync(string route, string pageHtml, CancellationToken cancellationToken)
    {
        var outputDir = Path.Combine(m_config.OutputPath, route);
        Directory.CreateDirectory(outputDir);
        await File.WriteAllTextAsync(Path.Combine(outputDir, "index.html"), pageHtml, cancellationToken);
    }

    #endregion

    #region Card Helpers

    private async Task<List<CardInfo>> BuildCardListAsync(
        string folderName,
        string detailRoutePrefix,
        List<string> files,
        CancellationToken cancellationToken)
    {
        var result = new List<CardInfo>();
        var folderPath = Path.Combine(m_config.ContentPath, folderName);
        if (!Directory.Exists(folderPath))
            return result;

        foreach (var file in files)
        {
            var filePath = Path.Combine(folderPath, file);
            var slug = ContentHelpers.GetSlugFromPath(file);
            string title = slug;
            string summary = "";

            if (File.Exists(filePath))
            {
                try
                {
                    var markdown = await File.ReadAllTextAsync(filePath, cancellationToken);
                    var (frontmatter, _) = ContentHelpers.ExtractFrontmatter(markdown);
                    title = frontmatter?.Title ?? slug;
                    var rawSummary = frontmatter?.Summary ?? frontmatter?.Description ?? "";
                    // Lists show a short, clean teaser — strip markdown and cap the length
                    // (some content has multi-paragraph / markdown-formatted summaries).
                    summary = ContentHelpers.TruncateText(ContentHelpers.ExtractPlainText(rawSummary), CARD_SUMMARY_LENGTH);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    Warning: Failed to read {file} for list: {ex.Message}");
                }
            }

            // Trailing slash → the final 200 URL (Cloudflare 308-redirects the
            // non-slash form), keeping crawler-followed links consistent with canonical/sitemap.
            result.Add(new CardInfo($"/{detailRoutePrefix}/{slug}/", title, summary));
        }

        return result;
    }

    /// <summary>
    /// Render a list of cards using the SAME markup/classes as the live
    /// <c>ContentCard</c> component (.projects-list / .content-card*), so the
    /// pre-rendered content is styled by the framework CSS and matches the
    /// hydrated UI — avoiding a flash of unstyled content before Blazor renders.
    /// </summary>
    private static string RenderCardSection(string id, string heading, List<CardInfo> cards)
    {
        var sb = new StringBuilder();
        sb.Append($"<section id=\"{ContentHelpers.EscapeHtml(id)}\" class=\"projects-section container\">");
        if (!string.IsNullOrEmpty(heading))
            sb.Append($"<h2 class=\"section-title\">{ContentHelpers.EscapeHtml(heading)}</h2>");
        sb.Append("<div class=\"projects-list\">");
        foreach (var card in cards)
        {
            sb.Append("<article class=\"content-card\">");
            sb.Append($"<h2 class=\"content-card__title\"><a href=\"{ContentHelpers.EscapeHtml(card.Url)}\">{ContentHelpers.EscapeHtml(card.Title)}</a></h2>");
            if (!string.IsNullOrWhiteSpace(card.Summary))
                sb.Append($"<div class=\"content-card__description\">{ContentHelpers.EscapeHtml(card.Summary)}</div>");
            sb.Append($"<div class=\"content-card__footer\"><a href=\"{ContentHelpers.EscapeHtml(card.Url)}\" class=\"content-card__more\">More...</a></div>");
            sb.Append("</article>");
        }
        sb.Append("</div></section>");
        return sb.ToString();
    }

    #endregion

    #region Page Generation

    private string GenerateStaticPage(
        string title,
        string description,
        string htmlContent,
        string canonicalUrl,
        string ogType = "website",
        DateTime? publishDate = null,
        List<string>? tags = null,
        string? bodyOverride = null,
        string? ogImageUrlOverride = null)
    {
        var html = m_templateHtml;
        var pageTitle = string.IsNullOrEmpty(title) ? m_siteName : $"{title} - {m_siteName}";
        var metaDescription = EscapeHtmlAttribute(description);

        string fullContent;
        if (bodyOverride != null)
        {
            fullContent = bodyOverride;
        }
        else
        {
            // Build date HTML
            var dateHtml = "";
            if (publishDate.HasValue && publishDate.Value != default)
            {
                var dateFormatted = publishDate.Value.ToString("MMMM d, yyyy");
                var dateIso = publishDate.Value.ToString("yyyy-MM-dd");
                dateHtml = $"<time datetime=\"{dateIso}\">{dateFormatted}</time>";
            }

            // Build tags HTML
            var tagsHtml = "";
            if (tags is { Count: > 0 })
            {
                tagsHtml = "<div class=\"tags\">" +
                    string.Join("", tags.Select(t => $"<span class=\"tag\">{ContentHelpers.EscapeHtml(t)}</span>")) +
                    "</div>";
            }

            // Build header
            var headerHtml = "";
            if (!string.IsNullOrEmpty(title))
            {
                headerHtml = $"""
                    <header class="page-header">
                        <h1 class="page-header__title">{ContentHelpers.EscapeHtml(title)}</h1>
                        {(string.IsNullOrEmpty(dateHtml) ? "" : $"<div class=\"page-header__meta\">{dateHtml}</div>")}
                        {tagsHtml}
                    </header>
                    """;
            }

            fullContent = $"""
                <div class="static-content container">
                    {headerHtml}
                    <article class="prose">
                        {htmlContent}
                    </article>
                </div>
                """;
        }

        // Replace title
        html = TitleRegex().Replace(html, $"<title>{ContentHelpers.EscapeHtml(pageTitle)}</title>");

        // Add/update meta description
        if (MetaDescriptionRegex().IsMatch(html))
        {
            html = MetaDescriptionRegex().Replace(html, $"<meta name=\"description\" content=\"{metaDescription}\" />");
        }
        else
        {
            html = html.Replace("</head>", $"    <meta name=\"description\" content=\"{metaDescription}\" />\n</head>");
        }

        // Build OG image URL (explicit override, else auto-detect from URL pattern).
        // The override is needed for landing pages whose canonical URL (/{route}/)
        // has a single segment and would otherwise fall back to the default image.
        var ogImageUrl = ogImageUrlOverride ?? GetOgImageUrl(canonicalUrl);

        // Get logo URL for og:logo
        var logoUrl = m_siteConfig != null && !string.IsNullOrEmpty(m_siteConfig.LogoDark)
            ? $"{m_siteUrl}{m_siteConfig.LogoDark}"
            : null;

        // Build OG tags
        var ogImageTag = string.IsNullOrEmpty(ogImageUrl) ? "" : $"\n        <meta property=\"og:image\" content=\"{ogImageUrl}\" />";
        var ogLogoTag = string.IsNullOrEmpty(logoUrl) ? "" : $"\n        <meta property=\"og:logo\" content=\"{logoUrl}\" />";
        var ogTags = $"""

                <!-- Open Graph (SSG) -->
                <meta property="og:title" content="{EscapeHtmlAttribute(pageTitle)}" />
                <meta property="og:description" content="{metaDescription}" />
                <meta property="og:type" content="{ogType}" />
                <meta property="og:url" content="{canonicalUrl}" />
                <meta property="og:locale" content="en_US" />{ogImageTag}{ogLogoTag}
                <link rel="canonical" href="{canonicalUrl}" />
            """;

        // Insert OG tags before </head> (only if not already present, to avoid duplicates)
        if (!html.Contains("<!-- Open Graph (SSG) -->"))
            html = html.Replace("</head>", $"{ogTags}\n</head>");

        // Pre-rendered content is VISIBLE by default, so the page is readable without
        // JavaScript and by crawlers straight from the HTML source — no "JavaScript
        // required" dead-end. The inline script below hides it and reveals the loading
        // indicator only when JS is available; Blazor then renders the live UI (same
        // content), so JS users see a spinner → hydrated app with no flash.
        var loadingIndicator = ExtractAppInner(m_templateHtml);
        var appContent =
            "\n                <div class=\"ssg-prerender\">\n" +
            fullContent +
            "\n                </div>\n" +
            "                <div class=\"ssg-loading\" style=\"display:none\">\n" +
            loadingIndicator +
            "\n                </div>\n" +
            "                <script>(function(){var a=document.getElementById('app');if(!a)return;" +
            "var c=a.querySelector('.ssg-prerender');var s=a.querySelector('.ssg-loading');" +
            "if(c)c.style.display='none';if(s)s.style.display='block';})();</script>\n            ";

        html = ReplaceAppContent(html, appContent);

        return html;
    }

    #endregion

    #region Tools

    /// <summary>
    /// Replace the inner content of the &lt;div id="app"&gt; element, correctly
    /// handling nested &lt;div&gt; elements by counting tag depth (a non-greedy
    /// regex would stop at the first nested &lt;/div&gt; and corrupt the markup).
    /// </summary>
    /// <summary>
    /// Return the original inner content of the &lt;div id="app"&gt; element (the
    /// template's loading indicator), matching the close tag by counting nested div
    /// depth. Used to preserve the spinner while injecting hidden SEO content.
    /// </summary>
    private static string ExtractAppInner(string html)
    {
        var open = AppDivOpenRegex().Match(html);
        if (!open.Success)
            return "";

        var innerStart = open.Index + open.Length;
        var cursor = innerStart;
        var depth = 1;

        while (true)
        {
            var tag = DivTagRegex().Match(html, cursor);
            if (!tag.Success)
                return "";

            if (tag.Value.StartsWith("</", StringComparison.OrdinalIgnoreCase))
            {
                depth--;
                if (depth == 0)
                    return html.Substring(innerStart, tag.Index - innerStart);
            }
            else
            {
                depth++;
            }

            cursor = tag.Index + tag.Length;
        }
    }

    private static string ReplaceAppContent(string html, string innerContent)
    {
        var open = AppDivOpenRegex().Match(html);
        if (!open.Success)
            return html;

        var openStart = open.Index;
        var cursor = open.Index + open.Length;
        var depth = 1;

        while (depth > 0)
        {
            var tag = DivTagRegex().Match(html, cursor);
            if (!tag.Success)
                return html; // malformed template — bail without corrupting

            if (tag.Value.StartsWith("</", StringComparison.OrdinalIgnoreCase))
                depth--;
            else
                depth++;

            cursor = tag.Index + tag.Length;
        }

        var rebuilt = $"<div id=\"app\">{innerContent}</div>";
        return string.Concat(html.AsSpan(0, openStart), rebuilt, html.AsSpan(cursor));
    }

    private static void IncrementStats(GenerationStats stats, string folder)
    {
        switch (folder)
        {
            case "blog": stats.Blog++; break;
            case "projects": stats.Projects++; break;
            case "articles": stats.Articles++; break;
            case "docs": stats.Docs++; break;
            default: stats.SectionItems++; break; // Dynamic sections
        }
    }

    /// <summary>
    /// Generate OG image URL based on canonical URL pattern.
    /// </summary>
    private string GetOgImageUrl(string canonicalUrl)
    {
        var urlPath = canonicalUrl.Replace(m_siteUrl, "").Trim('/');

        if (string.IsNullOrEmpty(urlPath))
            return $"{m_siteUrl}/og-images/default.png";

        var segments = urlPath.Split('/');
        if (segments.Length >= 2)
        {
            var contentType = segments[0];
            var slug = segments[1];
            return $"{m_siteUrl}/og-images/{contentType}-{slug}.png";
        }

        return "";
    }

    private static string EscapeHtmlAttribute(string text)
    {
        return ContentHelpers.EscapeHtml(text).Replace("\"", "&quot;");
    }

    #endregion

    #region Regex

    [GeneratedRegex(@"<title>[^<]*</title>")]
    private static partial Regex TitleRegex();

    [GeneratedRegex(@"<meta\s+name=""description""\s+content=""[^""]*""[^>]*>")]
    private static partial Regex MetaDescriptionRegex();

    [GeneratedRegex(@"<div\b[^>]*\bid\s*=\s*[""']app[""'][^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex AppDivOpenRegex();

    [GeneratedRegex(@"</?div\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex DivTagRegex();

    #endregion
}

/// <summary>
/// A single content list entry rendered into list/home pages.
/// </summary>
internal sealed record CardInfo(string Url, string Title, string Summary);

/// <summary>
/// Statistics for generation progress.
/// </summary>
internal sealed class GenerationStats
{
    public int Blog { get; set; }
    public int Projects { get; set; }
    public int Articles { get; set; }
    public int Docs { get; set; }
    public int Sections { get; set; }
    public int SectionItems { get; set; }
    public int Pages { get; set; }

    public int Total => Blog + Projects + Articles + Docs + SectionItems + Pages;
}
