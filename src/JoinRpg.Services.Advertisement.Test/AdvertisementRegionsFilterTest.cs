namespace JoinRpg.Services.Advertisement.Test;

public class AdvertisementRegionsFilterTest
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_WhenEmpty_ReturnsNull(string? regionsFilter) =>
        AdvertisementRegionsFilter.Parse(regionsFilter).ShouldBeNull();

    [Fact]
    public void Parse_WhenSingleRegion_ReturnsRegion() =>
        AdvertisementRegionsFilter.Parse("3").ShouldBe([Region.Moskva]);

    [Fact]
    public void Parse_WhenMultipleRegions_ReturnsAll() =>
        AdvertisementRegionsFilter.Parse("1,2,33").ShouldBe([Region.SanktPeterburg, Region.LeningradskayaOblast, Region.BelgorodskayaOblast]);

    [Fact]
    public void Parse_WhenGarbageTokens_SkipsThem() =>
        AdvertisementRegionsFilter.Parse("1, abc, 3").ShouldBe([Region.SanktPeterburg, Region.Moskva]);

    [Fact]
    public void Parse_WhenOnlyGarbage_ReturnsEmptySet() =>
        AdvertisementRegionsFilter.Parse("abc").ShouldBeEmpty();

    [Fact]
    public void Parse_WhenIdOutsideEnum_ReturnsEmptySet() =>
        AdvertisementRegionsFilter.Parse("999").ShouldBeEmpty();

    [Fact]
    public void Parse_WhenDuplicatesAndSpaces_Deduplicates() =>
        AdvertisementRegionsFilter.Parse(" 3 , 3 ").ShouldBe([Region.Moskva]);

    [Fact]
    public void Parse_WhenObsoleteButDefinedId_ReturnsRegion() =>
        AdvertisementRegionsFilter.Parse("172").ShouldBe([(Region)172]);
}
