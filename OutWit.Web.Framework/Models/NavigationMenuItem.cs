using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Web.Framework.Models;

/// <summary>
/// Lightweight menu item for navigation.
/// Contains only the data needed for menu rendering.
/// </summary>
public class NavigationMenuItem : ModelBase
{
    #region Model Base

    public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
    {
        if (modelBase is not NavigationMenuItem other)
            return false;

        return Slug.Is(other.Slug)
               && Title.Is(other.Title)
               && MenuTitle.Is(other.MenuTitle)
               && Order.Is(other.Order)
               && ShowInMenu.Is(other.ShowInMenu)
               && ShowInHeader.Is(other.ShowInHeader);
    }

    public override NavigationMenuItem Clone()
    {
        return new NavigationMenuItem
        {
            Slug = Slug,
            Title = Title,
            MenuTitle = MenuTitle,
            Order = Order,
            ShowInMenu = ShowInMenu,
            ShowInHeader = ShowInHeader
        };
    }

    #endregion

    #region Properties

    /// <summary>
    /// URL slug.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Full title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Short title for menu (optional).
    /// </summary>
    public string? MenuTitle { get; set; }

    /// <summary>
    /// Sort order.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Whether to show in dropdown menus.
    /// </summary>
    public bool ShowInMenu { get; set; } = true;

    /// <summary>
    /// Whether to show as top-level header item (projects only).
    /// </summary>
    public bool ShowInHeader { get; set; }

    /// <summary>
    /// Get display title (MenuTitle if set, otherwise Title).
    /// </summary>
    public string DisplayTitle => !string.IsNullOrEmpty(MenuTitle) ? MenuTitle : Title;

    #endregion
}
