using OutWit.Docs.Framework.Services;
using OutWit.Docs.Framework.Content;

namespace OutWit.Docs.Framework.Tests;

/// <summary>
/// Tests for MarkdownService.
/// </summary>
[TestFixture]
public class MarkdownServiceTests
{
    private MarkdownService m_service = null!;

    [SetUp]
    public void Setup()
    {
        m_service = new MarkdownService();
    }

    #region Code Highlighting Tests

    [Test]
    public void ToHtmlHighlightsFencedCodeTest()
    {
        var html = m_service.ToHtml("```csharp\nvar x = 1; // hi\n```");

        Assert.That(html, Does.Contain("class=\"code-block\""));
        Assert.That(html, Does.Contain("data-lang=\"csharp\""));
        Assert.That(html, Does.Contain("class=\"code-copy\""));
        Assert.That(html, Does.Contain("tok-keyword"));   // 'var'
        Assert.That(html, Does.Contain("tok-comment"));   // '// hi'
    }

    [Test]
    public void ToHtmlUnknownLanguageFallsBackToPlainTest()
    {
        var html = m_service.ToHtml("```bash\necho hello\n```");

        Assert.That(html, Does.Contain("class=\"code-block\""));
        Assert.That(html, Does.Contain("echo hello")); // plain, still wrapped + copy button
        Assert.That(html, Does.Contain("class=\"code-copy\""));
        Assert.That(html, Does.Not.Contain("tok-keyword"));
    }

    [Test]
    public void ToHtmlCodeBlockEscapesHtmlTest()
    {
        var html = m_service.ToHtml("```\n<script>alert(1)</script>\n```");

        // Code content must be escaped, never emitted as live markup.
        Assert.That(html, Does.Not.Contain("<script>alert(1)</script>"));
        Assert.That(html, Does.Contain("&lt;script&gt;"));
    }

    #endregion

    #region Image Figure Tests

    [Test]
    public void ToHtmlWrapsStandaloneImageInFigureTest()
    {
        var html = m_service.ToHtml("![The create table dialog](/images/studio/s-40.webp)");

        Assert.That(html, Does.Contain("<figure class=\"ow-figure\""));
        Assert.That(html, Does.Contain("class=\"ow-figure__zoom\""));
        Assert.That(html, Does.Contain("src=\"/images/studio/s-40.webp\""));
        Assert.That(html, Does.Contain("alt=\"The create table dialog\""));
        Assert.That(html, Does.Contain("loading=\"lazy\""));

        // The paragraph is replaced rather than wrapped: a figure inside a <p> is
        // markup the browser has to repair.
        Assert.That(html, Does.Not.Contain("<p>"));
    }

    [Test]
    public void ToHtmlImageTitleBecomesCaptionTest()
    {
        var html = m_service.ToHtml("![alt text](/img/a.webp \"What the picture shows\")");

        Assert.That(html, Does.Contain("<figcaption class=\"ow-figure__caption\">What the picture shows</figcaption>"));
        Assert.That(html, Does.Contain("alt=\"alt text\""));
    }

    [Test]
    public void ToHtmlImageWithoutTitleHasNoCaptionTest()
    {
        var html = m_service.ToHtml("![alt text](/img/a.webp)");

        Assert.That(html, Does.Contain("ow-figure"));
        Assert.That(html, Does.Not.Contain("figcaption"));
    }

    [Test]
    public void ToHtmlInlineImageStaysInlineTest()
    {
        var html = m_service.ToHtml("Build status ![badge](/img/badge.svg) as of today.");

        // An image used in a sentence is not a figure; it keeps flowing with the text.
        Assert.That(html, Does.Not.Contain("ow-figure"));
        Assert.That(html, Does.Contain("<p>"));
        Assert.That(html, Does.Contain("<img src=\"/img/badge.svg\""));
    }

    [Test]
    public void ToHtmlLinkedImageIsNotAFigureTest()
    {
        var html = m_service.ToHtml("[![badge](/img/badge.svg)](https://example.com)");

        // A button inside an anchor is not something a browser can be asked to make
        // sense of, so a linked image is left as the link the author wrote.
        Assert.That(html, Does.Not.Contain("ow-figure"));
        Assert.That(html, Does.Contain("<a href=\"https://example.com\""));
    }

    [Test]
    public void ToHtmlTwoImagesInOneParagraphAreNotFiguresTest()
    {
        var html = m_service.ToHtml("![one](/img/1.webp) ![two](/img/2.webp)");

        Assert.That(html, Does.Not.Contain("ow-figure"));
        Assert.That(html, Does.Contain("<img src=\"/img/1.webp\""));
        Assert.That(html, Does.Contain("<img src=\"/img/2.webp\""));
    }

