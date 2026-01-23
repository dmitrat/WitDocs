using NUnit.Framework;
using OutWit.Web.Generator.Services;

namespace OutWit.Web.Generator.Tests;

/// <summary>
/// Tests for ContentScanner service.
/// </summary>
[TestFixture]
public class ContentScannerTests
{
    #region Tests

    [Test]
    public async Task ScanAsyncEmptyDirectoryReturnsEmptyIndexTest()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var scanner = new ContentScanner(tempDir);

        try
        {
            // Act
            var index = await scanner.ScanAsync();

            // Assert
            Assert.That(index.Blog, Is.Empty);
            Assert.That(index.Projects, Is.Empty);
            Assert.That(index.Articles, Is.Empty);
            Assert.That(index.Docs, Is.Empty);
            Assert.That(index.Features, Is.Empty);
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task ScanAsyncBlogPostsReturnsSortedDescendingTest()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var blogDir = Path.Combine(tempDir, "blog");
        Directory.CreateDirectory(blogDir);
        
        File.WriteAllText(Path.Combine(blogDir, "2024-01-01-first.md"), "---\ntitle: First\n---\nContent");
        File.WriteAllText(Path.Combine(blogDir, "2024-01-15-second.md"), "---\ntitle: Second\n---\nContent");
        File.WriteAllText(Path.Combine(blogDir, "2024-01-10-third.md"), "---\ntitle: Third\n---\nContent");

        var scanner = new ContentScanner(tempDir);

        try
        {
            // Act
            var index = await scanner.ScanAsync();

            // Assert
            Assert.That(index.Blog, Has.Count.EqualTo(3));
            Assert.That(index.Blog[0], Is.EqualTo("2024-01-15-second.md")); // Latest first
            Assert.That(index.Blog[1], Is.EqualTo("2024-01-10-third.md"));
            Assert.That(index.Blog[2], Is.EqualTo("2024-01-01-first.md"));
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task ScanAsyncArticlesReturnsSortedAscendingTest()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var articlesDir = Path.Combine(tempDir, "articles");
        Directory.CreateDirectory(articlesDir);
        
        File.WriteAllText(Path.Combine(articlesDir, "02-second.md"), "content");
        File.WriteAllText(Path.Combine(articlesDir, "01-first.md"), "content");
        File.WriteAllText(Path.Combine(articlesDir, "03-third.md"), "content");

        var scanner = new ContentScanner(tempDir);

        try
        {
            // Act
            var index = await scanner.ScanAsync();

            // Assert
            Assert.That(index.Articles, Has.Count.EqualTo(3));
            Assert.That(index.Articles[0], Is.EqualTo("01-first.md"));
            Assert.That(index.Articles[1], Is.EqualTo("02-second.md"));
            Assert.That(index.Articles[2], Is.EqualTo("03-third.md"));
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task ScanAsyncFolderBasedProjectsReturnsIndexMdTest()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var projectsDir = Path.Combine(tempDir, "projects");
        var project1Dir = Path.Combine(projectsDir, "01-project");
        Directory.CreateDirectory(project1Dir);
        
        File.WriteAllText(Path.Combine(project1Dir, "index.md"), "content");
        File.WriteAllText(Path.Combine(projectsDir, "02-simple.md"), "content");

        var scanner = new ContentScanner(tempDir);

        try
        {
            // Act
            var index = await scanner.ScanAsync();

            // Assert
            Assert.That(index.Projects, Has.Count.EqualTo(2));
            Assert.That(index.Projects[0], Is.EqualTo("01-project/index.md"));
            Assert.That(index.Projects[1], Is.EqualTo("02-simple.md"));
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task ScanAsyncFolderBasedBlogPostsReturnsIndexMdTest()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var blogDir = Path.Combine(tempDir, "blog");
        var postDir = Path.Combine(blogDir, "2024-01-15-my-post");
        Directory.CreateDirectory(postDir);

        // Folder-based blog post with resources
        File.WriteAllText(Path.Combine(postDir, "index.md"), "---\ntitle: My Post\n---\nContent");
        File.WriteAllText(Path.Combine(postDir, "diagram.svg"), "<svg></svg>");
        // File-based blog post
        File.WriteAllText(Path.Combine(blogDir, "2024-01-10-simple.md"), "---\ntitle: Simple\n---\nContent");

        var scanner = new ContentScanner(tempDir);

        try
        {
            // Act
            var index = await scanner.ScanAsync();

            // Assert - both folder-based and file-based posts
            Assert.That(index.Blog, Has.Count.EqualTo(2));
            Assert.That(index.Blog[0], Is.EqualTo("2024-01-15-my-post/index.md")); // Latest first (descending)
            Assert.That(index.Blog[1], Is.EqualTo("2024-01-10-simple.md"));
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task ScanAsyncFolderBasedArticlesReturnsIndexMdTest()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var articlesDir = Path.Combine(tempDir, "articles");
        var articleDir = Path.Combine(articlesDir, "01-getting-started");
        Directory.CreateDirectory(articleDir);

        // Folder-based article with resources
        File.WriteAllText(Path.Combine(articleDir, "index.md"), "content");
        File.WriteAllText(Path.Combine(articleDir, "screenshot.png"), "image");
        // File-based article
        File.WriteAllText(Path.Combine(articlesDir, "02-advanced.md"), "content");

        var scanner = new ContentScanner(tempDir);

        try
        {
            // Act
            var index = await scanner.ScanAsync();

            // Assert - both folder-based and file-based articles (ascending)
            Assert.That(index.Articles, Has.Count.EqualTo(2));
            Assert.That(index.Articles[0], Is.EqualTo("01-getting-started/index.md"));
            Assert.That(index.Articles[1], Is.EqualTo("02-advanced.md"));
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task ScanAsyncFolderBasedDocsReturnsIndexMdTest()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var docsDir = Path.Combine(tempDir, "docs");
        var docDir = Path.Combine(docsDir, "01-installation");
        Directory.CreateDirectory(docDir);

        File.WriteAllText(Path.Combine(docDir, "index.md"), "content");
        File.WriteAllText(Path.Combine(docsDir, "02-configuration.md"), "content");

        var scanner = new ContentScanner(tempDir);

        try
        {
            // Act
            var index = await scanner.ScanAsync();

            // Assert
            Assert.That(index.Docs, Has.Count.EqualTo(2));
            Assert.That(index.Docs[0], Is.EqualTo("01-installation/index.md"));
            Assert.That(index.Docs[1], Is.EqualTo("02-configuration.md"));
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task ScanAsyncFolderBasedDynamicSectionsReturnsIndexMdTest()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var solutionsDir = Path.Combine(tempDir, "solutions");
        var solutionDir = Path.Combine(solutionsDir, "01-enterprise");
        Directory.CreateDirectory(solutionDir);

        File.WriteAllText(Path.Combine(solutionDir, "index.md"), "content");
        File.WriteAllText(Path.Combine(solutionDir, "architecture.svg"), "<svg></svg>");
        File.WriteAllText(Path.Combine(solutionsDir, "02-startup.md"), "content");

        var siteConfigPath = Path.Combine(Path.GetDirectoryName(tempDir)!, "site.config.json");
        var siteConfig = """
            {
              "contentSections": [
                { "folder": "solutions", "route": "solutions", "menuTitle": "Solutions" }
              ]
            }
            """;
        File.WriteAllText(siteConfigPath, siteConfig);

        var scanner = new ContentScanner(tempDir, siteConfigPath);

        try
        {
            // Act
            var index = await scanner.ScanAsync();

            // Assert
            Assert.That(index.Sections, Does.ContainKey("solutions"));
            Assert.That(index.Sections["solutions"], Has.Count.EqualTo(2));
            Assert.That(index.Sections["solutions"][0], Is.EqualTo("01-enterprise/index.md"));
            Assert.That(index.Sections["solutions"][1], Is.EqualTo("02-startup.md"));
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempDir, true);
            if (File.Exists(siteConfigPath))
                File.Delete(siteConfigPath);
        }
    }

    [Test]
    public async Task ScanAsyncIgnoresFoldersWithoutIndexMdTest()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var blogDir = Path.Combine(tempDir, "blog");
        var emptyDir = Path.Combine(blogDir, "2024-01-15-incomplete");
        var validDir = Path.Combine(blogDir, "2024-01-10-complete");
        Directory.CreateDirectory(emptyDir);
        Directory.CreateDirectory(validDir);

        // Folder without index.md should be ignored
        File.WriteAllText(Path.Combine(emptyDir, "draft.txt"), "draft");
        // Folder with index.md should be included
        File.WriteAllText(Path.Combine(validDir, "index.md"), "content");

        var scanner = new ContentScanner(tempDir);

        try
        {
            // Act
            var index = await scanner.ScanAsync();

            // Assert - only the folder with index.md
            Assert.That(index.Blog, Has.Count.EqualTo(1));
            Assert.That(index.Blog[0], Is.EqualTo("2024-01-10-complete/index.md"));
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task ScanAsyncSupportsMdxFilesTest()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var blogDir = Path.Combine(tempDir, "blog");
        var postDir = Path.Combine(blogDir, "2024-01-15-mdx-post");
        Directory.CreateDirectory(postDir);

        File.WriteAllText(Path.Combine(postDir, "index.mdx"), "content with JSX");
        File.WriteAllText(Path.Combine(blogDir, "2024-01-10-regular.md"), "content");

        var scanner = new ContentScanner(tempDir);

        try
        {
            // Act
            var index = await scanner.ScanAsync();

            // Assert - supports both .md and .mdx
            Assert.That(index.Blog, Has.Count.EqualTo(2));
            Assert.That(index.Blog[0], Is.EqualTo("2024-01-15-mdx-post/index.mdx"));
            Assert.That(index.Blog[1], Is.EqualTo("2024-01-10-regular.md"));
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task ScanAsyncDynamicSectionsFromConfigTest()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var solutionsDir = Path.Combine(tempDir, "solutions");
        Directory.CreateDirectory(solutionsDir);
        
        File.WriteAllText(Path.Combine(solutionsDir, "01-enterprise.md"), "content");
        File.WriteAllText(Path.Combine(solutionsDir, "02-startup.md"), "content");
        
        // Create site.config.json with contentSections
        var siteConfigPath = Path.Combine(Path.GetDirectoryName(tempDir)!, "site.config.json");
        var siteConfig = """
            {
              "contentSections": [
                { "folder": "solutions", "route": "solutions", "menuTitle": "Solutions" }
              ]
            }
            """;
        File.WriteAllText(siteConfigPath, siteConfig);

        var scanner = new ContentScanner(tempDir, siteConfigPath);

        try
        {
            // Act
            var index = await scanner.ScanAsync();

            // Assert
            Assert.That(index.Sections, Does.ContainKey("solutions"));
            Assert.That(index.Sections["solutions"], Has.Count.EqualTo(2));
            Assert.That(index.Sections["solutions"][0], Is.EqualTo("01-enterprise.md"));
            Assert.That(index.Sections["solutions"][1], Is.EqualTo("02-startup.md"));
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempDir, true);
            if (File.Exists(siteConfigPath))
                File.Delete(siteConfigPath);
        }
    }

    [Test]
    public async Task ScanAsyncEmptyDirectoryReturnsEmptySectionsTest()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var scanner = new ContentScanner(tempDir);

        try
        {
            // Act
            var index = await scanner.ScanAsync();

            // Assert
            Assert.That(index.Sections, Is.Empty);
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task ScanAsyncExcludesCompressedFilesTest()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var blogDir = Path.Combine(tempDir, "blog");
        var featuresDir = Path.Combine(tempDir, "features");
        Directory.CreateDirectory(blogDir);
        Directory.CreateDirectory(featuresDir);
        
        // Create .md files and their compressed versions (.gz, .br)
        File.WriteAllText(Path.Combine(blogDir, "2024-01-15-test-post.md"), "content");
        File.WriteAllText(Path.Combine(blogDir, "2024-01-15-test-post.md.gz"), "compressed");
        File.WriteAllText(Path.Combine(blogDir, "2024-01-15-test-post.md.br"), "compressed");
        
        File.WriteAllText(Path.Combine(featuresDir, "01-feature.md"), "content");
        File.WriteAllText(Path.Combine(featuresDir, "01-feature.md.gz"), "compressed");
        File.WriteAllText(Path.Combine(featuresDir, "01-feature.md.br"), "compressed");

        var scanner = new ContentScanner(tempDir);

        try
        {
            // Act
            var index = await scanner.ScanAsync();

            // Assert - should only include .md files, not .gz or .br
            Assert.That(index.Blog, Has.Count.EqualTo(1));
            Assert.That(index.Blog[0], Is.EqualTo("2024-01-15-test-post.md"));
            Assert.That(index.Blog, Has.None.Matches<string>(f => f.EndsWith(".gz")));
            Assert.That(index.Blog, Has.None.Matches<string>(f => f.EndsWith(".br")));
            
            Assert.That(index.Features, Has.Count.EqualTo(1));
            Assert.That(index.Features[0], Is.EqualTo("01-feature.md"));
            Assert.That(index.Features, Has.None.Matches<string>(f => f.EndsWith(".gz")));
            Assert.That(index.Features, Has.None.Matches<string>(f => f.EndsWith(".br")));
        }
        finally
        {
            // Cleanup
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
