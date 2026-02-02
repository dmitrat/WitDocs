using System.Text.Json;
using OutWit.Web.Framework.Content;
using OutWit.Web.Framework.Models;
using OutWit.Web.Generator.Commands;

namespace OutWit.Web.Generator.Services;

/// <summary>
/// Generates navigation-index.json with pre-built menu data.
/// This allows instant menu rendering without parsing markdown files.
/// </summary>
public class NavigationIndexGenerator
{
    #region Fields

    private readonly GeneratorConfig m_config;

    #endregion

    #region Constructors

    public NavigationIndexGenerator(GeneratorConfig config)
    {
        m_config = config;
    }

    #endregion

    #region Functions

    /// <summary>
    /// Generate navigation-index.json from content index.
    /// </summary>
    public async Task GenerateAsync(ContentIndex contentIndex, CancellationToken cancellationToken = default)
    {
        var navigationIndex = new NavigationIndex();

        // Process projects
        navigationIndex.Projects = await ProcessContentFolderAsync(
            "projects", contentIndex.Projects, cancellationToken);

        // Process articles
        navigationIndex.Articles = await ProcessContentFolderAsync(
            "articles", contentIndex.Articles, cancellationToken);

        // Process docs
        navigationIndex.Docs = await ProcessContentFolderAsync(
            "docs", contentIndex.Docs, cancellationToken);

        // Process dynamic sections
        foreach (var (sectionName, files) in contentIndex.Sections)
        {
            var sectionItems = await ProcessContentFolderAsync(
                sectionName, files, cancellationToken);
            
            if (sectionItems.Count > 0)
            {
                navigationIndex.Sections[sectionName] = sectionItems;
            }
        }

        // Write navigation-index.json
        var outputPath = Path.Combine(m_config.OutputPath, "navigation-index.json");
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(navigationIndex, options);
        await File.WriteAllTextAsync(outputPath, json, cancellationToken);

        var totalItems = navigationIndex.Projects.Count 
                         + navigationIndex.Articles.Count 
                         + navigationIndex.Docs.Count 
                         + navigationIndex.Sections.Values.Sum(s => s.Count);
        
        Console.WriteLine($"  Created: {outputPath} ({totalItems} menu items)");
    }

    #endregion

    #region Tools

    private async Task<List<NavigationMenuItem>> ProcessContentFolderAsync(
        string folderName,
        List<string> files,
        CancellationToken cancellationToken)
    {
        var items = new List<NavigationMenuItem>();
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

                items.Add(new NavigationMenuItem
                {
                    Slug = slug,
                    Title = frontmatter.Title ?? slug,
                    MenuTitle = frontmatter.MenuTitle,
                    Order = order,
                    ShowInMenu = frontmatter.ShowInMenu,
                    ShowInHeader = frontmatter.ShowInHeader
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    Warning: Failed to process {file}: {ex.Message}");
            }
        }

        return items.OrderBy(i => i.Order).ToList();
    }

    #endregion
}
