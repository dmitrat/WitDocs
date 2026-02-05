using System.Net.Http.Json;
using System.Text.Json;
using OutWit.Web.Framework.Models;

namespace OutWit.Web.Framework.Services;

/// <summary>
/// Service for loading pre-built content metadata index.
/// Provides fast access to content metadata for list pages without parsing markdown files.
/// </summary>
public class ContentMetadataService
{
    #region Fields

    private ContentMetadataIndex? m_metadataIndex;
    private bool m_indexChecked;
    private bool m_indexAvailable;
    private readonly SemaphoreSlim m_indexLock = new(1, 1);

    #endregion

    #region Constructors

    public ContentMetadataService(HttpClient httpClient)
    {
        HttpClient = httpClient;
    }

    #endregion

    #region Functions

    /// <summary>
    /// Get the content metadata index, loading it if necessary.
    /// </summary>
    public async Task<ContentMetadataIndex> GetMetadataIndexAsync()
    {
        if (m_metadataIndex != null)
            return m_metadataIndex;

        await m_indexLock.WaitAsync();
        try
        {
            if (m_metadataIndex != null)
                return m_metadataIndex;

            m_metadataIndex = await LoadMetadataIndexAsync();
            return m_metadataIndex;
        }
        finally
        {
            m_indexLock.Release();
        }
    }

    /// <summary>
    /// Get blog post metadata for list rendering.
    /// </summary>
    public async Task<List<BlogPostMetadata>> GetBlogPostsMetadataAsync()
    {
        var index = await GetMetadataIndexAsync();
        return index.Blog;
    }

    /// <summary>
    /// Get project metadata for list rendering.
    /// </summary>
    public async Task<List<ProjectMetadata>> GetProjectsMetadataAsync()
    {
        var index = await GetMetadataIndexAsync();
        return index.Projects;
    }

    /// <summary>
    /// Get article metadata for list rendering.
    /// </summary>
    public async Task<List<ArticleMetadata>> GetArticlesMetadataAsync()
    {
        var index = await GetMetadataIndexAsync();
        return index.Articles;
    }

    /// <summary>
    /// Get documentation metadata for list rendering.
    /// </summary>
    public async Task<List<DocMetadata>> GetDocsMetadataAsync()
    {
        var index = await GetMetadataIndexAsync();
        return index.Docs;
    }

    /// <summary>
    /// Get feature metadata for list rendering.
    /// </summary>
    public async Task<List<FeatureMetadata>> GetFeaturesMetadataAsync()
    {
        var index = await GetMetadataIndexAsync();
        return index.Features;
    }

    /// <summary>
    /// Get section article metadata for list rendering.
    /// </summary>
    public async Task<List<ArticleMetadata>> GetSectionMetadataAsync(string sectionName)
    {
        var index = await GetMetadataIndexAsync();
        return index.Sections.TryGetValue(sectionName, out var items)
            ? items
            : [];
    }

    /// <summary>
    /// Check if metadata index is available (pre-built).
    /// Returns cached result to avoid repeated HTTP requests.
    /// </summary>
    public async Task<bool> IsMetadataIndexAvailableAsync()
    {
        // Return cached result if already checked
        if (m_indexChecked)
            return m_indexAvailable;

        var index = await GetMetadataIndexAsync();
        return m_indexAvailable;
    }

    /// <summary>
    /// Invalidate cache (for development hot reload).
    /// </summary>
    public void InvalidateCache()
    {
        m_metadataIndex = null;
        m_indexChecked = false;
        m_indexAvailable = false;
    }

    #endregion

    #region Tools

    private async Task<ContentMetadataIndex> LoadMetadataIndexAsync()
    {
        m_indexChecked = true;
        m_indexAvailable = false;

        try
        {
            var index = await HttpClient.GetFromJsonAsync<ContentMetadataIndex>(
                "content-metadata.json",
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            );

            if (index != null)
            {
                var totalItems = index.Blog.Count + index.Projects.Count + 
                                 index.Articles.Count + index.Docs.Count + 
                                 index.Features.Count + 
                                 index.Sections.Values.Sum(s => s.Count);
                
                m_indexAvailable = totalItems > 0;
                
                if (m_indexAvailable)
                {
                    Console.WriteLine($"Loaded content metadata index: {totalItems} items");
                }
                
                return index;
            }
        }
        catch (HttpRequestException)
        {
            // File not found - metadata index not available (expected in Debug mode)
            Console.WriteLine("Content metadata index not found (Debug mode)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading content metadata index: {ex.Message}");
        }

        return new ContentMetadataIndex();
    }

    #endregion

    #region Properties

    private HttpClient HttpClient { get; }

    #endregion
}
