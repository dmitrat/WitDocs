using Microsoft.AspNetCore.Components;
using OutWit.Common.MVVM.Blazor.ViewModels;

namespace OutWit.Web.Framework.ViewModels.Common;

public class LoadingStateViewModel : ViewModelBase
{
    #region Parameters

    [Parameter]
    public bool IsLoading { get; set; }
    
    [Parameter]
    public bool HasContent { get; set; }
    
    [Parameter]
    public string LoadingText { get; set; } = "Loading...";
    
    [Parameter]
    public string NotFoundText { get; set; } = "Content not found.";
    
    [Parameter]
    public string? BackLinkUrl { get; set; }
    
    [Parameter]
    public string BackLinkText { get; set; } = "← Back";
    
    /// <summary>
    /// Content to display when loaded.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Skeleton content to display while loading.
    /// If provided, this is shown instead of the loading text.
    /// </summary>
    [Parameter]
    public RenderFragment? SkeletonContent { get; set; }

    /// <summary>
    /// Whether to use skeleton loading.
    /// </summary>
    protected bool UseSkeleton => SkeletonContent != null;

    #endregion
}