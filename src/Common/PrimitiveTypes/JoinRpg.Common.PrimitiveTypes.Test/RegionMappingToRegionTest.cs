namespace JoinRpg.Common.PrimitiveTypes.Test;

public class RegionMappingToRegionTest
{
    [Fact]
    public void ToRegion_WhenNull_ReturnsNull()
    {
        RegionMapping.ToRegion(null).ShouldBeNull();
    }

    [Fact]
    public void ToRegion_WhenKnownId_ReturnsRegion()
    {
        RegionMapping.ToRegion(1).ShouldBe(Region.SanktPeterburg);
        RegionMapping.ToRegion(3).ShouldBe(Region.Moskva);
    }

    [Fact]
    public void ToRegion_WhenIdOutsideEnum_ReturnsNull()
    {
        RegionMapping.ToRegion(999).ShouldBeNull();
        RegionMapping.ToRegion(0).ShouldBeNull();
        RegionMapping.ToRegion(-1).ShouldBeNull();
    }

    [Fact]
    public void ToRegion_WhenObsoleteButDefinedId_ReturnsRegion()
    {
        // 172 — дубль «Франция», помечен [Obsolete], но входит в справочник КогдаИгры
        RegionMapping.ToRegion(172).ShouldBe((Region)172);
    }
}