    [Test]
    public void ToHtmlFigureEscapesAltAndCaptionTest()
    {
        var html = m_service.ToHtml("![\"><script>alert(1)</script>](/img/a.webp \"<b>caption</b>\")");

        Assert.That(html, Does.Not.Contain("<script>alert(1)</script>"));
        Assert.That(html, Does.Not.Contain("<b>caption</b>"));
        Assert.That(html, Does.Contain("&lt;"));
    }

    [Test]
    public void ToHtmlFigureFlattensEmphasisInAltTest()
    {
        var html = m_service.ToHtml("![a *bold* claim](/img/a.webp)");

        // An alt attribute cannot carry markup, so the words survive and the tags do not.
        Assert.That(html, Does.Contain("alt=\"a bold claim\""));
        Assert.That(html, Does.Not.Contain("<em>"));
    }

    [Test]
    public void ToHtmlFigureLabelsTheZoomButtonWithTheAltTest()
    {
        var html = m_service.ToHtml("![The Database tab](/img/a.webp)");

        Assert.That(html, Does.Contain("aria-label=\"Open the image at full size: The Database tab\""));
    }

    [Test]
    public void ToHtmlImageInListItemIsNotAFigureTest()
    {
        var html = m_service.ToHtml("- ![one](/img/1.webp)\n- ![two](/img/2.webp)");

        // A tight list item has no paragraph of its own; a figure there would turn a
        // list into a stack of blocks.
        Assert.That(html, Does.Not.Contain("ow-figure"));
        Assert.That(html, Does.Contain("<li>"));
    }

    [Test]
    public void ToHtmlKeepsHeadingsAndParagraphsAroundAFigureTest()
    {
        var html = m_service.ToHtml("## Creating a table\n\nBefore.\n\n![shot](/img/a.webp)\n\nAfter.");

        Assert.That(html, Does.Contain("<h2"));
        Assert.That(html, Does.Contain("<p>Before.</p>"));
        Assert.That(html, Does.Contain("ow-figure"));
        Assert.That(html, Does.Contain("<p>After.</p>"));
    }

    #endregion

    #region Raw HTML Policy Tests

    [Test]
    public void ToHtmlAllowsRawHtmlByDefaultTest()
    {
        // Arrange - default service allows raw HTML
        var markdown = "Hello <b>bold</b> and <script>alert(1)</script>";

        // Act
        var html = m_service.ToHtml(markdown);

        // Assert - raw HTML is passed through
        Assert.That(html, Does.Contain("<script>alert(1)</script>"));
    }

    [Test]
    public void ToHtmlWithRawHtmlDisabledStripsRawHtmlTest()
    {
        // Arrange - hardened service disables raw HTML
        var service = new MarkdownService(allowRawHtml: false);
        var markdown = "Hello <script>alert(1)</script>";

        // Act
        var html = service.ToHtml(markdown);

        // Assert - the script tag is escaped/neutralized, not emitted as live HTML
        Assert.That(html, Does.Not.Contain("<script>"));
        // Markdown emphasis still works (only raw HTML is disabled)
        Assert.That(service.ToHtml("**x**"), Does.Contain("<strong>x</strong>"));
    }

    [Test]
    public void ConfigureTogglesRawHtmlHandlingTest()
    {
        // Arrange
        var service = new MarkdownService();
        const string markdown = "<script>alert(1)</script>";

        // Act / Assert - reconfigure to strip, then back to allow
        service.Configure(allowRawHtml: false);
        Assert.That(service.ToHtml(markdown), Does.Not.Contain("<script>"));

        service.Configure(allowRawHtml: true);
        Assert.That(service.ToHtml(markdown), Does.Contain("<script>alert(1)</script>"));
    }

    #endregion

    #region ToHtml Tests

    [Test]
    public void ToHtml_SimpleText_ReturnsHtml()
    {
        // Arrange
        var markdown = "Hello **world**!";
        
        // Act
        var html = m_service.ToHtml(markdown);
        
        // Assert
        Assert.That(html, Does.Contain("<strong>world</strong>"));
        Assert.That(html, Does.Contain("<p>"));
    }

    [Test]
    public void ToHtml_Headings_ReturnsHtmlWithIds()
    {
        // Arrange
        var markdown = "# Hello World\n\n## Section Two";
        
        // Act
        var html = m_service.ToHtml(markdown);
        
        // Assert
        Assert.That(html, Does.Contain("<h1"));
        Assert.That(html, Does.Contain("<h2"));
        Assert.That(html, Does.Contain("id=")); // AutoIdentifiers extension
    }

