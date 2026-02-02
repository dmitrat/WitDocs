using System.Text.Json;
using System.Text.RegularExpressions;
using OutWit.Web.Framework.Content;
using OutWit.Web.Framework.Models;
using OutWit.Web.Generator.Commands;

namespace OutWit.Web.Generator.Services;

/// <summary>
/// Generates content-metadata.json with pre-built metadata for all content.
/// This allows list pages to render without parsing individual markdown files.
/// </summary>
public partial class ContentMetadataGenerator
{
    #region Constants

    private const int WORDS_PER_MINUTE = 200;

    #endregion

    #region Fields

    private readonly GeneratorConfig m_config;

    #endregion

    #region Constructors

    public ContentMetadataGenerator(GeneratorConfig config)
    {
        m_config = config;
    }

    #endregion

    #region Functions

    /// <summary>
    /// Generate content-metadata.json from content index.
    /// </summary>
    public async Task GenerateAsync(ContentIndex contentIndex, CancellationToken cancellationToken = default)
    {
        var metadataIndex = new ContentMetadataIndex();

        // Process blog posts
        metadataIndex.Blog = await ProcessBlogPostsAsync(contentIndex.Blog, cancellationToken);

        // Process projects
        metadataIndex.Projects = await ProcessProjectsAsync(contentIndex.Projects, cancellationToken);

        // Process articles
        metadataIndex.Articles = await ProcessArticlesAsync("articles", contentIndex.Articles, cancellationToken);

        // Process docs
        metadataIndex.Docs = await ProcessDocsAsync(contentIndex.Docs, cancellationToken);

        // Process features
        metadataIndex.Features = await ProcessFeaturesAsync(contentIndex.Features, cancellationToken);

        // Process dynamic sections
        foreach (var (sectionName, files) in contentIndex.Sections)
        {
            var sectionItems = await ProcessArticlesAsync(sectionName, files, cancellationToken);
            if (sectionItems.Count > 0)
            {
                metadataIndex.Sections[sectionName] = sectionItems;
            }
        }

        // Write content-metadata.json
        var outputPath = Path.Combine(m_config.OutputPath, "content-metadata.json");
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(metadataIndex, options);
        await File.WriteAllTextAsync(outputPath, json, cancellationToken);

        var totalItems = metadataIndex.Blog.Count
                         + metadataIndex.Projects.Count
                         + metadataIndex.Articles.Count
                         + metadataIndex.Docs.Count
                         + metadataIndex.Features.Count
                         + metadataIndex.Sections.Values.Sum(s => s.Count);

        Console.WriteLine($"  Created: {outputPath} ({totalItems} items)");
    }

    #endregion

    #region Processing Methods

