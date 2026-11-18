using Bunit;
using Ui.Components;

namespace Ui.Tests.Components;

public sealed class IconTests : BunitContext
{
    [Fact]
    public void Icon_RendersOneInlineSvgThatInheritsTheTextColourAndIsHiddenFromScreenReaders()
    {
        var cut = Render<Icon>(p => p.Add(i => i.Name, IconName.Plus));

        var svg = cut.Find("svg");
        Assert.Equal("true", svg.GetAttribute("aria-hidden"));
        Assert.Equal("currentColor", svg.GetAttribute("stroke"));
        Assert.DoesNotContain("<metadata", cut.Markup);
        Assert.DoesNotContain("c2pa", cut.Markup);
    }

    [Fact]
    public void EveryIconNameHasAShippedFile()
    {
        foreach (var name in Enum.GetValues<IconName>())
        {
            Assert.Contains(IconCatalogue.FileNameOf(name), IconCatalogue.ShippedFileNames);
        }
    }

    [Fact]
    public void EveryShippedIconFileHasAName()
    {
        var named = Enum.GetValues<IconName>().Select(IconCatalogue.FileNameOf).ToHashSet();

        Assert.NotEmpty(IconCatalogue.ShippedFileNames);
        foreach (var file in IconCatalogue.ShippedFileNames)
        {
            Assert.Contains(file, named);
        }
    }

    [Theory]
    [InlineData(IconName.AddExercise, "icon-add-exercise.svg")]
    [InlineData(IconName.Plus, "icon-plus.svg")]
    [InlineData(IconName.MenuMore, "icon-menu-more.svg")]
    public void FileNamesAreTheKebabCaseOfTheName(IconName name, string expected)
    {
        Assert.Equal(expected, IconCatalogue.FileNameOf(name));
    }
}
