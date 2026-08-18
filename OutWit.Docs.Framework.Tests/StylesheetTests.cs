using System.Text.RegularExpressions;

namespace OutWit.Docs.Framework.Tests;

/// <summary>
/// Checks on the shipped stylesheet, for rules whose absence is a defect no C# test
/// would notice. There is no browser in this suite, so the contract is asserted
/// against the text of the sheet the package carries.
/// </summary>
[TestFixture]
public class StylesheetTests
{
    #region Fields

    private string m_css = null!;

    #endregion

    #region Initialization

    [SetUp]
    public void Setup()
    {
        m_css = File.ReadAllText(Path.Combine(FrameworkRoot(), "wwwroot", "css", "outwit-framework.css"));
    }

    /// <summary>
    /// The framework project directory, found by walking up from the test binary.
    /// </summary>
    private static string FrameworkRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "OutWit.Docs.Framework");
            if (Directory.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("OutWit.Docs.Framework was not found above the test binary.");
    }

    #endregion

    #region Lightbox Tests

    [Test]
    public void LightboxIsHiddenWhenClosedTest()
    {
        // The overlay is laid out with `display: flex`, which outranks the browser's own
        // `[hidden] { display: none }`. Without a rule of its own the closed overlay stays
        // over the whole page at opacity 0 and swallows every click on the article.
        Assert.That(m_css, Does.Match(@"\.ow-lightbox\[hidden\]\s*\{[^}]*display:\s*none"),
            "A closed .ow-lightbox must be display:none, or it covers the page and eats clicks.");
    }

    [Test]
    public void LightboxCoversTheViewportWhenOpenTest()
    {
        var rule = Regex.Match(m_css, @"\.ow-lightbox\s*\{(?<body>[^}]*)\}");

        Assert.That(rule.Success, Is.True, "The .ow-lightbox rule is missing.");
        Assert.That(rule.Groups["body"].Value, Does.Contain("position: fixed"));
        Assert.That(rule.Groups["body"].Value, Does.Contain("z-index: var(--z-modal)"));
    }

    #endregion

    #region Figure Tests

    [Test]
    public void FigureImageFillsItsColumnTest()
    {
        Assert.That(m_css, Does.Match(@"\.ow-figure img\s*\{[^}]*width:\s*100%"));
    }

    [Test]
    public void ZoomButtonLooksClickableTest()
    {
        // The button carries no visible chrome of its own, so the cursor is the only
        // thing telling a reader the picture opens.
        Assert.That(m_css, Does.Match(@"\.ow-figure__zoom\s*\{[^}]*cursor:\s*zoom-in"));
    }

    #endregion
}
