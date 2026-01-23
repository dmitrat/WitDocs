using System.Text.Json;
using OutWit.Web.Framework.Content;
using OutWit.Web.Framework.Configuration;

namespace OutWit.Web.Generator.Services;

/// <summary>
/// Scans content folders and builds the content index.
/// Supports both hardcoded sections (blog, projects, features) and
/// dynamic sections defined in site.config.json.
/// </summary>
public class ContentScanner
{
    #region Fields

    private readonly string m_contentPath;
    private readonly string m_siteConfigPath;

    #endregion

    #region Constructors

    public ContentScanner(string contentPath, string? siteConfigPath = null)
    {
        m_contentPath = contentPath;
        m_siteConfigPath = siteConfigPath ?? Path.Combine(Path.GetDirectoryName(contentPath) ?? "", "site.config.json");
    }

    #endregion

    #region Functions

    /// <summary>
    /// Scan all content folders and build the index.
    /// </summary>
    public async Task<ContentIndex> ScanAsync(CancellationToken cancellationToken = default)
    {
        var index = new ContentIndex();

        // Scan hardcoded sections
        // All sections support both file-based (post.md) and folder-based (post/index.md) content

        // Blog posts (sorted by name descending for date-based posts)
        var blogPath = Path.Combine(m_contentPath, "blog");
        if (Directory.Exists(blogPath))
        {
            index.Blog = ScanContentFolder(blogPath, sortDescending: true);
        }

        // Projects (sorted by order prefix)
        var projectsPath = Path.Combine(m_contentPath, "projects");
        if (Directory.Exists(projectsPath))
        {
            index.Projects = ScanContentFolder(projectsPath, sortDescending: false);
        }

        // Features (sorted by order prefix, typically flat files)
        var featuresPath = Path.Combine(m_contentPath, "features");
        if (Directory.Exists(featuresPath))
        {
            index.Features = ScanContentFolder(featuresPath, sortDescending: false);
        }

        // Articles (sorted by order prefix)
        var articlesPath = Path.Combine(m_contentPath, "articles");
        if (Directory.Exists(articlesPath))
        {
            index.Articles = ScanContentFolder(articlesPath, sortDescending: false);
        }

        // Docs (sorted by order prefix)
        var docsPath = Path.Combine(m_contentPath, "docs");
        if (Directory.Exists(docsPath))
        {
            index.Docs = ScanContentFolder(docsPath, sortDescending: false);
        }

        // Scan dynamic sections from site.config.json
        var siteConfig = await LoadSiteConfigAsync(cancellationToken);
        if (siteConfig?.ContentSections != null)
        {
            foreach (var section in siteConfig.ContentSections)
            {
                // Skip if folder matches a hardcoded section
                if (IsHardcodedSection(section.Folder))
                    continue;

                var sectionPath = Path.Combine(m_contentPath, section.Folder);
                if (Directory.Exists(sectionPath))
                {
                    index.Sections[section.Folder] = ScanContentFolder(sectionPath, sortDescending: false);
                }
            }
        }

        return index;
    }

    #endregion

    #region Tools

    private static bool IsHardcodedSection(string folder)
    {
        return folder.Equals("blog", StringComparison.OrdinalIgnoreCase)
            || folder.Equals("projects", StringComparison.OrdinalIgnoreCase)
            || folder.Equals("features", StringComparison.OrdinalIgnoreCase)
            || folder.Equals("articles", StringComparison.OrdinalIgnoreCase)
            || folder.Equals("docs", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<SiteConfig?> LoadSiteConfigAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(m_siteConfigPath))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(m_siteConfigPath, cancellationToken);
            return JsonSerializer.Deserialize<SiteConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Scan a content folder supporting both file-based and folder-based content.
    /// File-based: post.md -> "post.md"
    /// Folder-based: post/index.md -> "post/index.md"
    /// </summary>
    private static List<string> ScanContentFolder(string path, bool sortDescending)
    {
        var results = new List<string>();

        var entries = Directory.GetFileSystemEntries(path);
        var sorted = sortDescending
            ? entries.OrderByDescending(p => Path.GetFileName(p))
            : entries.OrderBy(p => Path.GetFileName(p));

        foreach (var item in sorted)
        {
            var name = Path.GetFileName(item);

            if (Directory.Exists(item))
            {
                // Folder-based content: look for index.md or index.mdx
                var indexMd = Path.Combine(item, "index.md");
                var indexMdx = Path.Combine(item, "index.mdx");

                if (File.Exists(indexMd))
                {
                    results.Add($"{name}/index.md");
                }
                else if (File.Exists(indexMdx))
                {
                    results.Add($"{name}/index.mdx");
                }
            }
            else if (item.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
                     item.EndsWith(".mdx", StringComparison.OrdinalIgnoreCase))
            {
                // File-based content
                results.Add(name);
            }
        }

        return results;
    }

    #endregion
}