    [Test]
    public void ToHtml_CodeBlock_PreservesCode()
    {
        // Arrange
        var markdown = """
            ```csharp
            var x = 1;
            ```
            """;
        
        // Act
        var html = m_service.ToHtml(markdown);
        
        // Assert - code is rendered in a highlighted code block (tokens are wrapped
        // in spans, so the source is no longer a single contiguous string).
        Assert.That(html, Does.Contain("<pre>"));
        Assert.That(html, Does.Contain("<code"));
        Assert.That(html, Does.Contain("tok-keyword")); // 'var' highlighted
    }

    [Test]
    public void ToHtml_Links_CreatesAnchors()
    {
        // Arrange
        var markdown = "Visit [GitHub](https://github.com)";
        
        // Act
        var html = m_service.ToHtml(markdown);
        
        // Assert
        Assert.That(html, Does.Contain("<a href=\"https://github.com\""));
        Assert.That(html, Does.Contain(">GitHub</a>"));
    }

    [Test]
    public void ToHtml_Lists_CreatesList()
    {
        // Arrange
        var markdown = "- Item 1\n- Item 2\n- Item 3";
        
        // Act
        var html = m_service.ToHtml(markdown);
        
        // Assert
        Assert.That(html, Does.Contain("<ul>"));
        Assert.That(html, Does.Contain("<li>Item 1</li>"));
    }

    [Test]
    public void ToHtml_TaskList_CreatesCheckboxes()
    {
        // Arrange
        var markdown = "- [x] Done\n- [ ] Todo";
        
        // Act
        var html = m_service.ToHtml(markdown);
        
        // Assert
        Assert.That(html, Does.Contain("type=\"checkbox\""));
        Assert.That(html, Does.Contain("checked"));
    }

    [Test]
    public void ToHtml_EmptyString_ReturnsEmpty()
    {
        // Act
        var html = m_service.ToHtml("");
        
        // Assert
        Assert.That(html, Is.EqualTo(""));
    }

    #endregion

    #region ParseWithFrontmatter Tests

    [Test]
    public void ParseWithFrontmatter_ValidYaml_ExtractsFrontmatter()
    {
        // Arrange
        var markdown = """
            ---
            title: Test Post
            description: A test description
            tags:
              - test
              - sample
            ---
            
            # Content Here
            
            Some text.
            """;
        
        // Act
        var (frontmatter, html) = m_service.ParseWithFrontmatter<FrontmatterData>(markdown);
        
        // Assert
        Assert.That(frontmatter, Is.Not.Null);
        Assert.That(frontmatter!.Title, Is.EqualTo("Test Post"));
        Assert.That(frontmatter.Description, Is.EqualTo("A test description"));
        Assert.That(frontmatter.Tags, Has.Count.EqualTo(2));
        Assert.That(frontmatter.Tags, Does.Contain("test"));
    }

    [Test]
    public void ParseWithFrontmatter_NoFrontmatter_ReturnsNullFrontmatter()
    {
        // Arrange
        var markdown = "# Just Content\n\nNo frontmatter here.";
        
        // Act
        var (frontmatter, html) = m_service.ParseWithFrontmatter<FrontmatterData>(markdown);
        
        // Assert
        Assert.That(frontmatter, Is.Null);
        Assert.That(html, Does.Contain("<h1"));
    }

    [Test]
    public void ParseWithFrontmatter_WithDate_ParsesDate()
    {
        // Arrange
        var markdown = """
            ---
            title: Blog Post
            publishDate: 2024-01-15
            ---
            
            Content
            """;
        
        // Act
        var (frontmatter, _) = m_service.ParseWithFrontmatter<FrontmatterData>(markdown);
        
        // Assert
        Assert.That(frontmatter, Is.Not.Null);
        Assert.That(frontmatter!.PublishDate, Is.EqualTo(new DateTime(2024, 1, 15)));
    }

    [Test]
    public void ParseWithFrontmatter_HtmlContainsContent()
    {
        // Arrange
        var markdown = """
            ---
            title: Test
            ---
            
            ## Hello
            
            Paragraph text.
            """;
        
        // Act
        var (_, html) = m_service.ParseWithFrontmatter<FrontmatterData>(markdown);
        
        // Assert
        Assert.That(html, Does.Contain("<h2"));
        Assert.That(html, Does.Contain("Paragraph text."));
    }

    #endregion

    #region CalculateReadingTime Tests

    [Test]
    public void CalculateReadingTime_ShortText_ReturnsOneMinute()
    {
        // Arrange
        var markdown = "Hello world."; // 2 words
        
        // Act
        var time = m_service.CalculateReadingTime(markdown);
        
        // Assert
        Assert.That(time, Is.EqualTo(1)); // Minimum 1 minute
    }

