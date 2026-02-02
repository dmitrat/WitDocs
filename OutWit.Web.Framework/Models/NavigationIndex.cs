using OutWit.Common.Abstract;
using OutWit.Common.Collections;

namespace OutWit.Web.Framework.Models;

/// <summary>
/// Pre-built navigation index loaded from navigation-index.json.
/// Contains only the data needed for menu rendering, avoiding full content parsing.
/// </summary>
public class NavigationIndex : ModelBase
{
    #region Model Base

    public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
    {
        if (modelBase is not NavigationIndex other)
            return false;

        return Projects.Is(other.Projects)
               && Articles.Is(other.Articles)
               && Docs.Is(other.Docs)
               && Sections.Keys.Is(other.Sections.Keys);
    }

    public override NavigationIndex Clone()
    {
        return new NavigationIndex
        {
            Projects = Projects.Select(p => p.Clone()).ToList(),
            Articles = Articles.Select(a => a.Clone()).ToList(),
            Docs = Docs.Select(d => d.Clone()).ToList(),
            Sections = Sections.ToDictionary(
                kv => kv.Key, 
                kv => kv.Value.Select(i => i.Clone()).ToList())
        };
    }

    #endregion

    #region Properties

    /// <summary>
    /// Project menu items.
    /// </summary>
    public List<NavigationMenuItem> Projects { get; set; } = [];

    /// <summary>
    /// Article menu items.
    /// </summary>
    public List<NavigationMenuItem> Articles { get; set; } = [];

    /// <summary>
    /// Documentation menu items.
    /// </summary>
    public List<NavigationMenuItem> Docs { get; set; } = [];

    /// <summary>
    /// Dynamic section menu items.
    /// Key = section folder name, Value = menu items.
    /// </summary>
    public Dictionary<string, List<NavigationMenuItem>> Sections { get; set; } = new();

    #endregion
}
