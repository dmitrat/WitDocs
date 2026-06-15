namespace OutWit.Web.Framework.Services;

/// <summary>
/// A DI-collected registration mapping a markdown component name to its Blazor
/// component type. Sites add these via
/// <c>services.AddContentComponent&lt;TComponent&gt;("Name")</c> to embed custom
/// components in markdown content without modifying the framework.
/// </summary>
public sealed class ContentComponentRegistration
{
    #region Constructors

    public ContentComponentRegistration(string name, Type componentType)
    {
        Name = name;
        ComponentType = componentType;
    }

    #endregion

    #region Properties

    /// <summary>The name used in markdown, e.g. "Pricing" for <c>[[Pricing ...]]</c>.</summary>
    public string Name { get; }

    /// <summary>The Blazor component type to render for this name.</summary>
    public Type ComponentType { get; }

    #endregion
}
