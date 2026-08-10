using JoinRpg.DataModel;
using JoinRpg.Domain.Problems.ClaimProblemFilters;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.Domain.Test.Problems;

public class ClaimContactsMissingFilterTests
{
    private readonly MockedProject mock = new();

    [Theory]
    [InlineData(MandatoryStatus.Optional, null)]
    [InlineData(MandatoryStatus.Recommended, ProblemSeverity.Warning)]
    [InlineData(MandatoryStatus.Required, ProblemSeverity.Warning)]
    public void TelegramRequirement_MissingTelegram_ProducesExpectedSeverity(MandatoryStatus status, ProblemSeverity? expectedSeverity)
    {
        mock.Player.Extra = new UserExtra();
        var projectInfo = mock.ProjectInfo.WithProfileRequirementSettings(
            ProjectProfileRequirementSettings.AllNotRequired with { RequireTelegram = status });
        var claim = mock.CreateClaim(mock.Character, mock.Player);

        var problems = new ClaimContactsMissingFilter().GetProblems(claim, projectInfo).ToList();

        if (expectedSeverity is null)
        {
            problems.ShouldBeEmpty();
        }
        else
        {
            problems.ShouldContain(p => p.ProblemType == ClaimProblemType.MissingTelegram && p.Severity == expectedSeverity);
        }
    }
}
