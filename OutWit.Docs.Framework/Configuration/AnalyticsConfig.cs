using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Docs.Framework.Configuration;

/// <summary>
/// Web analytics configuration (opt-in). When configured, the generator injects a
/// provider-agnostic tracker snippet into every prerendered page:
/// <c>&lt;script defer src="{scriptUrl}" data-website-id="{websiteId}" ... &gt;</c>.
/// The site only knows the script URL and the website id, so the analytics backend
/// can be swapped without touching site code or content.
/// </summary>
public class AnalyticsConfig : ModelBase
{
    #region Model Base

    public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
    {
        if (modelBase is not AnalyticsConfig other)
            return false;

        return ScriptUrl.Is(other.ScriptUrl)
            && WebsiteId.Is(other.WebsiteId)
            && Domains.Is(other.Domains)
            && ExcludeSearch.Is(other.ExcludeSearch);
    }

    public override AnalyticsConfig Clone()
    {
        return new AnalyticsConfig
        {
            ScriptUrl = ScriptUrl,
            WebsiteId = WebsiteId,
            Domains = Domains,
            ExcludeSearch = ExcludeSearch
        };
    }

    #endregion

    #region Properties

    /// <summary>
    /// Absolute URL of the tracker script (e.g. <c>https://stats.example.com/u.js</c>).
    /// Null/empty disables analytics — nothing is injected.
    /// </summary>
    public string? ScriptUrl { get; set; }

    /// <summary>
    /// Website identifier issued by the analytics backend (rendered as the
    /// <c>data-website-id</c> attribute). Required for injection when ScriptUrl is set.
    /// </summary>
    public string? WebsiteId { get; set; }

    /// <summary>
    /// Optional comma-separated list of hostnames the tracker should report from
    /// (rendered as <c>data-domains</c>). Events from other hosts — localhost,
    /// deploy previews — are dropped client-side. Null/empty omits the attribute.
    /// </summary>
    public string? Domains { get; set; }

    /// <summary>
    /// Strip query strings from tracked URLs (rendered as
    /// <c>data-exclude-search</c>). Default true — path-only tracking.
    /// </summary>
    public bool ExcludeSearch { get; set; } = true;

    #endregion

}
