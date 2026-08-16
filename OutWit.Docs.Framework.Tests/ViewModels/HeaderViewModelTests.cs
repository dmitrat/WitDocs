using OutWit.Docs.Framework.Configuration;
using OutWit.Docs.Framework.Models;
using OutWit.Docs.Framework.ViewModels.Layout;

namespace OutWit.Docs.Framework.Tests.ViewModels;

/// <summary>
/// Tests for the navigation highlighting rules in <see cref="HeaderViewModel"/>.
/// </summary>
[TestFixture]
public class HeaderViewModelTests
{
    #region Match Tests

    [Test]
    public void IsMatchExactPathIsActiveTest()
    {
        Assert.That(HeaderViewModel.IsMatch("/witsql", "/witsql"), Is.True);
    }

    [Test]
    public void IsMatchDescendantPathIsActiveTest()
    {
        Assert.That(HeaderViewModel.IsMatch("/witsql/functions", "/witsql"), Is.True);
    }

    [Test]
    public void IsMatchIgnoresTrailingSlashesTest()
    {
        Assert.That(HeaderViewModel.IsMatch("/witsql/", "/witsql"), Is.True);
        Assert.That(HeaderViewModel.IsMatch("/witsql", "/witsql/"), Is.True);
    }

    [Test]
    public void IsMatchIgnoresQueryAndFragmentTest()
    {
        Assert.That(HeaderViewModel.IsMatch("/search?query=index", "/search"), Is.True);
        Assert.That(HeaderViewModel.IsMatch("/witsql/types#rowid", "/witsql"), Is.True);
    }

    [Test]
    public void IsMatchStopsAtSegmentBoundaryTest()
    {
        Assert.That(HeaderViewModel.IsMatch("/blogroll", "/blog"), Is.False);
        Assert.That(HeaderViewModel.IsMatch("/quick-starter", "/quick-start"), Is.False);
    }

    [Test]
    public void IsMatchRootIsActiveOnlyOnRootTest()
    {
        Assert.That(HeaderViewModel.IsMatch("/", "/"), Is.True);
        Assert.That(HeaderViewModel.IsMatch("", "/"), Is.True);
        Assert.That(HeaderViewModel.IsMatch("/blog", "/"), Is.False);
    }

    [Test]
    public void IsMatchEmptyHrefIsNeverActiveTest()
    {
        Assert.That(HeaderViewModel.IsMatch("/witsql", ""), Is.False);
    }

    #endregion

    #region Most Specific Match Tests

    [Test]
    public void IsMostSpecificMatchHighlightsExactlyOneEntryTest()
    {
        // The reported bug: on /quick-start/first-database the menu showed two
        // entries selected, because "Introduction" is served at the bare section
        // route and matched by prefix. Exactly one entry may be active.
        var section = Section();

        var active = section
            .Where(item => HeaderViewModel.IsMostSpecificMatch("/quick-start/first-database", section, item))
            .ToList();

        Assert.That(active, Has.Count.EqualTo(1));
        Assert.That(active[0].Title, Is.EqualTo("First database"));
    }

    [Test]
    public void IsMostSpecificMatchPrefersTheLongerSiblingTest()
    {
        // A landing section serves its lead article at the bare section route, so
        // "Overview" matches every page below it. Only the deeper entry lights up.
        var section = Section();
        var overview = section[0];
        var installation = section[1];

        Assert.That(HeaderViewModel.IsMostSpecificMatch("/quick-start/installation", section, overview), Is.False);
        Assert.That(HeaderViewModel.IsMostSpecificMatch("/quick-start/installation", section, installation), Is.True);
    }

    [Test]
    public void IsMostSpecificMatchKeepsTheLeadOnTheSectionRootTest()
    {
        var section = Section();

        Assert.That(HeaderViewModel.IsMostSpecificMatch("/quick-start", section, section[0]), Is.True);
        Assert.That(HeaderViewModel.IsMostSpecificMatch("/quick-start", section, section[1]), Is.False);
    }

