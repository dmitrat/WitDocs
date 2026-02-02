using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Values;

namespace OutWit.Web.Framework.Models;

/// <summary>
/// Lightweight blog post metadata for list rendering.
/// </summary>
public class BlogPostMetadata : ModelBase
{
    #region Model Base

    public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
    {
        if (modelBase is not BlogPostMetadata other)
            return false;

        return Slug.Is(other.Slug)
               && Title.Is(other.Title)
               && Description.Is(other.Description)
               && Summary.Is(other.Summary)
               && PublishDate.Is(other.PublishDate)
               && Author.Is(other.Author)
               && Tags.Is(other.Tags)
               && ReadingTimeMinutes.Is(other.ReadingTimeMinutes)
               && FeaturedImage.Is(other.FeaturedImage);
    }

    public override BlogPostMetadata Clone()
    {
        return new BlogPostMetadata
        {
            Slug = Slug,
            Title = Title,
            Description = Description,
            Summary = Summary,
            PublishDate = PublishDate,
            Author = Author,
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

    public string Author { get; set; } = string.Empty;

    public List<string> Tags { get; set; } = [];

    public int ReadingTimeMinutes { get; set; }

    public string FeaturedImage { get; set; } = string.Empty;

    #endregion
}
