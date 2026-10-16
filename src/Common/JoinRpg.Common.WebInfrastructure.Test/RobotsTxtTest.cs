using Shouldly;
using Xunit;

namespace JoinRpg.Common.WebInfrastructure;

public class RobotsTxtTest
{
    private const string AllowEverything = "User-agent: *\nDisallow: /health\nDisallow: /metrics\n";
    private const string DisallowEverything = "User-agent: *\nDisallow: /\n";

    [Fact]
    public void WithoutSettingsEverythingIsAllowed()
        => RobotsTxtExtensions.BuildRobotsTxt("anything.ru", new RobotsTxtOptions()).ShouldBe(AllowEverything);

    [Fact]
    public void MainHostIsAllowed()
        => RobotsTxtExtensions.BuildRobotsTxt("JoinRpg.ru", new RobotsTxtOptions { MainHost = "joinrpg.ru" }).ShouldBe(AllowEverything);

    [Fact]
    public void MainHostWithPortIsAllowed()
        => RobotsTxtExtensions.BuildRobotsTxt("localhost:5001", new RobotsTxtOptions { MainHost = "localhost:5001" }).ShouldBe(AllowEverything);

    [Fact]
    public void PortIsIgnoredWhenMainHostHasNone()
        => RobotsTxtExtensions.BuildRobotsTxt("joinrpg.ru:443", new RobotsTxtOptions { MainHost = "joinrpg.ru" }).ShouldBe(AllowEverything);

    [Fact]
    public void OtherPortIsDisallowedWhenMainHostHasOne()
        => RobotsTxtExtensions.BuildRobotsTxt("localhost:5002", new RobotsTxtOptions { MainHost = "localhost:5001" }).ShouldBe(DisallowEverything);

    [Fact]
    public void EmptyMainHostMeansUnknown()
        => RobotsTxtExtensions.BuildRobotsTxt("anything.ru", new RobotsTxtOptions { MainHost = "" }).ShouldBe(AllowEverything);

    [Fact]
    public void OtherHostsAreDisallowed()
        => RobotsTxtExtensions.BuildRobotsTxt("joinrpg.tsarevs.ru", new RobotsTxtOptions { MainHost = "joinrpg.ru" }).ShouldBe(DisallowEverything);

    [Fact]
    public void DisallowAllAppliesToMainHost()
        => RobotsTxtExtensions.BuildRobotsTxt("dev.joinrpg.ru", new RobotsTxtOptions { MainHost = "dev.joinrpg.ru", DisallowAll = true }).ShouldBe(DisallowEverything);

    [Fact]
    public void DisallowAllWithoutMainHost()
        => RobotsTxtExtensions.BuildRobotsTxt("anything.ru", new RobotsTxtOptions { DisallowAll = true }).ShouldBe(DisallowEverything);

    [Fact]
    public void DisallowedPathsAreListed()
    {
        var options = new RobotsTxtOptions { MainHost = "joinrpg.ru", DisallowedPaths = ["/x-api"] };

        RobotsTxtExtensions.BuildRobotsTxt("joinrpg.ru", options)
            .ShouldBe("User-agent: *\nDisallow: /health\nDisallow: /metrics\nDisallow: /x-api\n");
    }
}
