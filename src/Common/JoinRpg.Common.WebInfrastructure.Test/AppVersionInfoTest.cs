using Shouldly;
using Xunit;

namespace JoinRpg.Common.WebInfrastructure;

public class AppVersionInfoTest
{
    [Theory]
    [InlineData("https://github.com/joinrpg/joinrpg-net")]
    [InlineData("https://github.com/joinrpg/joinrpg-net.git")]
    [InlineData("https://github.com/joinrpg/joinrpg-net/")]
    public void VersionWithCommitIsSplit_LinkGoesToOwnRepository(string repositoryUrl)
    {
        var info = AppVersionInfo.Parse("1.2.3+abc123", repositoryUrl);

        info.Version.ShouldBe("1.2.3");
        info.CommitSha.ShouldBe("abc123");
        info.CommitUrl.ShouldBe(new Uri("https://github.com/joinrpg/joinrpg-net/commit/abc123"));
    }

    [Fact]
    public void OtherApplicationRepository_LinksToItsCommit()
    {
        var info = AppVersionInfo.Parse("2.0.0+def456", "https://github.com/leotsarev/bastilia-rating");

        info.CommitUrl.ShouldBe(new Uri("https://github.com/leotsarev/bastilia-rating/commit/def456"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://gitlab.com/joinrpg/joinrpg-net")]
    public void UnknownOrNotGitHubRepository_ShowsCommitWithoutLink(string? repositoryUrl)
    {
        var info = AppVersionInfo.Parse("1.2.3+abc123", repositoryUrl);

        info.CommitSha.ShouldBe("abc123");
        info.CommitUrl.ShouldBeNull();
    }

    [Fact]
    public void VersionWithoutCommitHasNoCommit()
    {
        var info = AppVersionInfo.Parse("1.2.3", "https://github.com/joinrpg/joinrpg-net");

        info.Version.ShouldBe("1.2.3");
        info.CommitSha.ShouldBeNull();
        info.CommitUrl.ShouldBeNull();
    }

    [Fact]
    public void MissingVersionIsUnknown()
    {
        var info = AppVersionInfo.Parse(null, null);

        info.Version.ShouldBe("неизвестна");
        info.CommitSha.ShouldBeNull();
    }
}
