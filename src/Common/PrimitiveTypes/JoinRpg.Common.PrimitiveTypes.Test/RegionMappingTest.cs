namespace JoinRpg.Common.PrimitiveTypes.Test;

public class RegionMappingTest
{
    [Fact]
    public void EveryRegion_MapsToDefinedMacroRegion()
    {
        foreach (var region in Enum.GetValues<Region>())
        {
            var macroRegion = region.ToMacroRegion();
            Enum.IsDefined(macroRegion).ShouldBeTrue($"у {region} макрорегион {macroRegion} не определён");
        }
    }

    [Fact]
    public void EveryMacroRegion_RoundTrips()
    {
        foreach (var macroRegion in Enum.GetValues<MacroRegion>())
        {
            var regions = macroRegion.GetRegions();
            regions.ShouldNotBeEmpty();
            foreach (var region in regions)
            {
                region.ToMacroRegion().ShouldBe(macroRegion, $"у {region} должен быть макрорегион {macroRegion}");
            }
        }
    }

    [Fact]
    public void GetRegions_CoversEveryRegion()
    {
        var covered = Enum.GetValues<MacroRegion>().SelectMany(m => m.GetRegions()).ToHashSet();
        foreach (var region in Enum.GetValues<Region>())
        {
            covered.ShouldContain(region);
        }
    }

    [Fact]
    public void UnknownRegion_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ((Region)999).ToMacroRegion());
        Should.Throw<ArgumentOutOfRangeException>(() => default(Region).ToMacroRegion());
    }

    [Fact]
    public void UnknownMacroRegion_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ((MacroRegion)999).GetRegions());
        Should.Throw<ArgumentOutOfRangeException>(() => default(MacroRegion).GetRegions());
    }

    /// <summary>
    /// Значения — внешний контракт с КогдаИгрой (sub_region_id), случайная перенумерация должна ломать тест.
    /// </summary>
    [Fact]
    public Task Region_Values_Snapshot()
    {
        var snapshot = Enum.GetValues<Region>()
            .ToDictionary(k => k.ToString(), v => (int)v);

        return Verify(snapshot);
    }

    [Fact]
    public Task MacroRegion_Values_Snapshot()
    {
        var snapshot = Enum.GetValues<MacroRegion>()
            .ToDictionary(k => k.ToString(), v => (int)v);

        return Verify(snapshot);
    }
}
