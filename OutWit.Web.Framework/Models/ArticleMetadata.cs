using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Values;

namespace OutWit.Web.Framework.Models;

/// <summary>
/// Lightweight article metadata for list rendering.
/// </summary>
public class ArticleMetadata : ModelBase
{
    #region Model Base

    public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
    {
        if (modelBase is not ArticleMetadata other)
            return false;

        return Slug.Is(other.Slug)
               && Title.Is(other.Title)
               && Description.Is(other.Description)
               && Order.Is(other.Order)
               && Tags.Is(other.Tags)
               && PublishDate.Is(other.PublishDate);
    }

    public override ArticleMetadata Clone()
    {
        return new ArticleMetadata
        {
            Slug = Slug,
            Title = Title,
            Description = Description,
            Order = Order,
            Tags = Tags.ToList(),
            PublishDate = PublishDate
        };
    }

    #endregion

    #region Properties

    public string Slug { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int Order { get; set; }

    public List<string> Tags { get; set; } = [];

    public DateTime PublishDate { get; set; }

    #endregion
}
