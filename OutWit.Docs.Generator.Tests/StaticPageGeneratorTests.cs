using NUnit.Framework;
using OutWit.Docs.Framework.Configuration;
using OutWit.Docs.Framework.Content;
using OutWit.Docs.Generator.Commands;
using OutWit.Docs.Generator.Services;

namespace OutWit.Docs.Generator.Tests;

/// <summary>
/// Tests for StaticPageGenerator service.
/// </summary>
[TestFixture]
public class StaticPageGeneratorTests
{
    #region Tests

    [Test]
    public async Task GenerateAsyncSkipsIfNoTemplateTest()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var config = new GeneratorConfig { OutputPath = tempDir };
        var generator = new StaticPageGenerator(config, null, "https://example.com", "Test Site");
        var index = new ContentIndex { Blog = ["test.md"] };

        try
        {
            // Act - should not throw, just skip
            await generator.GenerateAsync(index);

            // Assert - no pages generated without index.html template
            var blogDir = Path.Combine(tempDir, "blog");
            // blog dir may exist from index page but no post subdirectory
            Assert.That(Directory.Exists(Path.Combine(tempDir, "blog", "test")), Is.False);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task GenerateAsyncCreatesStaticHtmlPagesTest()
    {
        // Arrange
        var tempDir = CreateTempDirectory();

        // Create template
        CreateTemplate(tempDir);

        // Create blog post in content directory
        SetupContentDirectory(tempDir, "blog", "2024-01-15-test-post.md", """
            ---
            title: Test Blog Post
            description: Test description
            publishDate: 2024-01-15
            ---
            
            # Hello World
            This is test content.
            """);

        var config = new GeneratorConfig { OutputPath = tempDir };
        var generator = new StaticPageGenerator(config, null, "https://example.com", "Test Site");
        var index = new ContentIndex { Blog = ["2024-01-15-test-post.md"] };

        try
        {
            // Act
            await generator.GenerateAsync(index);

            // Assert
            var outputPath = Path.Combine(tempDir, "blog", "test-post", "index.html");
            Assert.That(File.Exists(outputPath), Is.True);

            var content = await File.ReadAllTextAsync(outputPath);
            Assert.That(content, Does.Contain("Test Blog Post"));
            Assert.That(content, Does.Contain("og:title"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task GenerateAsyncGeneratesIndexPagesTest()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        CreateTemplate(tempDir);

        var config = new GeneratorConfig { OutputPath = tempDir };
        var generator = new StaticPageGenerator(config, null, "https://example.com", "Test Site");
        var index = new ContentIndex();

        try
        {
            // Act
            await generator.GenerateAsync(index);

            // Assert - home + always-present form pages are created even with no content
            Assert.That(File.Exists(Path.Combine(tempDir, "index.html")), Is.True);
            Assert.That(File.Exists(Path.Combine(tempDir, "contact", "index.html")), Is.True);
            Assert.That(File.Exists(Path.Combine(tempDir, "search", "index.html")), Is.True);

            // Content list pages are only generated when content exists
            Assert.That(File.Exists(Path.Combine(tempDir, "blog", "index.html")), Is.False);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task GenerateAsyncPrerendersHomePageContentTest()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        CreateTemplate(tempDir);
        SetupContentDirectory(tempDir, "projects", "01-my-project.md", """
            ---
            title: My Project
            summary: A neat project.
            ---

            # My Project
            Body.
            """);

        var config = new GeneratorConfig { OutputPath = tempDir };
        var generator = new StaticPageGenerator(config, null, "https://example.com", "Test Site");
        var index = new ContentIndex { Projects = ["01-my-project.md"] };

        try
        {
            // Act
            await generator.GenerateAsync(index);

            // Assert - the home page (root index.html) now contains real, crawlable content
            var home = await File.ReadAllTextAsync(Path.Combine(tempDir, "index.html"));
            Assert.That(home, Does.Contain("My Project"));
            Assert.That(home, Does.Contain("/project/my-project/")); // final 200 URL (trailing slash)
            // Blazor bootstrap must still be present so the SPA can hydrate
            Assert.That(home, Does.Contain("id=\"app\""));
            // Uses the framework's card markup so it is styled (no flash of an
            // unstyled list before hydration).
            Assert.That(home, Does.Contain("projects-list"));
            Assert.That(home, Does.Contain("content-card"));
            Assert.That(home, Does.Not.Contain("content-list__items"));
            // Pre-rendered content sits in a hidden block (crawlers read it from
            // source; users see the template's loading indicator then the SPA) —
            // no flash of unstyled content.
            Assert.That(home, Does.Contain("ssg-prerender"));
            Assert.That(home, Does.Contain("Loading...")); // template spinner preserved
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task GenerateAsyncLandingSectionPutsLeadPageAtRootTest()
    {
        // Arrange — a landing section: the lead (first) page should live at the
        // short route itself (/vision/), the rest at /vision/{slug}/, and there
        // should be NO /vision/{lead-slug}/ and NO listing root.
        var tempDir = CreateTempDirectory();
        CreateTemplate(tempDir);
        SetupContentDirectory(tempDir, "vision", "01-the-ultracomputer.md", """
            ---
            title: The Ultracomputer
            description: Lead.
            ---

            # The Ultracomputer
            Lead body.
            """);
        SetupContentDirectory(tempDir, "vision", "02-roadmap.md", """
            ---
            title: Roadmap
            description: Second.
            ---

            # Roadmap
            Second body.
            """);

        var siteConfig = new SiteConfig
        {
            ContentSections =
            [
                new ContentSectionConfig { Folder = "vision", Route = "vision", LandingPage = true }
            ]
        };
        var config = new GeneratorConfig { OutputPath = tempDir };
        var generator = new StaticPageGenerator(config, siteConfig, "https://example.com", "Test Site");
        var index = new ContentIndex
        {
            Sections = { ["vision"] = ["01-the-ultracomputer.md", "02-roadmap.md"] }
        };

        try
        {
            // Act
            await generator.GenerateAsync(index);

            // Assert — lead page is at the section root, carrying its real content
            var root = Path.Combine(tempDir, "vision", "index.html");
            Assert.That(File.Exists(root), Is.True);
            var rootHtml = await File.ReadAllTextAsync(root);
            Assert.That(rootHtml, Does.Contain("The Ultracomputer"));
            Assert.That(rootHtml, Does.Contain("https://example.com/vision/")); // canonical

            // og:image points to the lead page's own image ({folder}-{slug}), not the
            // default — even though its canonical URL has a single path segment.
            Assert.That(rootHtml, Does.Contain("https://example.com/og-images/vision-the-ultracomputer.png"));

            // Lead page is NOT duplicated under /vision/{lead-slug}/
            Assert.That(File.Exists(Path.Combine(tempDir, "vision", "the-ultracomputer", "index.html")), Is.False);

            // Remaining page keeps /vision/{slug}/
            Assert.That(File.Exists(Path.Combine(tempDir, "vision", "roadmap", "index.html")), Is.True);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public void ReplaceAppContentHandlesNestedDivsTest()
    {
        // Arrange - a realistic template whose #app contains a nested loading <div>
        var tempDir = CreateTempDirectory();
        File.WriteAllText(Path.Combine(tempDir, "index.html"), """
            <!DOCTYPE html>
            <html>
            <head><title>Template</title></head>
            <body>
              <div id="app">
                <div class="loading"><span>Loading...</span></div>
              </div>
              <script src="_framework/blazor.webassembly.js"></script>
            </body>
            </html>
            """);

        var config = new GeneratorConfig { OutputPath = tempDir };
        var generator = new StaticPageGenerator(config, null, "https://example.com", "Test Site");

        try
        {
            // Act
            generator.GenerateAsync(new ContentIndex()).GetAwaiter().GetResult();

            // Assert - no orphaned </div> after the script (which the old non-greedy regex produced)
            var home = File.ReadAllText(Path.Combine(tempDir, "index.html"));
            var scriptIdx = home.IndexOf("blazor.webassembly.js", StringComparison.Ordinal);
            Assert.That(scriptIdx, Is.GreaterThan(0));
            var afterScript = home[scriptIdx..];
            Assert.That(afterScript, Does.Not.Contain("</div>"));
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

    private static void CreateTemplate(string rootDir)
    {
        File.WriteAllText(Path.Combine(rootDir, "index.html"), """
            <!DOCTYPE html>
            <html>
            <head><title>Template</title></head>
            <body><div id="app">Loading...</div></body>
            </html>
            """);
    }

    private static void SetupContentDirectory(string rootDir, string folder, string filename, string content)
    {
        var dir = Path.Combine(rootDir, "content", folder);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, filename), content);
    }

    #endregion
}
