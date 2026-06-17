using OutWit.Docs.Framework.Services;

namespace OutWit.Docs.Framework.Tests.Services;

/// <summary>
/// Tests for ContentParser.StripComponentsForStaticHtml (SSG degradation).
/// </summary>
[TestFixture]
public class ContentParserStripTests
{
    private ContentParser m_parser = null!;

    [SetUp]
    public void Setup()
    {
        m_parser = new ContentParser();
    }

    [Test]
    public void StripComponentsKeepsBlockInnerContentTest()
    {
        var input = "Before [[Note type=\"info\"]]Important text[[/Note]] after.";

        var result = m_parser.StripComponentsForStaticHtml(input);

        Assert.That(result, Does.Contain("Important text"));
        Assert.That(result, Does.Not.Contain("[[Note"));
        Assert.That(result, Does.Not.Contain("[[/Note]]"));
    }

    [Test]
    public void StripComponentsRemovesSelfClosingTest()
    {
        var input = "Before [[YouTube videoId=\"abc\"/]] after.";

        var result = m_parser.StripComponentsForStaticHtml(input);

        Assert.That(result, Does.Not.Contain("[[YouTube"));
        Assert.That(result, Does.Contain("Before"));
        Assert.That(result, Does.Contain("after."));
    }

    [Test]
    public void StripComponentsLeavesPlainMarkdownUntouchedTest()
    {
        var input = "# Title\n\nA paragraph with **bold** and a [link](https://x).";

        var result = m_parser.StripComponentsForStaticHtml(input);

        Assert.That(result, Is.EqualTo(input));
    }
}
