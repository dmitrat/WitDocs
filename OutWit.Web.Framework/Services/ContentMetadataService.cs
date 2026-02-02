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
    /// </summary>
    public async Task<bool> IsMetadataIndexAvailableAsync()
    {
        var index = await GetMetadataIndexAsync();
        return index.Blog.Count > 0
               || index.Projects.Count > 0
               || index.Articles.Count > 0
               || index.Docs.Count > 0
               || index.Features.Count > 0
               || index.Sections.Count > 0;
    }

    /// <summary>
    /// Invalidate cache (for development hot reload).
    /// </summary>
    public void InvalidateCache()
    {
        m_metadataIndex = null;
    }

    #endregion

    #region Tools

    private async Task<ContentMetadataIndex> LoadMetadataIndexAsync()
    {
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
                Console.WriteLine($"Loaded content metadata index: {totalItems} items");
                return index;
            }
        }
        catch (HttpRequestException)
        {
            // File not found - metadata index not available
            Console.WriteLine("Content metadata index not found, will fall back to content parsing");
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