    [Test]
    public void CalculateReadingTime_LongText_CalculatesCorrectly()
    {
        // Arrange - 400 words at 200 wpm = 2 minutes
        var words = string.Join(" ", Enumerable.Repeat("word", 400));
        
        // Act
        var time = m_service.CalculateReadingTime(words, 200);
        
        // Assert
        Assert.That(time, Is.EqualTo(2));
    }

    [Test]
    public void CalculateReadingTime_WithMarkdown_StripsFormatting()
    {
        // Arrange
        var markdown = "**Bold** and *italic* and `code` with [link](url)";
        
        // Act
        var time = m_service.CalculateReadingTime(markdown);
        
        // Assert
        // Should still calculate based on actual words
        Assert.That(time, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void CalculateReadingTime_CustomWordsPerMinute()
    {
        // Arrange - 300 words at 100 wpm = 3 minutes
        var words = string.Join(" ", Enumerable.Repeat("word", 300));
        
        // Act
        var time = m_service.CalculateReadingTime(words, 100);
        
        // Assert
        Assert.That(time, Is.EqualTo(3));
    }

    #endregion

    #region ExtractPlainText Tests

    [Test]
    public void ExtractPlainText_RemovesFrontmatter()
    {
        // Arrange
        var markdown = """
            ---
            title: Test
            ---
            
            Actual content here.
            """;
        
        // Act
        var text = m_service.ExtractPlainText(markdown);
        
        // Assert
        Assert.That(text, Does.Not.Contain("title:"));
        Assert.That(text, Does.Not.Contain("---"));
        Assert.That(text, Does.Contain("Actual content here"));
    }

    [Test]
    public void ExtractPlainText_RemovesMarkdownSyntax()
    {
        // Arrange
        var markdown = "**Bold** and *italic* and `code`";
        
        // Act
        var text = m_service.ExtractPlainText(markdown);
        
        // Assert
        Assert.That(text, Does.Not.Contain("**"));
        Assert.That(text, Does.Not.Contain("*"));
        Assert.That(text, Does.Not.Contain("`"));
    }

    [Test]
    public void ExtractPlainText_NormalizesWhitespace()
    {
        // Arrange
        var markdown = "Hello    world\n\n\ntest";
        
        // Act
        var text = m_service.ExtractPlainText(markdown);
        
        // Assert
        Assert.That(text, Does.Not.Contain("  ")); // No double spaces
    }

    #endregion

    #region ExtractTableOfContents Tests

    [Test]
    public void ExtractTableOfContents_SimpleHeadings_ReturnsToc()
    {
        // Arrange
        var markdown = """
            # Heading 1
            
            Content
            
            ## Heading 2
            
            More content
            
            ## Heading 3
            """;
        
        // Act
        var toc = m_service.ExtractTableOfContents(markdown);
        
        // Assert
        Assert.That(toc, Has.Count.EqualTo(1)); // One H1
        Assert.That(toc[0].Title, Is.EqualTo("Heading 1"));
        Assert.That(toc[0].Children, Has.Count.EqualTo(2)); // Two H2s under it
    }

    [Test]
    public void ExtractTableOfContents_GeneratesIds()
    {
        // Arrange
        var markdown = "# Hello World";
        
        // Act
        var toc = m_service.ExtractTableOfContents(markdown);
        
        // Assert
        Assert.That(toc, Has.Count.EqualTo(1));
        Assert.That(toc[0].Id, Is.EqualTo("hello-world"));
    }

    [Test]
    public void ExtractTableOfContents_NoHeadings_ReturnsEmptyList()
    {
        // Arrange
        var markdown = "Just some text without any headings.";
        
        // Act
        var toc = m_service.ExtractTableOfContents(markdown);
        
        // Assert
        Assert.That(toc, Is.Empty);
    }

    [Test]
    public void ExtractTableOfContents_NestedHeadings_BuildsHierarchy()
    {
        // Arrange
        var markdown = """
            # Chapter 1
            ## Section 1.1
            ### Subsection 1.1.1
            ## Section 1.2
            # Chapter 2
            ## Section 2.1
            """;
        
        // Act
        var toc = m_service.ExtractTableOfContents(markdown);
        
        // Assert
        Assert.That(toc, Has.Count.EqualTo(2)); // Two H1s
        Assert.That(toc[0].Title, Is.EqualTo("Chapter 1"));
        Assert.That(toc[0].Children, Has.Count.EqualTo(2)); // Two H2s
        Assert.That(toc[0].Children[0].Children, Has.Count.EqualTo(1)); // One H3
        Assert.That(toc[1].Title, Is.EqualTo("Chapter 2"));
    }

    #endregion
}
