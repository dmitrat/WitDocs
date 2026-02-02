using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Values;

namespace OutWit.Web.Framework.Models;

/// <summary>
/// Unified blog post item for list rendering.
/// Works with both full BlogPost and BlogPostMetadata.
/// </summary>
public class BlogPostListItem : ModelBase
{
    #region Model Base

    public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
    {
        if (modelBase is not BlogPostListItem other)
            return false;

        return Slug.Is(other.Slug)
               && Title.Is(other.Title)
               && Description.Is(other.Description)
               && Summary.Is(other.Summary)
               && PublishDate.Is(other.PublishDate)
               && Tags.Is(other.Tags)
               && ReadingTimeMinutes.Is(other.ReadingTimeMinutes)
               && FeaturedImage.Is(other.FeaturedImage);
    }

    public override BlogPostListItem Clone()
    {
        return new BlogPostListItem
        {
            Slug = Slug,
            Title = Title,
            Description = Description,
            Summary = Summary,
            PublishDate = PublishDate,
            Tags = Tags.ToList(),
            ReadingTimeMinutes = ReadingTimeMinutes,
            FeaturedImage = FeaturedImage
        };
    }

    #endregion

    #region Properties

    public string Slug { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public DateTime PublishDate { get; set; }

    public List<string> Tags { get; set; } = [];

    public int ReadingTimeMinutes { get; set; }

    public string FeaturedImage { get; set; } = string.Empty;

    #endregion
}
