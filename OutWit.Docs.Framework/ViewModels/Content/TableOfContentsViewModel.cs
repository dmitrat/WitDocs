using Microsoft.AspNetCore.Components;
using OutWit.Common.MVVM.Blazor.ViewModels;
using OutWit.Docs.Framework.Models;

namespace OutWit.Docs.Framework.ViewModels.Content;

public class TableOfContentsViewModel : ViewModelBase
{
    #region Parameters

    [Parameter]
    public List<TocEntry> Entries { get; set; } = [];

    #endregion
}
