using System.Net.Http.Json;
using System.Text.Json;
using OutWit.Web.Framework.Models;

namespace OutWit.Web.Framework.Services;

/// <summary>
/// Service for loading pre-built navigation index.
/// Provides fast menu data without parsing markdown files.
/// </summary>
public class NavigationService
{
    #region Fields

    private NavigationIndex? m_navigationIndex;
    private readonly SemaphoreSlim m_indexLock = new(1, 1);

    #endregion

    #region Constructors

    public NavigationService(HttpClient httpClient)
    {
        HttpClient = httpClient;
    }

    #endregion

    #region Functions

    /// <summary>
    /// Get the navigation index, loading it if necessary.
    /// </summary>
    public async Task<NavigationIndex> GetNavigationIndexAsync()
    {
        if (m_navigationIndex != null)
            return m_navigationIndex;

        await m_indexLock.WaitAsync();
        try
        {
            if (m_navigationIndex != null)
                return m_navigationIndex;

            m_navigationIndex = await LoadNavigationIndexAsync();
            return m_navigationIndex;
        }
        finally
        {
            m_indexLock.Release();
        }
    }

    /// <summary>
    /// Get project menu items.
    /// </summary>
    public async Task<List<NavigationMenuItem>> GetProjectMenuItemsAsync()
    {
        var index = await GetNavigationIndexAsync();
        return index.Projects;
    }

    /// <summary>
    /// Get article menu items.
    /// </summary>
    public async Task<List<NavigationMenuItem>> GetArticleMenuItemsAsync()
    {
        var index = await GetNavigationIndexAsync();
        return index.Articles;
    }

    /// <summary>
    /// Get documentation menu items.
    /// </summary>
    public async Task<List<NavigationMenuItem>> GetDocsMenuItemsAsync()
    {
        var index = await GetNavigationIndexAsync();
        return index.Docs;
    }

    /// <summary>
    /// Get menu items for a dynamic section.
    /// </summary>
    public async Task<List<NavigationMenuItem>> GetSectionMenuItemsAsync(string sectionName)
    {
        var index = await GetNavigationIndexAsync();
        return index.Sections.TryGetValue(sectionName, out var items) 
            ? items 
            : [];
    }

    /// <summary>
    /// Check if navigation index is available (pre-built).
    /// </summary>
    public async Task<bool> IsNavigationIndexAvailableAsync()
    {
        var index = await GetNavigationIndexAsync();
        return index.Projects.Count > 0 
               || index.Articles.Count > 0 
               || index.Docs.Count > 0 
               || index.Sections.Count > 0;
    }

    /// <summary>
    /// Invalidate cache (for development hot reload).
    /// </summary>
    public void InvalidateCache()
    {
        m_navigationIndex = null;
    }

    #endregion

    #region Tools

    private async Task<NavigationIndex> LoadNavigationIndexAsync()
    {
        try
        {
            var index = await HttpClient.GetFromJsonAsync<NavigationIndex>(
                "navigation-index.json",
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            );

            if (index != null)
            {
                Console.WriteLine($"Loaded navigation index: {index.Projects.Count} projects, {index.Articles.Count} articles, {index.Docs.Count} docs, {index.Sections.Count} sections");
                return index;
            }
        }
        catch (HttpRequestException)
        {
            // File not found - navigation index not available
            Console.WriteLine("Navigation index not found, will fall back to content parsing");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading navigation index: {ex.Message}");
        }

        return new NavigationIndex();
    }

    #endregion

    #region Properties

    private HttpClient HttpClient { get; }

    #endregion
}
