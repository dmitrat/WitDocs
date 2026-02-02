using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Web.Framework.Models;

/// <summary>
/// Lightweight feature metadata for list rendering.
/// </summary>
public class FeatureMetadata : ModelBase
{
    #region Model Base

    public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
    {
        if (modelBase is not FeatureMetadata other)
            return false;

        return Slug.Is(other.Slug)
               && Title.Is(other.Title)
               && Description.Is(other.Description)
               && Order.Is(other.Order)
               && Icon.Is(other.Icon)
               && IconSvg.Is(other.IconSvg);
    }

    public override FeatureMetadata Clone()
    {
        return new FeatureMetadata
        {
            Slug = Slug,
            Title = Title,
            Description = Description,
            Order = Order,
            Icon = Icon,
            IconSvg = IconSvg
        };
    }

    #endregion

    #region Properties

    public string Slug { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int Order { get; set; }

    public string Icon { get; set; } = string.Empty;

    public string IconSvg { get; set; } = string.Empty;

    #endregion
}
