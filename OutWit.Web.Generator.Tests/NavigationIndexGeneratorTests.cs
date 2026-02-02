using System.Text.Json;
using NUnit.Framework;
using OutWit.Web.Framework.Models;
using OutWit.Web.Generator.Commands;
using OutWit.Web.Generator.Services;

namespace OutWit.Web.Generator.Tests;

/// <summary>
/// Tests for NavigationIndexGenerator service.
/// </summary>
[TestFixture]
public class NavigationIndexGeneratorTests
{
    #region Tests

    [Test]
    public async Task GenerateAsyncEmptyContentReturnsEmptyIndexTest()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var config = new GeneratorConfig
        {
            SitePath = tempDir,
            OutputPath = tempDir
        };
        var generator = new NavigationIndexGenerator(config);
        var contentIndex = new Framework.Content.ContentIndex();

        try
        {
            // Act
            await generator.GenerateAsync(contentIndex);

            // Assert
            var outputPath = Path.Combine(tempDir, "navigation-index.json");
            Assert.That(File.Exists(outputPath), Is.True);

            var json = await File.ReadAllTextAsync(outputPath);
            var navIndex = JsonSerializer.Deserialize<NavigationIndex>(json, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.That(navIndex, Is.Not.Null);
            Assert.That(navIndex!.Projects, Is.Empty);
            Assert.That(navIndex.Articles, Is.Empty);
            Assert.That(navIndex.Docs, Is.Empty);
            Assert.That(navIndex.Sections, Is.Empty);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task GenerateAsyncProjectsExtractsMenuDataTest()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var contentDir = Path.Combine(tempDir, "content");
        var projectsDir = Path.Combine(contentDir, "projects");
        Directory.CreateDirectory(projectsDir);

        // Create project with menu-relevant frontmatter
        File.WriteAllText(Path.Combine(projectsDir, "01-first-project.md"), """
            ---
            title: First Project
            menuTitle: First
            showInMenu: true
            showInHeader: true
            ---
            Content here
            """);

        File.WriteAllText(Path.Combine(projectsDir, "02-second-project.md"), """
            ---
            title: Second Project
            showInMenu: false
            ---
            Content here
            """);

        var config = new GeneratorConfig
        {
            SitePath = tempDir,
            OutputPath = tempDir
        };
        var generator = new NavigationIndexGenerator(config);
        var contentIndex = new Framework.Content.ContentIndex
        {
            Projects = ["01-first-project.md", "02-second-project.md"]
        };

        try
        {
            // Act
            await generator.GenerateAsync(contentIndex);

            // Assert
            var outputPath = Path.Combine(tempDir, "navigation-index.json");
            var json = await File.ReadAllTextAsync(outputPath);
            var navIndex = JsonSerializer.Deserialize<NavigationIndex>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.That(navIndex!.Projects, Has.Count.EqualTo(2));
            
            var first = navIndex.Projects.First(p => p.Slug == "first-project");
            Assert.That(first.Title, Is.EqualTo("First Project"));
            Assert.That(first.MenuTitle, Is.EqualTo("First"));
            Assert.That(first.ShowInMenu, Is.True);
            Assert.That(first.ShowInHeader, Is.True);
            Assert.That(first.Order, Is.EqualTo(1));

            var second = navIndex.Projects.First(p => p.Slug == "second-project");
            Assert.That(second.ShowInMenu, Is.False);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task GenerateAsyncFolderBasedContentTest()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var contentDir = Path.Combine(tempDir, "content");
        var articlesDir = Path.Combine(contentDir, "articles");
        var articleFolder = Path.Combine(articlesDir, "01-getting-started");
        Directory.CreateDirectory(articleFolder);

        File.WriteAllText(Path.Combine(articleFolder, "index.md"), """
            ---
            title: Getting Started Guide
            menuTitle: Getting Started
            showInMenu: true
            ---
            Content here
            """);

        var config = new GeneratorConfig
        {
            SitePath = tempDir,
            OutputPath = tempDir
        };
        var generator = new NavigationIndexGenerator(config);
        var contentIndex = new Framework.Content.ContentIndex
        {
            Articles = ["01-getting-started/index.md"]
        };

        try
        {
            // Act
            await generator.GenerateAsync(contentIndex);

            // Assert
            var outputPath = Path.Combine(tempDir, "navigation-index.json");
            var json = await File.ReadAllTextAsync(outputPath);
            var navIndex = JsonSerializer.Deserialize<NavigationIndex>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.That(navIndex!.Articles, Has.Count.EqualTo(1));
            Assert.That(navIndex.Articles[0].Slug, Is.EqualTo("getting-started"));
            Assert.That(navIndex.Articles[0].Title, Is.EqualTo("Getting Started Guide"));
            Assert.That(navIndex.Articles[0].MenuTitle, Is.EqualTo("Getting Started"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task GenerateAsyncDynamicSectionsTest()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var contentDir = Path.Combine(tempDir, "content");
        var solutionsDir = Path.Combine(contentDir, "solutions");
        Directory.CreateDirectory(solutionsDir);

        File.WriteAllText(Path.Combine(solutionsDir, "01-enterprise.md"), """
            ---
            title: Enterprise Solution
            showInMenu: true
            ---
            Content
            """);

        File.WriteAllText(Path.Combine(solutionsDir, "02-startup.md"), """
            ---
            title: Startup Solution
            showInMenu: true
            ---
            Content
            """);

        var config = new GeneratorConfig
        {
            SitePath = tempDir,
            OutputPath = tempDir
        };
        var generator = new NavigationIndexGenerator(config);
        var contentIndex = new Framework.Content.ContentIndex
        {
            Sections = new Dictionary<string, List<string>>
            {
                ["solutions"] = ["01-enterprise.md", "02-startup.md"]
            }
        };

        try
        {
            // Act
            await generator.GenerateAsync(contentIndex);

            // Assert
            var outputPath = Path.Combine(tempDir, "navigation-index.json");
            var json = await File.ReadAllTextAsync(outputPath);
            var navIndex = JsonSerializer.Deserialize<NavigationIndex>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.That(navIndex!.Sections, Does.ContainKey("solutions"));
            Assert.That(navIndex.Sections["solutions"], Has.Count.EqualTo(2));
            Assert.That(navIndex.Sections["solutions"][0].Slug, Is.EqualTo("enterprise"));
            Assert.That(navIndex.Sections["solutions"][1].Slug, Is.EqualTo("startup"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task GenerateAsyncSortsItemsByOrderTest()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var contentDir = Path.Combine(tempDir, "content");
        var docsDir = Path.Combine(contentDir, "docs");
        Directory.CreateDirectory(docsDir);

        // Create out of order
        File.WriteAllText(Path.Combine(docsDir, "03-advanced.md"), """
            ---
            title: Advanced
            ---
            Content
            """);

        File.WriteAllText(Path.Combine(docsDir, "01-intro.md"), """
            ---
            title: Introduction
            ---
            Content
            """);

        File.WriteAllText(Path.Combine(docsDir, "02-basics.md"), """
            ---
            title: Basics
            ---
            Content
            """);

        var config = new GeneratorConfig
        {
            SitePath = tempDir,
            OutputPath = tempDir
        };
        var generator = new NavigationIndexGenerator(config);
        var contentIndex = new Framework.Content.ContentIndex
        {
            Docs = ["03-advanced.md", "01-intro.md", "02-basics.md"]
        };

        try
        {
            // Act
            await generator.GenerateAsync(contentIndex);

            // Assert
            var outputPath = Path.Combine(tempDir, "navigation-index.json");
            var json = await File.ReadAllTextAsync(outputPath);
            var navIndex = JsonSerializer.Deserialize<NavigationIndex>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Should be sorted by order
            Assert.That(navIndex!.Docs[0].Order, Is.EqualTo(1));
            Assert.That(navIndex.Docs[0].Title, Is.EqualTo("Introduction"));
            Assert.That(navIndex.Docs[1].Order, Is.EqualTo(2));
            Assert.That(navIndex.Docs[1].Title, Is.EqualTo("Basics"));
            Assert.That(navIndex.Docs[2].Order, Is.EqualTo(3));
            Assert.That(navIndex.Docs[2].Title, Is.EqualTo("Advanced"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public void NavigationMenuItemDisplayTitleReturnsMenuTitleWhenSetTest()
    {
        // Arrange
        var item = new NavigationMenuItem
        {
            Title = "Full Title",
            MenuTitle = "Short"
        };

        // Act & Assert
        Assert.That(item.DisplayTitle, Is.EqualTo("Short"));
    }

    [Test]
    public void NavigationMenuItemDisplayTitleReturnsTitleWhenMenuTitleNotSetTest()
    {
        // Arrange
        var item = new NavigationMenuItem
        {
            Title = "Full Title",
            MenuTitle = null
        };

        // Act & Assert
        Assert.That(item.DisplayTitle, Is.EqualTo("Full Title"));
    }

    #endregion

    #region Tools

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "OutWit.Test." + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(path);
        return path;
    }

    #endregion
}
