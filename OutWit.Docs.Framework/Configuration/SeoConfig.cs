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
            && FacebookAppId.Is(other.FacebookAppId);
    }

    public override SeoConfig Clone()
    {
        return new SeoConfig
        {
            DefaultImage = DefaultImage,
            Description = Description,
            TwitterHandle = TwitterHandle,
            FacebookAppId = FacebookAppId
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

    #endregion

}