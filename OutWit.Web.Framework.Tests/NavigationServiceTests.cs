using OutWit.Web.Framework.Models;
using OutWit.Web.Framework.Services;

namespace OutWit.Web.Framework.Tests;

/// <summary>
/// Tests for NavigationService.
/// </summary>
[TestFixture]
public class NavigationServiceTests
{
    #region Tests

    [Test]
    public void NavigationIndexCloneCreatesDeepCopyTest()
    {
        // Arrange
        var original = new NavigationIndex
        {
            Projects = [new NavigationMenuItem { Slug = "p1", Title = "Project 1" }],
            Articles = [new NavigationMenuItem { Slug = "a1", Title = "Article 1" }],
            Docs = [new NavigationMenuItem { Slug = "d1", Title = "Doc 1" }],
            Sections = new Dictionary<string, List<NavigationMenuItem>>
            {
                ["solutions"] = [new NavigationMenuItem { Slug = "s1", Title = "Solution 1" }]
            }
        };

        // Act
        var clone = original.Clone();

        // Assert - Clone is equal
        Assert.That(clone.Is(original), Is.True);
        
        // Assert - Modifying clone doesn't affect original
        clone.Projects[0].Title = "Modified";
        Assert.That(original.Projects[0].Title, Is.EqualTo("Project 1"));
    }

    [Test]
    public void NavigationMenuItemDisplayTitleReturnsMenuTitleWhenSetTest()
    {
        // Arrange
        var item = new NavigationMenuItem
        {
            Title = "Full Title",
            MenuTitle = "Short"
        };

        // Act & Assert
        Assert.That(item.DisplayTitle, Is.EqualTo("Short"));
    }

    [Test]
    public void NavigationMenuItemDisplayTitleReturnsTitleWhenMenuTitleNullTest()
    {
        // Arrange
        var item = new NavigationMenuItem
        {
            Title = "Full Title",
            MenuTitle = null
        };

        // Act & Assert
        Assert.That(item.DisplayTitle, Is.EqualTo("Full Title"));
    }

    [Test]
    public void NavigationMenuItemDisplayTitleReturnsTitleWhenMenuTitleEmptyTest()
    {
        // Arrange
        var item = new NavigationMenuItem
        {
            Title = "Full Title",
            MenuTitle = ""
        };

        // Act & Assert
        Assert.That(item.DisplayTitle, Is.EqualTo("Full Title"));
    }

    [Test]
    public void NavigationMenuItemCloneCreatesDeepCopyTest()
    {
        // Arrange
        var original = new NavigationMenuItem
        {
            Slug = "test",
            Title = "Test Title",
            MenuTitle = "Short",
            Order = 5,
            ShowInMenu = true,
            ShowInHeader = true
        };

        // Act
        var clone = original.Clone();

        // Assert
        Assert.That(clone.Is(original), Is.True);
        Assert.That(ReferenceEquals(clone, original), Is.False);
    }

    [Test]
    public void NavigationMenuItemIsReturnsTrueForEqualItemsTest()
    {
        // Arrange
        var item1 = new NavigationMenuItem
        {
            Slug = "test",
            Title = "Test",
            Order = 1,
            ShowInMenu = true
        };
        
        var item2 = new NavigationMenuItem
        {
            Slug = "test",
            Title = "Test",
            Order = 1,
            ShowInMenu = true
        };

        // Act & Assert
        Assert.That(item1.Is(item2), Is.True);
    }

    [Test]
    public void NavigationMenuItemIsReturnsFalseForDifferentItemsTest()
    {
        // Arrange
        var item1 = new NavigationMenuItem { Slug = "test1" };
        var item2 = new NavigationMenuItem { Slug = "test2" };

        // Act & Assert
        Assert.That(item1.Is(item2), Is.False);
    }

    [Test]
    public void NavigationIndexIsReturnsTrueForEqualIndicesTest()
    {
        // Arrange
        var index1 = new NavigationIndex
        {
            Projects = [new NavigationMenuItem { Slug = "p1" }],
            Articles = [],
            Docs = []
        };
        
        var index2 = new NavigationIndex
        {
            Projects = [new NavigationMenuItem { Slug = "p1" }],
            Articles = [],
            Docs = []
        };

        // Act & Assert
        Assert.That(index1.Is(index2), Is.True);
    }

    [Test]
    public void NavigationIndexIsReturnsFalseForDifferentIndicesTest()
    {
        // Arrange
        var index1 = new NavigationIndex
        {
            Projects = [new NavigationMenuItem { Slug = "p1" }]
        };
        
        var index2 = new NavigationIndex
        {
            Projects = [new NavigationMenuItem { Slug = "p2" }]
        };

        // Act & Assert
        Assert.That(index1.Is(index2), Is.False);
    }

    #endregion
}
