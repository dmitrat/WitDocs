using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Docs.Framework.Configuration;

/// <summary>
/// Configuration for a dynamic content section.
/// Allows defining custom content sections like "solutions", "products", etc.
/// that behave like docs/articles but with custom folder and route names.
/// </summary>
public class ContentSectionConfig : ModelBase
{
    #region Model Base

    public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
    {
        if (modelBase is not ContentSectionConfig other)
            return false;

        return Folder.Is(other.Folder)
            && Route.Is(other.Route)
            && MenuTitle.Is(other.MenuTitle)
            && Type.Is(other.Type)
            && LandingPage.Is(other.LandingPage);
    }

    public override ContentSectionConfig Clone()
    {
        return new ContentSectionConfig
        {
            Folder = Folder,
            Route = Route,
            MenuTitle = MenuTitle,
            Type = Type,
            LandingPage = LandingPage
        };
    }

    #endregion

    #region Properties

    /// <summary>
    /// Physical folder name in content/ directory.
    /// Example: "solutions"
    /// </summary>
    public string Folder { get; set; } = "";

    /// <summary>
    /// URL route prefix for this section.
    /// Example: "solutions" → /solutions/{slug}
    /// </summary>
    public string Route { get; set; } = "";

    /// <summary>
    /// Display name for menu/navigation.
    /// Example: "Solutions"
    /// </summary>
    public string MenuTitle { get; set; } = "";

    /// <summary>
    /// Content type: "article" (with TOC) or "doc" (with prev/next navigation).
    /// Default: "article"
    /// </summary>
    public string Type { get; set; } = "article";

    /// <summary>
    /// When true, the section's lead (first) page is served at the short section
    /// route itself (<c>/{route}/</c>) instead of <c>/{route}/{lead-slug}/</c>, and
    /// no separate listing page is generated for the root. Remaining pages keep
    /// <c>/{route}/{slug}/</c>. This gives each page exactly one canonical URL and
    /// makes the human-navigable paths match the sitemap. Default: false (the root
    /// is a card listing and every page lives under <c>/{route}/{slug}/</c>).
    /// </summary>
    public bool LandingPage { get; set; } = false;

    #endregion
}
