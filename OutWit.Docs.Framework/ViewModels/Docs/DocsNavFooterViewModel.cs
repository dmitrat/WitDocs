using Microsoft.AspNetCore.Components;
using OutWit.Common.MVVM.Blazor.ViewModels;
using OutWit.Docs.Framework.Models;

namespace OutWit.Docs.Framework.ViewModels.Docs;

public class DocsNavFooterViewModel : ViewModelBase
{
    #region Parameters

    [Parameter]
    public DocNavLink? Previous { get; set; }

    [Parameter]
    public DocNavLink? Next { get; set; }

    #endregion
}
