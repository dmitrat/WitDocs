using NUnit.Framework;
using OutWit.Web.Generator.Commands;

namespace OutWit.Web.Generator.Tests;

/// <summary>
/// End-to-end tests for the full generation pipeline (ContentGenerator.GenerateAllAsync),
/// which wires the scanner together with every generator. Catches cross-generator
/// wiring problems that the per-generator unit tests cannot.
/// </summary>
[TestFixture]
public class ContentGeneratorTests
{
    #region Tests

    [Test]
    public async Task GenerateAllAsyncProducesAllExpectedArtifactsTest()
    {
        // Arrange
        var dir = CreateSiteFixture();
        var config = new GeneratorConfig
        {
            SitePath = dir,
            OutputPath = dir,
            SiteUrl = "https://example.com",
            HostingProvider = "cloudflare",
            GenerateOgImages = false // no Playwright in CI
        };
        var generator = new ContentGenerator(config);

        try
        {
            // Act
            await generator.GenerateAllAsync();

            // Assert - every pipeline step wrote its artifact
            AssertExists(dir, "content/index.json");
            AssertExists(dir, "navigation-index.json");
            AssertExists(dir, "content-metadata.json");
            AssertExists(dir, "sitemap.xml");
            AssertExists(dir, "robots.txt");
            AssertExists(dir, "search-index.json");
            AssertExists(dir, "feed.xml");
            AssertExists(dir, "_routes.json");   // cloudflare hosting config
            AssertExists(dir, "_headers");

            // Static pages: home (root index.html rewritten) + detail pages
            AssertExists(dir, "index.html");
            AssertExists(dir, "blog/hello-world/index.html");
            AssertExists(dir, "project/my-project/index.html");

            // Cross-cutting content correctness
            var sitemap = await File.ReadAllTextAsync(Path.Combine(dir, "sitemap.xml"));
            Assert.That(sitemap, Does.Contain("https://example.com/blog/hello-world"));

            var feed = await File.ReadAllTextAsync(Path.Combine(dir, "feed.xml"));
            Assert.That(feed, Does.Contain("Hello World"));

            var home = await File.ReadAllTextAsync(Path.Combine(dir, "index.html"));
            Assert.That(home, Does.Contain("/project/my-project"));

            var post = await File.ReadAllTextAsync(Path.Combine(dir, "blog/hello-world/index.html"));
            Assert.That(post, Does.Contain("Hello World"));
            Assert.That(post, Does.Contain("og:title"));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    #endregion

    #region Tools

    private static string CreateSiteFixture()
    {
        var dir = Path.Combine(Path.GetTempPath(), "OutWit.Test." + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);

        // index.html template (with a nested div inside #app, like the real template)
        File.WriteAllText(Path.Combine(dir, "index.html"), """
            <!DOCTYPE html>
            <html>
            <head><title>Template</title></head>
            <body>
              <div id="app"><div class="loading">Loading...</div></div>
              <script src="_framework/blazor.webassembly.js"></script>
            </body>
            </html>
            """);

        File.WriteAllText(Path.Combine(dir, "site.config.json"), """
            { "siteName": "Test Site", "baseUrl": "https://example.com" }
            """);

        WriteContent(dir, "blog", "2024-05-01-hello-world.md", """
            ---
            title: Hello World
            summary: A first post.
            publishDate: 2024-05-01
            ---

            # Hello World

            Body content here.
            """);

        WriteContent(dir, "projects", "01-my-project.md", """
            ---
            title: My Project
            summary: A neat project.
            ---

            # My Project

            Project body.
            """);

        return dir;
    }

    private static void WriteContent(string root, string folder, string filename, string content)
    {
        var dir = Path.Combine(root, "content", folder);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, filename), content);
    }

    private static void AssertExists(string root, string relativePath)
    {
        var full = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.That(File.Exists(full), Is.True, $"Expected artifact not found: {relativePath}");
    }

    #endregion
}
