using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Docs.Framework.Configuration;

/// <summary>
/// Header configuration loaded from site.config.json.
/// </summary>
public class HeaderConfig : ModelBase
{
    #region Constants

    /// <summary>
    /// Width below which the navigation is replaced by the mobile menu when the
    /// site does not say otherwise. Chosen for a menu of six to eight entries.
    /// </summary>
    public const int DEFAULT_COLLAPSE_BREAKPOINT = 1000;

    /// <summary>
    /// Narrowest breakpoint that is worth honouring. Below this the mobile menu
    /// would be taking over on a phone only, which is what the old fixed 768px
    /// rule already did.
    /// </summary>
    public const int MIN_COLLAPSE_BREAKPOINT = 480;

    /// <summary>
    /// Widest breakpoint that is worth honouring. A site asking for more than
    /// this has a menu that will not fit on a laptop at all.
    /// </summary>
    public const int MAX_COLLAPSE_BREAKPOINT = 1600;

    #endregion

    #region Model Base

    public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
    {
        if (modelBase is not HeaderConfig other)
            return false;

        return CollapseBreakpoint.Is(other.CollapseBreakpoint);
    }

    public override HeaderConfig Clone()
    {
        return new HeaderConfig
        {
            CollapseBreakpoint = CollapseBreakpoint
        };
    }

    #endregion

    #region Properties

    /// <summary>
    /// Viewport width in pixels below which the navigation is replaced by the
    /// mobile menu. How much room the row needs depends on how many entries the
    /// site has and how long their titles are, so this is a per-site number
    /// rather than one constant for everybody: eight entries with dropdowns run
    /// out of room around 1000px, three short ones hold on past 700px.
    ///
    /// Two further steps are derived from it, so one number configures the whole
    /// ladder: the row tightens 300px above it and the search field is dropped
    /// 150px above it.
    ///
    /// Clamped to <see cref="MIN_COLLAPSE_BREAKPOINT"/>..<see cref="MAX_COLLAPSE_BREAKPOINT"/>.
    /// </summary>
    public int CollapseBreakpoint { get; set; } = DEFAULT_COLLAPSE_BREAKPOINT;

    #endregion
}
