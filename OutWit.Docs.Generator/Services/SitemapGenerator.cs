using System.Text;
using OutWit.Docs.Generator.Commands;
using OutWit.Docs.Framework.Configuration;
using OutWit.Docs.Framework.Content;

namespace OutWit.Docs.Generator.Services;

/// <summary>
/// Generates sitemap.xml and robots.txt files.
/// </summary>
public class SitemapGenerator
{
    #region Fields

    private readonly GeneratorConfig m_config;
    private readonly string m_siteUrl;
    private readonly SiteConfig? m_siteConfig;

    #endregion

    #region Constructors

    public SitemapGenerator(GeneratorConfig config, string siteUrl, SiteConfig? siteConfig = null)
    {
        m_config = config;
        m_siteUrl = siteUrl.TrimEnd('/');
        m_siteConfig = siteConfig;
    }

    #endregion

    #region Functions

    /// <summary>
    /// Generate sitemap.xml and robots.txt.
    /// </summary>
    public async Task GenerateAsync(ContentIndex contentIndex, CancellationToken cancellationToken = default)
    {
        var entries = new List<SitemapEntry>();
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Add static pages
        entries.Add(new SitemapEntry { Url = $"{m_siteUrl}/", LastMod = today, Priority = "1.0" });
        entries.Add(new SitemapEntry { Url = $"{m_siteUrl}/blog/", LastMod = today, Priority = "0.8" });
        entries.Add(new SitemapEntry { Url = $"{m_siteUrl}/contact/", LastMod = today, Priority = "0.5" });
        entries.Add(new SitemapEntry { Url = $"{m_siteUrl}/search/", LastMod = today, Priority = "0.3" });

        // Add blog posts with real file dates
        var blogPath = Path.Combine(m_config.ContentPath, "blog");
        foreach (var file in contentIndex.Blog)
        {
            var slug = ContentHelpers.GetSlugFromPath(file);
            var lastMod = GetFileLastModified(Path.Combine(blogPath, file));
            entries.Add(new SitemapEntry
            {
                Url = $"{m_siteUrl}/blog/{slug}/",
                LastMod = lastMod,
                Priority = "0.6" // Match PS version
            });
        }

        // Add projects with real file dates
        var projectsPath = Path.Combine(m_config.ContentPath, "projects");
        foreach (var file in contentIndex.Projects)
        {
            var slug = ContentHelpers.GetSlugFromPath(file);
            var lastMod = GetFileLastModified(Path.Combine(projectsPath, file));
            entries.Add(new SitemapEntry
            {
                Url = $"{m_siteUrl}/project/{slug}/",
                LastMod = lastMod,
                Priority = "0.7"
            });
        }

        // Add articles with real file dates
        var articlesPath = Path.Combine(m_config.ContentPath, "articles");
        foreach (var file in contentIndex.Articles)
        {
            var slug = ContentHelpers.GetSlugFromPath(file);
            var lastMod = GetFileLastModified(Path.Combine(articlesPath, file));
            entries.Add(new SitemapEntry
            {
                Url = $"{m_siteUrl}/article/{slug}/",
                LastMod = lastMod,
                Priority = "0.6"
            });
        }

        // Add docs with real file dates
        var docsPath = Path.Combine(m_config.ContentPath, "docs");
        foreach (var file in contentIndex.Docs)
        {
            var slug = ContentHelpers.GetSlugFromPath(file);
            var lastMod = GetFileLastModified(Path.Combine(docsPath, file));
            entries.Add(new SitemapEntry
            {
                Url = $"{m_siteUrl}/docs/{slug}/",
                LastMod = lastMod,
                Priority = "0.6"
            });
        }

        // Add dynamic sections
        foreach (var (sectionName, files) in contentIndex.Sections)
        {
            var section = m_siteConfig?.ContentSections?.FirstOrDefault(s => s.Folder == sectionName);
            var route = string.IsNullOrEmpty(section?.Route) ? sectionName : section!.Route;
            var landing = section?.LandingPage == true;
            var sectionPath = Path.Combine(m_config.ContentPath, sectionName);

            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];
                var slug = ContentHelpers.GetSlugFromPath(file);
                var lastMod = GetFileLastModified(Path.Combine(sectionPath, file));

                // Landing section: the lead (first) page is canonical at the short
                // route itself; the rest keep /{route}/{slug}/.
                var url = landing && i == 0
                    ? $"{m_siteUrl}/{route}/"
                    : $"{m_siteUrl}/{route}/{slug}/";

                entries.Add(new SitemapEntry
                {
                    Url = url,
                    LastMod = lastMod,
                    Priority = "0.6"
                });
            }
        }

        // Write sitemap.xml
        var sitemapPath = Path.Combine(m_config.OutputPath, "sitemap.xml");
        await WriteSitemapAsync(sitemapPath, entries, cancellationToken);
        Console.WriteLine($"  Created: {sitemapPath} ({entries.Count} URLs)");

        // Write robots.txt
        var robotsPath = Path.Combine(m_config.OutputPath, "robots.txt");
        await WriteRobotsAsync(robotsPath, cancellationToken);
        Console.WriteLine($"  Created: {robotsPath}");

        // Write the IndexNow key file (opt-in via seo.indexNowKey)
        await WriteIndexNowKeyFileAsync(cancellationToken);
    }

    /// <summary>
    /// Write the IndexNow verification file (<c>{key}.txt</c>) to the site root
    /// when <see cref="SeoConfig.IndexNowKey"/> is configured. The file content is
    /// the key itself, as required by the IndexNow spec. No-op when the key is unset
    /// (feature is opt-in).
    /// </summary>
    private async Task WriteIndexNowKeyFileAsync(CancellationToken cancellationToken)
    {
        var key = m_siteConfig?.Seo?.IndexNowKey?.Trim();
        if (string.IsNullOrEmpty(key))
            return;

        // The key file name and its content must both be the key (IndexNow spec).
        var keyFilePath = Path.Combine(m_config.OutputPath, $"{key}.txt");
        await File.WriteAllTextAsync(keyFilePath, key, cancellationToken);
        Console.WriteLine($"  Created: {keyFilePath} (IndexNow key file)");
    }

    #endregion

    #region Tools

    private static string GetFileLastModified(string filePath)
    {
        if (File.Exists(filePath))
        {
            var lastWrite = File.GetLastWriteTimeUtc(filePath);
            return lastWrite.ToString("yyyy-MM-dd");
        }
        return DateTime.UtcNow.ToString("yyyy-MM-dd");
    }

    private static async Task WriteSitemapAsync(string path, List<SitemapEntry> entries, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        foreach (var entry in entries)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{ContentHelpers.EscapeXml(entry.Url)}</loc>");
            sb.AppendLine($"    <lastmod>{entry.LastMod}</lastmod>");
            sb.AppendLine($"    <priority>{entry.Priority}</priority>");
            sb.AppendLine("  </url>");
        }

        sb.AppendLine("</urlset>");

        await File.WriteAllTextAsync(path, sb.ToString(), cancellationToken);
    }

    private async Task WriteRobotsAsync(string path, CancellationToken cancellationToken)
    {
        var content = $"""
            # robots.txt for {m_siteUrl}
            User-agent: *
            Allow: /

            # Sitemap
            Sitemap: {m_siteUrl}/sitemap.xml
            """;

        await File.WriteAllTextAsync(path, content, cancellationToken);
    }

    #endregion
}

/// <summary>
/// Sitemap entry for XML generation.
/// </summary>
internal class SitemapEntry
{
    public string Url { get; set; } = "";
    public string LastMod { get; set; } = "";
    public string Priority { get; set; } = "0.5";
}
