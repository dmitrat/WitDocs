namespace OutWit.Web.Framework.Services;

/// <summary>
/// Service for preloading content indices in parallel at application startup.
/// This significantly speeds up initial page load in both Debug and Release modes
/// by avoiding sequential HTTP requests for index files.
/// </summary>
public class ContentPreloader
{
    #region Fields

    private readonly NavigationService m_navigationService;
    private readonly ContentMetadataService m_contentMetadataService;
    private readonly ContentService m_contentService;
    private bool m_preloaded;

    #endregion

    #region Constructors

    public ContentPreloader(
        NavigationService navigationService,
        ContentMetadataService contentMetadataService,
        ContentService contentService)
    {
        m_navigationService = navigationService;
        m_contentMetadataService = contentMetadataService;
        m_contentService = contentService;
    }

    #endregion

    #region Functions

    /// <summary>
    /// Preload all content indices in parallel.
    /// Call this once at application startup (e.g., in App.razor or MainLayout).
    /// Safe to call - never throws, logs errors to console.
    /// </summary>
    public async Task PreloadAsync()
    {
        if (m_preloaded)
            return;

        m_preloaded = true;

        try
        {
            // Load all indices in parallel to avoid sequential HTTP delays
            // Each method handles its own errors internally
            await Task.WhenAll(
                m_navigationService.GetNavigationIndexAsync(),
                m_contentMetadataService.GetMetadataIndexAsync(),
                m_contentService.GetContentIndexAsync()
            );
        }
        catch (Exception ex)
        {
            // Should not happen as each service handles errors, but just in case
            Console.WriteLine($"ContentPreloader error: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if preloading has been completed.
    /// </summary>
    public bool IsPreloaded => m_preloaded;

    #endregion
}
