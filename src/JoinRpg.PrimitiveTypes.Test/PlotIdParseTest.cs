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
    public void PlotVersionShouldRoundTrip()
    {
        var version = new PlotVersionIdentification(1, 2, 3, 4);
        var val = version.ToString();
        PlotVersionIdentification.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.ShouldBe(version);
    }

    [Theory]
    [InlineData("1-2-3")]
    [InlineData("PlotElementIdentification(1-2-3)")]
    [InlineData("PlotElement(1-2-3)")]
    public void PlotElementShouldParseTo123(string val)
    {
        PlotElementIdentification.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.ShouldBe(new PlotElementIdentification(1, 2, 3));
    }

    [Fact]
    public void PlotElementShouldRoundTrip()
    {
        var version = new PlotElementIdentification(1, 2, 3);
        var val = version.ToString();
        PlotElementIdentification.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.ShouldBe(version);
    }

    [Theory]
    [InlineData("1-2")]
    [InlineData("PlotFolderIdentification(1-2)")]
    [InlineData("PlotFolder(1-2)")]
    public void PlotFolderShouldParseTo123(string val)
    {
        PlotFolderIdentification.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.ShouldBe(new PlotFolderIdentification(1, 2));
    }

    [Fact]
    public void PlotFolderShouldRoundTrip()
    {
        var version = new PlotFolderIdentification(1, 2);
        var val = version.ToString();
        PlotFolderIdentification.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.ShouldBe(version);
    }
}
