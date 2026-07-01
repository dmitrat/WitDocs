using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Docs.Framework.Configuration;

/// <summary>
/// SEO configuration.
/// </summary>
public class SeoConfig : ModelBase
{
    #region Model Base

    public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
    {
        if (modelBase is not SeoConfig other)
            return false;
        
        return DefaultImage.Is(other.DefaultImage)
            && Description.Is(other.Description)
            && TwitterHandle.Is(other.TwitterHandle)
            && FacebookAppId.Is(other.FacebookAppId)
            && IndexNowKey.Is(other.IndexNowKey);
    }

    public override SeoConfig Clone()
    {
        return new SeoConfig
        {
            DefaultImage = DefaultImage,
            Description = Description,
            TwitterHandle = TwitterHandle,
            FacebookAppId = FacebookAppId,
            IndexNowKey = IndexNowKey
        };
    }

    #endregion

    #region Properties

    public string DefaultImage { get; set; } = "/images/social-card.png";
    
    /// <summary>
    /// Default site description for SEO and OG images.
    /// </summary>
    public string? Description { get; set; }
    
    public string? TwitterHandle { get; set; }

    public string? FacebookAppId { get; set; }

    /// <summary>
    /// IndexNow key (opt-in). When set, the generator writes a
    /// <c>{key}.txt</c> verification file to the site root so search engines
    /// that support IndexNow (Bing, Yandex, Seznam, Naver) can verify
    /// ownership; the deploy pipeline submits changed URLs using this key.
    /// The key is public by design (allowed chars: a–z, A–Z, 0–9, dash; 8–128 long).
    /// Leave null/empty to disable IndexNow.
    /// </summary>
    public string? IndexNowKey { get; set; }

    #endregion

}