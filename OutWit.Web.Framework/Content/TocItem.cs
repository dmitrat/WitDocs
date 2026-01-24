using OutWit.Common.Abstract;
using OutWit.Common.Attributes;
using OutWit.Common.Collections;
using OutWit.Common.Values;

namespace OutWit.Web.Framework.Content;

/// <summary>
/// Table of contents item representing a heading in markdown.
/// Supports hierarchical structure with nested children.
/// </summary>
public class TocItem : ModelBase
{
    #region Model Base

    public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
    {
        if (modelBase is not TocItem other)
            return false;

        return Level.Is(other.Level)
            && Id.Is(other.Id)
            && Text.Is(other.Text)
            && Children.Is(other.Children);
    }

    public override TocItem Clone()
    {
        return new TocItem
        {
            Level = Level,
            Id = Id,
            Text = Text,
            Children = Children.Select(child => child.Clone()).ToList()
        };
    }

    #endregion

    #region Properties

    /// <summary>Heading level: 1=H1, 2=H2, 3=H3, etc.</summary>
    public int Level { get; set; }

    /// <summary>Anchor ID (slugified from heading text)</summary>
    public string Id { get; set; } = "";

    /// <summary>Display text of the heading</summary>
    [ToString]
    public string Text { get; set; } = "";

    /// <summary>Child headings (nested under this heading)</summary>
    public List<TocItem> Children { get; set; } = [];

    #endregion
}