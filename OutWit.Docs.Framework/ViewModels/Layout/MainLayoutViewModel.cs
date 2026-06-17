using Microsoft.AspNetCore.Components;
using OutWit.Docs.Framework.Services;

namespace OutWit.Docs.Framework.ViewModels.Layout;

public class MainLayoutViewModel : LayoutComponentBase
{
    #region Initialization

    protected override async Task OnInitializedAsync()
    {
        // Preload all content indices in parallel for faster page rendering
        await ContentPreloader.PreloadAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var config = await ConfigService.GetConfigAsync();
            
            await ThemeService.InitializeAsync(config.DefaultTheme);
            StateHasChanged();
        }
    }

    #endregion

    #region Injected Dependencies

    [Inject]
    public ConfigService ConfigService { get; set; } = null!;
    
    [Inject]
    public ThemeService ThemeService { get; set; } = null!;
    
    [Inject]
    public ContentPreloader ContentPreloader { get; set; } = null!;

    #endregion
}
