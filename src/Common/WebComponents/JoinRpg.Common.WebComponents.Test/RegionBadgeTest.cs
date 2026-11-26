using JoinRpg.Common.PrimitiveTypes;

namespace JoinRpg.Common.WebComponents.Test;

public class RegionBadgeTest
{
    [Fact]
    public void RendersShortName_WithFullNameInTooltip()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<RegionBadge>(p => p.Add(x => x.Region, Region.SanktPeterburg));

        cut.Find(".label").TextContent.Trim().ShouldBe("СПб");
        cut.Find(".join-tooltip").TextContent.Trim().ShouldBe("Санкт-Петербург");
    }

    [Fact]
    public void DefaultVariation_IsInfo()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<RegionBadge>(p => p.Add(x => x.Region, Region.Moskva));

        cut.Find(".label").ClassList.ShouldContain("label-info");
    }
}