    private async Task<List<BlogPostMetadata>> ProcessBlogPostsAsync(
        List<string> files,
        CancellationToken cancellationToken)
    {
        var items = new List<BlogPostMetadata>();
        var folderPath = Path.Combine(m_config.ContentPath, "blog");

        if (!Directory.Exists(folderPath))
            return items;

        foreach (var file in files)
        {
            var filePath = Path.Combine(folderPath, file);
            if (!File.Exists(filePath))
                continue;

            try
            {
                var markdown = await File.ReadAllTextAsync(filePath, cancellationToken);
                var (frontmatter, content) = ContentHelpers.ExtractFrontmatter(markdown);

                if (frontmatter == null)
                    continue;

                var slug = ContentHelpers.GetSlugFromPath(file);
                var readingTime = CalculateReadingTime(content);

                items.Add(new BlogPostMetadata
                {
                    Slug = slug,
                    Title = frontmatter.Title ?? slug,
                    Description = frontmatter.Description ?? "",
                    Summary = frontmatter.Summary ?? "",
                    PublishDate = frontmatter.PublishDate,
                    Author = frontmatter.Author ?? "",
                    Tags = frontmatter.Tags ?? [],
                    ReadingTimeMinutes = readingTime,
                    FeaturedImage = frontmatter.FeaturedImage ?? ""
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    Warning: Failed to process blog post {file}: {ex.Message}");
            }
        }

        return items.OrderByDescending(b => b.PublishDate).ToList();
    }

    private async Task<List<ProjectMetadata>> ProcessProjectsAsync(
        List<string> files,
        CancellationToken cancellationToken)
    {
        var items = new List<ProjectMetadata>();
        var folderPath = Path.Combine(m_config.ContentPath, "projects");

        if (!Directory.Exists(folderPath))
            return items;

        foreach (var file in files)
        {
            var filePath = Path.Combine(folderPath, file);
            if (!File.Exists(filePath))
                continue;

            try
            {
                var markdown = await File.ReadAllTextAsync(filePath, cancellationToken);
                var (frontmatter, _) = ContentHelpers.ExtractFrontmatter(markdown);

                if (frontmatter == null)
                    continue;

                var (order, slug) = ContentHelpers.GetOrderAndSlugFromPath(file);

                items.Add(new ProjectMetadata
                {
                    Slug = slug,
                    Title = frontmatter.Title ?? slug,
                    Description = frontmatter.Description ?? "",
                    Summary = frontmatter.Summary ?? "",
                    Order = order,
                    Tags = frontmatter.Tags ?? [],
                    Url = frontmatter.Url ?? ""
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    Warning: Failed to process project {file}: {ex.Message}");
            }
        }

        return items.OrderBy(p => p.Order).ToList();
    }

    private async Task<List<ArticleMetadata>> ProcessArticlesAsync(
        string folderName,
        List<string> files,
        CancellationToken cancellationToken)
    {
        var items = new List<ArticleMetadata>();
        var folderPath = Path.Combine(m_config.ContentPath, folderName);

        if (!Directory.Exists(folderPath))
            return items;

        foreach (var file in files)
        {
            var filePath = Path.Combine(folderPath, file);
            if (!File.Exists(filePath))
                continue;

            try
            {
                var markdown = await File.ReadAllTextAsync(filePath, cancellationToken);
                var (frontmatter, _) = ContentHelpers.ExtractFrontmatter(markdown);

                if (frontmatter == null)
                    continue;

                var (order, slug) = ContentHelpers.GetOrderAndSlugFromPath(file);

                items.Add(new ArticleMetadata
                {
                    Slug = slug,
                    Title = frontmatter.Title ?? slug,
                    Description = frontmatter.Description ?? "",
                    Order = order,
                    Tags = frontmatter.Tags ?? [],
                    PublishDate = frontmatter.PublishDate
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    Warning: Failed to process article {file}: {ex.Message}");
            }
        }

        return items.OrderBy(a => a.Order).ToList();
    }

    private async Task<List<DocMetadata>> ProcessDocsAsync(
        List<string> files,
        CancellationToken cancellationToken)
    {
        var items = new List<DocMetadata>();
        var folderPath = Path.Combine(m_config.ContentPath, "docs");

        if (!Directory.Exists(folderPath))
            return items;

        foreach (var file in files)
        {
            var filePath = Path.Combine(folderPath, file);
            if (!File.Exists(filePath))
                continue;

            try
            {
                var markdown = await File.ReadAllTextAsync(filePath, cancellationToken);
                var (frontmatter, _) = ContentHelpers.ExtractFrontmatter(markdown);

                if (frontmatter == null)
                    continue;

                var (order, slug) = ContentHelpers.GetOrderAndSlugFromPath(file);

                items.Add(new DocMetadata
                {
                    Slug = slug,
                    Title = frontmatter.Title ?? slug,
                    Description = frontmatter.Description ?? "",
                    Order = order,
                    ParentSlug = frontmatter.Parent ?? ""
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    Warning: Failed to process doc {file}: {ex.Message}");
            }
        }

        return items.OrderBy(d => d.Order).ToList();
    }

    private async Task<List<FeatureMetadata>> ProcessFeaturesAsync(
        List<string> files,
        CancellationToken cancellationToken)
    {
        var items = new List<FeatureMetadata>();
        var folderPath = Path.Combine(m_config.ContentPath, "features");

        if (!Directory.Exists(folderPath))
            return items;

        foreach (var file in files)
        {
            var filePath = Path.Combine(folderPath, file);
            if (!File.Exists(filePath))
                continue;

            try
            {
                var markdown = await File.ReadAllTextAsync(filePath, cancellationToken);
                var (frontmatter, _) = ContentHelpers.ExtractFrontmatter(markdown);

                if (frontmatter == null)
                    continue;

                var (order, slug) = ContentHelpers.GetOrderAndSlugFromPath(file);

                items.Add(new FeatureMetadata
                {
                    Slug = slug,
                    Title = frontmatter.Title ?? slug,
                    Description = frontmatter.Description ?? "",
                    Order = order,
                    Icon = frontmatter.Icon ?? "",
                    IconSvg = frontmatter.IconSvg ?? ""
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    Warning: Failed to process feature {file}: {ex.Message}");
            }
        }

        return items.OrderBy(f => f.Order).ToList();
    }

    #endregion

    #region Tools

    private static int CalculateReadingTime(string content)
    {
        // Remove markdown syntax for more accurate word count
        var plainText = MarkdownSyntaxRegex().Replace(content, " ");
        var wordCount = plainText.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Max(1, (int)Math.Ceiling((double)wordCount / WORDS_PER_MINUTE));
    }

    [GeneratedRegex(@"[#*`\[\]()>!_~\-]")]
    private static partial Regex MarkdownSyntaxRegex();

    #endregion
}