    [Test]
    public void IsMostSpecificMatchIgnoresSiblingsOnAnotherPathTest()
    {
        var section = Section();

        Assert.That(HeaderViewModel.IsMostSpecificMatch("/witsql/types", section, section[0]), Is.False);
    }

    [Test]
    public void IsMostSpecificMatchWithoutSiblingsFallsBackToTheMatchTest()
    {
        var child = new NavItem { Title = "Blog", Href = "/blog" };

        Assert.That(HeaderViewModel.IsMostSpecificMatch("/blog/some-post", null, child), Is.True);
    }

    #endregion

    #region Collapse Breakpoint Tests

    [Test]
    public void ResolveCollapseBreakpointFallsBackToTheDefaultTest()
    {
        Assert.That(HeaderViewModel.ResolveCollapseBreakpoint(null),
            Is.EqualTo(HeaderConfig.DEFAULT_COLLAPSE_BREAKPOINT));

        Assert.That(HeaderViewModel.ResolveCollapseBreakpoint(new SiteConfig()),
            Is.EqualTo(HeaderConfig.DEFAULT_COLLAPSE_BREAKPOINT));
    }

    [Test]
    public void ResolveCollapseBreakpointTakesTheConfiguredWidthTest()
    {
        var config = new SiteConfig { Header = new HeaderConfig { CollapseBreakpoint = 780 } };

        Assert.That(HeaderViewModel.ResolveCollapseBreakpoint(config), Is.EqualTo(780));
    }

    [Test]
    public void ResolveCollapseBreakpointClampsBothEndsTest()
    {
        var tooNarrow = new SiteConfig { Header = new HeaderConfig { CollapseBreakpoint = 10 } };
        var tooWide = new SiteConfig { Header = new HeaderConfig { CollapseBreakpoint = 9000 } };

        Assert.That(HeaderViewModel.ResolveCollapseBreakpoint(tooNarrow),
            Is.EqualTo(HeaderConfig.MIN_COLLAPSE_BREAKPOINT));
        Assert.That(HeaderViewModel.ResolveCollapseBreakpoint(tooWide),
            Is.EqualTo(HeaderConfig.MAX_COLLAPSE_BREAKPOINT));
    }

    [Test]
    public void BuildResponsiveCssDerivesTheLadderFromOneWidthTest()
    {
        var css = HeaderViewModel.BuildResponsiveCss(1000);

        // Tighten 300 above, drop the search 150 above, collapse at the width itself.
        Assert.That(css, Does.Contain("@media (max-width:1300px)"));
        Assert.That(css, Does.Contain("@media (max-width:1150px)"));
        Assert.That(css, Does.Contain("@media (max-width:1000px)"));
    }

    [Test]
    public void BuildResponsiveCssCollapsesTheNavAndOpensTheMenuTest()
    {
        var css = HeaderViewModel.BuildResponsiveCss(820);

        // The panel's visibility lives in outwit-framework.css at 768px, so the
        // collapse step has to turn it on or the toggle would open nothing.
        Assert.That(css, Does.Contain("@media (max-width:820px)"));
        Assert.That(css, Does.Contain(".header__nav{display:none}"));
        Assert.That(css, Does.Contain(".header__mobile-toggle{display:block}"));
        Assert.That(css, Does.Contain(".header__mobile-menu{display:block}"));
    }

    [Test]
    public void BuildResponsiveCssIsAStyleElementTest()
    {
        var css = HeaderViewModel.BuildResponsiveCss(1000);

        Assert.That(css, Does.StartWith("<style>"));
        Assert.That(css, Does.EndWith("</style>"));
    }

    #endregion

    #region Tools

    private static List<NavItem> Section()
    {
        return
        [
            new NavItem { Title = "Introduction", Href = "/quick-start" },
            new NavItem { Title = "Installation", Href = "/quick-start/installation" },
            new NavItem { Title = "First database", Href = "/quick-start/first-database" }
        ];
    }

    #endregion
}
