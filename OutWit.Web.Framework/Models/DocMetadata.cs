using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Web.Framework.Models;

/// <summary>
/// Lightweight documentation metadata for list rendering.
/// </summary>
public class DocMetadata : ModelBase
{
    #region Model Base

    public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
    {
        if (modelBase is not DocMetadata other)
            return false;

        return Slug.Is(other.Slug)
               && Title.Is(other.Title)
               && Description.Is(other.Description)
               && Order.Is(other.Order)
               && ParentSlug.Is(other.ParentSlug);
    }

    public override DocMetadata Clone()
    {
        return new DocMetadata
        {
            Slug = Slug,
            Title = Title,
            Description = Description,
            Order = Order,
            ParentSlug = ParentSlug
        };
    }

    #endregion

    #region Properties

    public string Slug { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int Order { get; set; }

    public string ParentSlug { get; set; } = string.Empty;

    #endregion
}
