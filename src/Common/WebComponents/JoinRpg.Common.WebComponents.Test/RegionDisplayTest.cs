using JoinRpg.Common.PrimitiveTypes;

namespace JoinRpg.Common.WebComponents.Test;

public class RegionDisplayTest
{
    [Fact]
    public void AllRegions_HaveNames()
    {
        foreach (var region in Enum.GetValues<Region>())
        {
            region.GetName().ShouldNotBeNullOrWhiteSpace($"у {region} нет полного имени");
            region.GetShortName().ShouldNotBeNullOrWhiteSpace($"у {region} нет короткого имени");
        }
    }

    [Fact]
    public void AllMacroRegions_HaveNames()
    {
        foreach (var macroRegion in Enum.GetValues<MacroRegion>())
        {
            macroRegion.GetName().ShouldNotBeNullOrWhiteSpace($"у {macroRegion} нет имени");
        }
    }

    [Fact]
    public void SanktPeterburg_HasExpectedNames()
    {
        Region.SanktPeterburg.GetName().ShouldBe("Санкт-Петербург");
        Region.SanktPeterburg.GetShortName().ShouldBe("СПб");
        Region.SanktPeterburg.ToMacroRegion().GetName().ShouldBe("Северо-Запад");
    }

    [Fact]
    public void UnknownValues_Throw()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ((Region)999).GetName());
        Should.Throw<ArgumentOutOfRangeException>(() => ((Region)999).GetShortName());
        Should.Throw<ArgumentOutOfRangeException>(() => ((MacroRegion)999).GetName());
    }
}
