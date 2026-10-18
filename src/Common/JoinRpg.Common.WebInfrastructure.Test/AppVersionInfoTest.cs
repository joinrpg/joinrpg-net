using Shouldly;
using Xunit;

namespace JoinRpg.Common.WebInfrastructure;

public class AppVersionInfoTest
{
    [Fact]
    public void VersionWithCommitIsSplit()
    {
        var info = AppVersionInfo.Parse("1.2.3+abc123");

        info.Version.ShouldBe("1.2.3");
        info.CommitSha.ShouldBe("abc123");
        info.CommitUrl.ShouldBe(new Uri("https://github.com/joinrpg/joinrpg-net/commit/abc123"));
    }

    [Fact]
    public void VersionWithoutCommitHasNoCommit()
    {
        var info = AppVersionInfo.Parse("1.2.3");

        info.Version.ShouldBe("1.2.3");
        info.CommitSha.ShouldBeNull();
        info.CommitUrl.ShouldBeNull();
    }

    [Fact]
    public void MissingVersionIsUnknown()
    {
        var info = AppVersionInfo.Parse(null);

        info.Version.ShouldBe("неизвестна");
        info.CommitSha.ShouldBeNull();
    }
}
