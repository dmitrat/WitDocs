using System.Net;
using System.Text;
using OutWit.Docs.Framework.Services;

namespace OutWit.Docs.Framework.Tests.Services;

/// <summary>
/// Tests for ContentService using a fake HttpClient that serves content files
/// from an in-memory route table.
/// </summary>
[TestFixture]
public class ContentServiceTests
{
    #region Fake Http

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, string> m_routes;

        public FakeHttpMessageHandler(Dictionary<string, string> routes)
        {
            m_routes = routes;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath.TrimStart('/');
            if (m_routes.TryGetValue(path, out var content))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(content, Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    private static ContentService CreateService(Dictionary<string, string> routes)
    {
        var http = new HttpClient(new FakeHttpMessageHandler(routes))
        {
            BaseAddress = new Uri("http://localhost/")
        };
        return new ContentService(http, new MarkdownService(), new ContentParser());
    }

    #endregion

    #region Content Index Tests

    [Test]
    public async Task GetContentIndexReturnsEmptyWhenMissingTest()
    {
        var service = CreateService(new Dictionary<string, string>());

        var index = await service.GetContentIndexAsync();

        Assert.That(index, Is.Not.Null);
        Assert.That(index.Blog, Is.Empty);
        Assert.That(index.Docs, Is.Empty);
    }

    #endregion

    #region Blog Tests

    [Test]
    public async Task GetBlogPostsReturnsPostsSortedByDateDescendingTest()
    {
        var routes = new Dictionary<string, string>
        {
            ["content/index.json"] = """{"blog":["2024-01-01-old.md","2024-05-01-new.md"],"projects":[],"articles":[],"docs":[],"features":[],"sections":{}}""",
            ["content/blog/2024-01-01-old.md"] = "---\ntitle: Old\npublishDate: 2024-01-01\n---\n\nOld body.",
            ["content/blog/2024-05-01-new.md"] = "---\ntitle: New\npublishDate: 2024-05-01\n---\n\nNew body.",
        };
        var service = CreateService(routes);

        var posts = await service.GetBlogPostsAsync();

        Assert.That(posts.Count, Is.EqualTo(2));
        Assert.That(posts[0].Title, Is.EqualTo("New")); // newest first
        Assert.That(posts[1].Title, Is.EqualTo("Old"));
        Assert.That(posts[0].HtmlContent, Does.Contain("New body"));
    }

    [Test]
    public async Task GetBlogPostsCachesResultTest()
    {
        var routes = new Dictionary<string, string>
        {
            ["content/index.json"] = """{"blog":["a.md"],"projects":[],"articles":[],"docs":[],"features":[],"sections":{}}""",
            ["content/blog/a.md"] = "---\ntitle: A\npublishDate: 2024-01-01\n---\n\nBody.",
        };
        var service = CreateService(routes);

        var first = await service.GetBlogPostsAsync();
        var second = await service.GetBlogPostsAsync();

        Assert.That(second, Is.SameAs(first)); // cached instance returned
    }

    [Test]
    public async Task GetBlogPostsSkipsFileWithoutFrontmatterTest()
    {
        var routes = new Dictionary<string, string>
        {
            ["content/index.json"] = """{"blog":["good.md","bad.md"],"projects":[],"articles":[],"docs":[],"features":[],"sections":{}}""",
            ["content/blog/good.md"] = "---\ntitle: Good\npublishDate: 2024-01-01\n---\n\nBody.",
            ["content/blog/bad.md"] = "No frontmatter, just text.",
        };
        var service = CreateService(routes);

        var posts = await service.GetBlogPostsAsync();

        Assert.That(posts.Count, Is.EqualTo(1));
        Assert.That(posts[0].Title, Is.EqualTo("Good"));
    }

    [Test]
    public async Task GetBlogPostBySlugReturnsMatchingPostTest()
    {
        var routes = new Dictionary<string, string>
        {
            ["content/index.json"] = """{"blog":["2024-05-01-hello-world.md"],"projects":[],"articles":[],"docs":[],"features":[],"sections":{}}""",
            ["content/blog/2024-05-01-hello-world.md"] = "---\ntitle: Hello\npublishDate: 2024-05-01\n---\n\nBody.",
        };
        var service = CreateService(routes);

        var post = await service.GetBlogPostAsync("hello-world");

        Assert.That(post, Is.Not.Null);
        Assert.That(post!.Title, Is.EqualTo("Hello"));
    }

    #endregion

    #region Docs Tests

    [Test]
    public async Task GetDocsBuildsPreviousNextNavigationTest()
    {
        var routes = new Dictionary<string, string>
        {
            ["content/index.json"] = """{"blog":[],"projects":[],"articles":[],"docs":["01-intro.md","02-setup.md","03-usage.md"],"features":[],"sections":{}}""",
            ["content/docs/01-intro.md"] = "---\ntitle: Intro\n---\n\nIntro.",
            ["content/docs/02-setup.md"] = "---\ntitle: Setup\n---\n\nSetup.",
            ["content/docs/03-usage.md"] = "---\ntitle: Usage\n---\n\nUsage.",
        };
        var service = CreateService(routes);

        var docs = await service.GetDocsAsync();

        Assert.That(docs.Count, Is.EqualTo(3));
        Assert.That(docs[0].PreviousPage, Is.Null);
        Assert.That(docs[0].NextPage, Is.Not.Null);
        Assert.That(docs[1].PreviousPage, Is.Not.Null);
        Assert.That(docs[1].NextPage, Is.Not.Null);
        Assert.That(docs[2].NextPage, Is.Null);
    }

    [Test]
    public async Task GetDocsCachesResultTest()
    {
        var routes = new Dictionary<string, string>
        {
            ["content/index.json"] = """{"blog":[],"projects":[],"articles":[],"docs":["01-intro.md"],"features":[],"sections":{}}""",
            ["content/docs/01-intro.md"] = "---\ntitle: Intro\n---\n\nIntro.",
        };
        var service = CreateService(routes);

        var first = await service.GetDocsAsync();
        var second = await service.GetDocsAsync();

        Assert.That(second.Count, Is.EqualTo(first.Count));
        Assert.That(second[0].Slug, Is.EqualTo(first[0].Slug));
    }

    #endregion
}
