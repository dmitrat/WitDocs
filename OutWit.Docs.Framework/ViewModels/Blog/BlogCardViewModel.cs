using Microsoft.AspNetCore.Components;
using OutWit.Common.MVVM.Blazor.ViewModels;
using OutWit.Docs.Framework.Models;

namespace OutWit.Docs.Framework.ViewModels.Blog;

public class BlogCardViewModel : ViewModelBase
{
    #region Parameters

    [Parameter]
    [EditorRequired]
    public BlogPost Post { get; set; } = null!;

    #endregion
}