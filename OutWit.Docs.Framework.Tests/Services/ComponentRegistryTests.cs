using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Docs.Framework;
using OutWit.Docs.Framework.Services;

namespace OutWit.Docs.Framework.Tests.Services;

/// <summary>
/// Tests for ComponentRegistry and the AddContentComponent registration API.
/// </summary>
[TestFixture]
public class ComponentRegistryTests
{
    private sealed class DummyComponent : ComponentBase { }

    [Test]
    public void RegistryIncludesBuiltInComponentsTest()
    {
        var registry = new ComponentRegistry();

        Assert.That(registry.IsRegistered("YouTube"), Is.True);
        Assert.That(registry.IsRegistered("Svg"), Is.True);
        Assert.That(registry.IsRegistered("FloatingImage"), Is.True);
    }

    [Test]
    public void RegistryIncludesDiSuppliedComponentsTest()
    {
        var registrations = new[] { new ContentComponentRegistration("Pricing", typeof(DummyComponent)) };

        var registry = new ComponentRegistry(registrations);

        Assert.That(registry.IsRegistered("Pricing"), Is.True);
        Assert.That(registry.GetComponentType("Pricing"), Is.EqualTo(typeof(DummyComponent)));
        Assert.That(registry.IsRegistered("YouTube"), Is.True); // built-ins still present
    }

    [Test]
    public void AddContentComponentRegistersResolvableViaDiTest()
    {
        var services = new ServiceCollection();
        services.AddOutWitWebFramework();
        services.AddContentComponent<DummyComponent>("Pricing");

        // Only ComponentRegistry is resolved; it depends solely on the registrations
        // (not on HttpClient), so this does not require a configured HttpClient.
        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<ComponentRegistry>();

        Assert.That(registry.IsRegistered("Pricing"), Is.True);
        Assert.That(registry.GetComponentType("Pricing"), Is.EqualTo(typeof(DummyComponent)));
    }
}
