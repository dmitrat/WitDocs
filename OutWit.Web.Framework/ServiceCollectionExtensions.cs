using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Web.Framework.Services;

namespace OutWit.Web.Framework;

/// <summary>
/// Extension methods for registering OutWit.Web.Framework services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add OutWit.Web.Framework services to the DI container.
    /// </summary>
    public static IServiceCollection AddOutWitWebFramework(this IServiceCollection services)
    {
        services.AddScoped<MarkdownService>();
        services.AddScoped<ThemeService>();
        services.AddScoped<ConfigService>();
        services.AddScoped<ContentService>();
        services.AddScoped<SearchService>();
        services.AddScoped<NavigationService>();
        services.AddScoped<ContentMetadataService>();
        services.AddScoped<ContentPreloader>();
        services.AddSingleton<ContentParser>();
        services.AddSingleton<ComponentRegistry>();

        return services;
    }

    /// <summary>
    /// Register a custom component that can be embedded in markdown content via
    /// <c>[[Name ...]]</c>, without modifying the framework. Call after
    /// <see cref="AddOutWitWebFramework"/>.
    /// </summary>
    /// <typeparam name="TComponent">The Blazor component to render.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name used in markdown (e.g. "Pricing").</param>
    public static IServiceCollection AddContentComponent<TComponent>(this IServiceCollection services, string name)
        where TComponent : IComponent
    {
        services.AddSingleton(new ContentComponentRegistration(name, typeof(TComponent)));
        return services;
    }
}
