namespace JoinRpg.PrimitiveTypes.Test;

public class ProjectIdParseTest
{
    [Theory]
    [InlineData("1")]
    [InlineData("ProjectIdentification(1)")]
    public void ShouldParseTo1(string val)
    {
        ProjectIdentification.TryParse(val, provider: null, out var parseResult).ShouldBeTrue();
        parseResult.Value.ShouldBe(1);
    }

    [Theory]
    [InlineData("xxxx")]
    [InlineData("Pr(1)")]
    [InlineData("Pr1")]
    public void ShouldFailToParse(string val)
    {
        ProjectIdentification.TryParse(val, provider: null, out var _).ShouldBeFalse();
    }
}
