using System.Text.Json;
using NUnit.Framework;
using OutWit.Web.Framework.Models;
using OutWit.Web.Generator.Commands;
using OutWit.Web.Generator.Services;

namespace OutWit.Web.Generator.Tests;

/// <summary>
/// Tests for ContentMetadataGenerator service.
/// </summary>
[TestFixture]
public class ContentMetadataGeneratorTests
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
        var generator = new ContentMetadataGenerator(config);
        var contentIndex = new Framework.Content.ContentIndex();

        try
        {
            // Act
            await generator.GenerateAsync(contentIndex);

            // Assert
            var outputPath = Path.Combine(tempDir, "content-metadata.json");
            Assert.That(File.Exists(outputPath), Is.True);

            var json = await File.ReadAllTextAsync(outputPath);
            var metadataIndex = JsonSerializer.Deserialize<ContentMetadataIndex>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.That(metadataIndex, Is.Not.Null);
            Assert.That(metadataIndex!.Blog, Is.Empty);
            Assert.That(metadataIndex.Projects, Is.Empty);
            Assert.That(metadataIndex.Articles, Is.Empty);
            Assert.That(metadataIndex.Docs, Is.Empty);
            Assert.That(metadataIndex.Features, Is.Empty);
            Assert.That(metadataIndex.Sections, Is.Empty);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task GenerateAsyncBlogPostsExtractsMetadataTest()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var contentDir = Path.Combine(tempDir, "content");
        var blogDir = Path.Combine(contentDir, "blog");
        Directory.CreateDirectory(blogDir);

        File.WriteAllText(Path.Combine(blogDir, "2024-01-15-my-post.md"), """
            ---
            title: My Blog Post
            description: Short description
            summary: This is a longer summary
            tags: [blazor, dotnet]
            publishDate: 2024-01-15
            author: John Doe
            featuredImage: /images/post.png
            ---
            
            This is the content of the blog post with some words here.
            """);

        var config = new GeneratorConfig
        {
            SitePath = tempDir,
            OutputPath = tempDir
        };
        var generator = new ContentMetadataGenerator(config);
        var contentIndex = new Framework.Content.ContentIndex
        {
            Blog = ["2024-01-15-my-post.md"]
        };

        try
        {
            // Act
            await generator.GenerateAsync(contentIndex);

            // Assert
            var outputPath = Path.Combine(tempDir, "content-metadata.json");
            var json = await File.ReadAllTextAsync(outputPath);
            var metadataIndex = JsonSerializer.Deserialize<ContentMetadataIndex>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.That(metadataIndex!.Blog, Has.Count.EqualTo(1));
            
            var post = metadataIndex.Blog[0];
            Assert.That(post.Slug, Is.EqualTo("my-post"));
            Assert.That(post.Title, Is.EqualTo("My Blog Post"));
            Assert.That(post.Description, Is.EqualTo("Short description"));
            Assert.That(post.Summary, Is.EqualTo("This is a longer summary"));
            Assert.That(post.Tags, Does.Contain("blazor"));
            Assert.That(post.Tags, Does.Contain("dotnet"));
            Assert.That(post.PublishDate, Is.EqualTo(new DateTime(2024, 1, 15)));
            Assert.That(post.Author, Is.EqualTo("John Doe"));
            Assert.That(post.FeaturedImage, Is.EqualTo("/images/post.png"));
            Assert.That(post.ReadingTimeMinutes, Is.GreaterThanOrEqualTo(1));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task GenerateAsyncProjectsExtractsMetadataTest()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var contentDir = Path.Combine(tempDir, "content");
        var projectsDir = Path.Combine(contentDir, "projects");
        Directory.CreateDirectory(projectsDir);

        File.WriteAllText(Path.Combine(projectsDir, "01-my-project.md"), """
            ---
            title: My Project
            description: Project description
            summary: Project summary
            tags: [csharp, blazor]
            url: https://example.com
            ---
            Content
            """);

        var config = new GeneratorConfig
        {
            SitePath = tempDir,
            OutputPath = tempDir
        };
        var generator = new ContentMetadataGenerator(config);
        var contentIndex = new Framework.Content.ContentIndex
        {
            Projects = ["01-my-project.md"]
        };

        try
        {
            // Act
            await generator.GenerateAsync(contentIndex);

            // Assert
            var outputPath = Path.Combine(tempDir, "content-metadata.json");
            var json = await File.ReadAllTextAsync(outputPath);
            var metadataIndex = JsonSerializer.Deserialize<ContentMetadataIndex>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.That(metadataIndex!.Projects, Has.Count.EqualTo(1));
            
            var project = metadataIndex.Projects[0];
            Assert.That(project.Slug, Is.EqualTo("my-project"));
            Assert.That(project.Title, Is.EqualTo("My Project"));
            Assert.That(project.Description, Is.EqualTo("Project description"));
            Assert.That(project.Summary, Is.EqualTo("Project summary"));
            Assert.That(project.Order, Is.EqualTo(1));
            Assert.That(project.Tags, Does.Contain("csharp"));
            Assert.That(project.Url, Is.EqualTo("https://example.com"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task GenerateAsyncBlogPostsSortedByDateDescendingTest()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var contentDir = Path.Combine(tempDir, "content");
        var blogDir = Path.Combine(contentDir, "blog");
        Directory.CreateDirectory(blogDir);

        File.WriteAllText(Path.Combine(blogDir, "2024-01-01-first.md"), """
            ---
            title: First Post
            publishDate: 2024-01-01
            ---
            Content
            """);

        File.WriteAllText(Path.Combine(blogDir, "2024-01-15-second.md"), """
            ---
            title: Second Post
            publishDate: 2024-01-15
            ---
            Content
            """);

        File.WriteAllText(Path.Combine(blogDir, "2024-01-10-third.md"), """
            ---
            title: Third Post
            publishDate: 2024-01-10
            ---
            Content
            """);

        var config = new GeneratorConfig
        {
            SitePath = tempDir,
            OutputPath = tempDir
        };
        var generator = new ContentMetadataGenerator(config);
        var contentIndex = new Framework.Content.ContentIndex
        {
            Blog = ["2024-01-01-first.md", "2024-01-15-second.md", "2024-01-10-third.md"]
        };

        try
        {
            // Act
            await generator.GenerateAsync(contentIndex);

            // Assert
            var outputPath = Path.Combine(tempDir, "content-metadata.json");
            var json = await File.ReadAllTextAsync(outputPath);
            var metadataIndex = JsonSerializer.Deserialize<ContentMetadataIndex>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Should be sorted by date descending (newest first)
            Assert.That(metadataIndex!.Blog[0].Title, Is.EqualTo("Second Post"));
            Assert.That(metadataIndex.Blog[1].Title, Is.EqualTo("Third Post"));
            Assert.That(metadataIndex.Blog[2].Title, Is.EqualTo("First Post"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task GenerateAsyncDynamicSectionsExtractsMetadataTest()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var contentDir = Path.Combine(tempDir, "content");
        var solutionsDir = Path.Combine(contentDir, "solutions");
        Directory.CreateDirectory(solutionsDir);

        File.WriteAllText(Path.Combine(solutionsDir, "01-enterprise.md"), """
            ---
            title: Enterprise Solution
            description: For large companies
            tags: [enterprise]
            ---
            Content
            """);

        var config = new GeneratorConfig
        {
            SitePath = tempDir,
            OutputPath = tempDir
        };
        var generator = new ContentMetadataGenerator(config);
        var contentIndex = new Framework.Content.ContentIndex
        {
            Sections = new Dictionary<string, List<string>>
            {
                ["solutions"] = ["01-enterprise.md"]
            }
        };

        try
        {
            // Act
            await generator.GenerateAsync(contentIndex);

            // Assert
            var outputPath = Path.Combine(tempDir, "content-metadata.json");
            var json = await File.ReadAllTextAsync(outputPath);
            var metadataIndex = JsonSerializer.Deserialize<ContentMetadataIndex>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.That(metadataIndex!.Sections, Does.ContainKey("solutions"));
            Assert.That(metadataIndex.Sections["solutions"], Has.Count.EqualTo(1));
            Assert.That(metadataIndex.Sections["solutions"][0].Title, Is.EqualTo("Enterprise Solution"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task GenerateAsyncFeaturesExtractsMetadataTest()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var contentDir = Path.Combine(tempDir, "content");
        var featuresDir = Path.Combine(contentDir, "features");
        Directory.CreateDirectory(featuresDir);

        File.WriteAllText(Path.Combine(featuresDir, "01-fast.md"), """
            ---
            title: Fast Performance
            description: Lightning fast
            icon: bolt
            iconSvg: <svg>...</svg>
            ---
            Content
            """);

        var config = new GeneratorConfig
        {
            SitePath = tempDir,
            OutputPath = tempDir
        };
        var generator = new ContentMetadataGenerator(config);
        var contentIndex = new Framework.Content.ContentIndex
        {
            Features = ["01-fast.md"]
        };

        try
        {
            // Act
            await generator.GenerateAsync(contentIndex);

            // Assert
            var outputPath = Path.Combine(tempDir, "content-metadata.json");
            var json = await File.ReadAllTextAsync(outputPath);
            var metadataIndex = JsonSerializer.Deserialize<ContentMetadataIndex>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.That(metadataIndex!.Features, Has.Count.EqualTo(1));
            Assert.That(metadataIndex.Features[0].Title, Is.EqualTo("Fast Performance"));
            Assert.That(metadataIndex.Features[0].Icon, Is.EqualTo("bolt"));
            Assert.That(metadataIndex.Features[0].IconSvg, Is.EqualTo("<svg>...</svg>"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
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
