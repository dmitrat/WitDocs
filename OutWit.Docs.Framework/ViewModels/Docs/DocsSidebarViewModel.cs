using Microsoft.AspNetCore.Components;
using OutWit.Common.MVVM.Blazor.ViewModels;
using OutWit.Docs.Framework.Models;

namespace OutWit.Docs.Framework.ViewModels.Docs;

public class DocsSidebarViewModel : ViewModelBase
{
    #region Parameters

    [Parameter]
    public List<DocsSidebarSection> Sections { get; set; } = [];

    [Parameter]
    public string CurrentPath { get; set; } = string.Empty;

    #endregion
}
