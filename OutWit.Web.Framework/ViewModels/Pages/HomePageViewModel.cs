using Microsoft.AspNetCore.Components;
using OutWit.Common.MVVM.Blazor.ViewModels;
using OutWit.Web.Framework.Content;
using OutWit.Web.Framework.Models;
using OutWit.Web.Framework.Services;

namespace OutWit.Web.Framework.ViewModels.Pages;

public class HomePageViewModel : ViewModelBase
{
    #region Fields

    private List<ProjectCard> m_projects = [];
    private List<ProjectMetadata>? m_projectsMetadata;
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
                m_projectsMetadata = await ContentMetadataService.GetProjectsMetadataAsync();
            }
            else
            {
                // Fallback: load full content (slow path for development)
                m_projects = await ContentService.GetProjectsAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading projects: {ex.Message}");
            m_projects = [];
            m_projectsMetadata = null;
        }
        m_loading = false;
    }

    #endregion

    #region Functions

    /// <summary>
    /// Convert summary string to MarkupString for rendering.
    /// Summary is already HTML (either from pre-built metadata or ContentService).
    /// </summary>
    protected MarkupString RenderSummary(string summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
            return new MarkupString(string.Empty);
        
        // Summary is already HTML - just wrap it
        return new MarkupString(summary);
    }
    
    protected string GetFirstProjectAnchor()
    {
        if (m_projectsMetadata != null && m_projectsMetadata.Count > 0)
        {
            var first = m_projectsMetadata.FirstOrDefault();
            return first != null ? $"#project-{first.Slug}" : "#projects";
        }
        
        var firstProject = m_projects.FirstOrDefault(p => p.IsFirstProject);
        if (firstProject != null)
            return $"#project-{firstProject.Slug}";
        
        firstProject = m_projects.FirstOrDefault(p => !p.ShowInHeader);
        if (firstProject != null)
            return $"#project-{firstProject.Slug}";
        
        return "#projects";
    }

    #endregion

    #region Properties

    /// <summary>
    /// Get projects for rendering. Uses metadata if available, falls back to full content.
    /// </summary>
    protected IEnumerable<(string Slug, string Title, string Summary, List<string> Tags)> ProjectItems
    {
        get
        {
            if (m_projectsMetadata != null)
            {
                return m_projectsMetadata.Select(p => (p.Slug, p.Title, p.Summary, p.Tags));
            }
            return m_projects.Select(p => (p.Slug, p.Title, p.Summary, p.Tags));
        }
    }

    protected List<ProjectCard> Projects => m_projects;
    
    protected List<ProjectMetadata>? ProjectsMetadata => m_projectsMetadata;
    
    protected bool HasMetadata => m_projectsMetadata != null;
    
    protected bool Loading => m_loading;

    #endregion

    #region Parameters

    [Parameter]
    public RenderFragment? HeroContent { get; set; }
    
    [Parameter]
    public string? ProjectsSectionTitle { get; set; }
    
    [Parameter]
    public string SeoTitle { get; set; } = "Home";
    
    [Parameter]
    public string SeoDescription { get; set; } = string.Empty;

    #endregion

    #region Injected Dependencies

    [Inject]
    public ContentService ContentService { get; set; } = null!;
    
    [Inject]
    public ContentMetadataService ContentMetadataService { get; set; } = null!;

    #endregion
}
