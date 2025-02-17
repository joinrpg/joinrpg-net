using JoinRpg.PrimitiveTypes.Plots;

namespace JoinRpg.PrimitiveTypes.Test;

public class PlotIdParseTest
{
    [Theory]
    [InlineData("1-2-3-4")]
    [InlineData("PlotVersionIdentification(1-2-3-4)")]
    [InlineData("PlotVersion(1-2-3-4)")]
    public void PlotVersionShouldParseTo1234(string val)
    {
        PlotVersionIdentification.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.ShouldBe(new PlotVersionIdentification(1, 2, 3, 4));
    }

    [Fact]
    public void ShouldRoundTrip()
    {
        var version = new PlotVersionIdentification(1, 2, 3, 4);
        var val = version.ToString();
        PlotVersionIdentification.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.ShouldBe(version);
    }

    [Fact]
    public void ShouldRoundTripWithoutVersion()
    {
        var version = new PlotVersionIdentification(1, 2, 3, null);
        var val = version.ToString();
        PlotVersionIdentification.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.ShouldBe(version);
    }

    [Fact]
    public void ToStringWithoutVersion()
    {
        var version = new PlotVersionIdentification(1, 2, 3, null);
        version.ToString().ShouldBe("PlotVersion(1-2-3)");
    }
}
