using Microsoft.AspNetCore.Components;
using OutWit.Common.MVVM.Blazor.ViewModels;
using OutWit.Web.Framework.Models;
using OutWit.Web.Framework.Services;

namespace OutWit.Web.Framework.ViewModels.Pages;

public class BlogListPageViewModel : ViewModelBase
{
    #region Fields

    private List<BlogPost> m_posts = [];
    private List<BlogPostMetadata>? m_postsMetadata;
    private bool m_loading = true;

    #endregion

    #region Initialization

    protected override async Task OnInitializedAsync()
    {
        try
        {
            // Try to use pre-built metadata first (fast path)
            if (await ContentMetadataService.IsMetadataIndexAvailableAsync())
            {
                m_postsMetadata = await ContentMetadataService.GetBlogPostsMetadataAsync();
            }
            else
            {
                // Fallback: load full content (slow path for development)
                m_posts = await ContentService.GetBlogPostsAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading blog posts: {ex.Message}");
            m_posts = [];
            m_postsMetadata = null;
        }
        m_loading = false;
    }

    #endregion

    #region Functions

    protected string GetPostUrl(string slug) => $"{PostUrlPrefix}/{slug}";

    #endregion

    #region Properties

    /// <summary>
    /// Get blog posts for rendering. Uses metadata if available, falls back to full content.
    /// </summary>
    protected IEnumerable<BlogPostListItem> PostItems
    {
        get
        {
            if (m_postsMetadata != null)
            {
                return m_postsMetadata.Select(p => new BlogPostListItem
                {
                    Slug = p.Slug,
                    Title = p.Title,
                    Description = p.Description,
                    Summary = p.Summary,
                    PublishDate = p.PublishDate,
                    Tags = p.Tags,
                    ReadingTimeMinutes = p.ReadingTimeMinutes,
                    FeaturedImage = p.FeaturedImage
                });
            }
            return m_posts.Select(p => new BlogPostListItem
            {
                Slug = p.Slug,
                Title = p.Title,
                Description = p.Description,
                Summary = p.Summary,
                PublishDate = p.PublishDate,
                Tags = p.Tags,
                ReadingTimeMinutes = p.ReadingTimeMinutes,
                FeaturedImage = p.FeaturedImage ?? ""
            });
        }
    }

    protected List<BlogPost> Posts => m_posts;
    
    protected List<BlogPostMetadata>? PostsMetadata => m_postsMetadata;
    
    protected bool HasMetadata => m_postsMetadata != null;
    
    protected bool Loading => m_loading;
    
    protected bool HasContent => m_postsMetadata?.Count > 0 || m_posts.Count > 0;

    #endregion

    #region Parameters

    [Parameter]
    public string Title { get; set; } = string.Empty;
    
    [Parameter]
    public string? Description { get; set; }
    
    [Parameter]
    public string SeoTitle { get; set; } = "Blog";
    
    [Parameter]
    public string SeoDescription { get; set; } = string.Empty;
    
    [Parameter]
    public string SidebarTitle { get; set; } = "Recent posts";
    
    [Parameter]
    public int SidebarMaxPosts { get; set; } = 10;
    
    [Parameter]
    public string PostUrlPrefix { get; set; } = "/blog";

    #endregion

    #region Injected Dependencies

    [Inject]
    public ContentService ContentService { get; set; } = null!;
    
    [Inject]
    public ContentMetadataService ContentMetadataService { get; set; } = null!;

    #endregion
}
