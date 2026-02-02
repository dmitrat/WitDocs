using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Values;

namespace OutWit.Web.Framework.Models;

/// <summary>
/// Lightweight project metadata for list rendering.
/// </summary>
public class ProjectMetadata : ModelBase
{
    #region Model Base

    public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
    {
        if (modelBase is not ProjectMetadata other)
            return false;

        return Slug.Is(other.Slug)
               && Title.Is(other.Title)
               && Description.Is(other.Description)
               && Summary.Is(other.Summary)
               && Order.Is(other.Order)
               && Tags.Is(other.Tags)
               && Url.Is(other.Url);
    }

    public override ProjectMetadata Clone()
    {
        return new ProjectMetadata
        {
            Slug = Slug,
            Title = Title,
            Description = Description,
            Summary = Summary,
            Order = Order,
            Tags = Tags.ToList(),
            Url = Url
        };
    }

    #endregion

    #region Properties

    public string Slug { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public int Order { get; set; }

    public List<string> Tags { get; set; } = [];

    public string Url { get; set; } = string.Empty;

    #endregion
}
