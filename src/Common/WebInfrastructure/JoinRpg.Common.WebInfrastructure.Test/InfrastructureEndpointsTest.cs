using Shouldly;
using Xunit;

namespace JoinRpg.Common.WebInfrastructure;

public class InfrastructureEndpointsTest
{
    [Theory]
    [InlineData("/health", true)]
    [InlineData("/health/live", true)]
    [InlineData("/Health/Ready", true)]
    [InlineData("/metrics", true)]
    [InlineData("/healthcare", false)]
    [InlineData("/", false)]
    [InlineData("/project/1", false)]
    public void RecognizesInfrastructurePaths(string path, bool expected)
        => InfrastructureEndpoints.IsInfrastructurePath(path).ShouldBe(expected);
}
