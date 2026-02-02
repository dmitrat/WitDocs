using OutWit.Common.Abstract;
using OutWit.Common.Collections;

namespace OutWit.Web.Framework.Models;

/// <summary>
/// Pre-built content metadata index loaded from content-metadata.json.
/// Contains lightweight metadata for all content items, enabling fast list rendering
/// without parsing individual markdown files.
/// </summary>
public class ContentMetadataIndex : ModelBase
{
    #region Model Base

    public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
    {
        if (modelBase is not ContentMetadataIndex other)
            return false;

        return Blog.Is(other.Blog)
               && Projects.Is(other.Projects)
               && Articles.Is(other.Articles)
               && Docs.Is(other.Docs)
               && Features.Is(other.Features)
               && Sections.Keys.Is(other.Sections.Keys);
    }

    public override ContentMetadataIndex Clone()
    {
        return new ContentMetadataIndex
        {
            Blog = Blog.Select(b => b.Clone()).ToList(),
            Projects = Projects.Select(p => p.Clone()).ToList(),
            Articles = Articles.Select(a => a.Clone()).ToList(),
            Docs = Docs.Select(d => d.Clone()).ToList(),
            Features = Features.Select(f => f.Clone()).ToList(),
            Sections = Sections.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.Select(i => i.Clone()).ToList())
        };
    }

    #endregion

    #region Properties

    /// <summary>
    /// Blog post metadata.
    /// </summary>
    public List<BlogPostMetadata> Blog { get; set; } = [];

    /// <summary>
    /// Project metadata.
    /// </summary>
    public List<ProjectMetadata> Projects { get; set; } = [];

    /// <summary>
    /// Article metadata.
    /// </summary>
    public List<ArticleMetadata> Articles { get; set; } = [];

    /// <summary>
    /// Documentation metadata.
    /// </summary>
    public List<DocMetadata> Docs { get; set; } = [];

    /// <summary>
    /// Feature metadata.
    /// </summary>
    public List<FeatureMetadata> Features { get; set; } = [];

    /// <summary>
    /// Dynamic section metadata.
    /// Key = section folder name, Value = article metadata list.
    /// </summary>
    public Dictionary<string, List<ArticleMetadata>> Sections { get; set; } = new();

    #endregion
}
