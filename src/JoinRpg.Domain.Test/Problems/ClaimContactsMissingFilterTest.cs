using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain.Problems.ClaimProblemFilters;
using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.Test.Problems;

public class ClaimContactsMissingFilterTest
{
    private MockedProject Mock { get; } = new MockedProject();
    private ClaimContactsMissingFilter Filter { get; } = new ClaimContactsMissingFilter();

    [Fact]
    public void PhoneMissingWhenNotFilledAtAll()
    {
        var projectInfo = Mock.ProjectInfo.WithProfileRequirementSettings(
            ProjectProfileRequirementSettings.AllNotRequired with { RequirePhone = MandatoryStatus.Required });
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);

        Filter.GetProblems(claim, projectInfo).ShouldContain(p => p.ProblemType == ClaimProblemType.MissingPhone);
    }

    [Fact]
    public void PhoneMissingWhenTooShort()
    {
        var projectInfo = Mock.ProjectInfo.WithProfileRequirementSettings(
            ProjectProfileRequirementSettings.AllNotRequired with { RequirePhone = MandatoryStatus.Required });
        Mock.Player.Extra = new UserExtra { PhoneNumber = "123" };
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);

        Filter.GetProblems(claim, projectInfo).ShouldContain(p => p.ProblemType == ClaimProblemType.MissingPhone);
    }

    [Fact]
    public void PhoneNotMissingWhenFilledCorrectly()
    {
        var projectInfo = Mock.ProjectInfo.WithProfileRequirementSettings(
            ProjectProfileRequirementSettings.AllNotRequired with { RequirePhone = MandatoryStatus.Required });
        Mock.Player.Extra = new UserExtra { PhoneNumber = "+79991234567" };
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);

        Filter.GetProblems(claim, projectInfo).ShouldNotContain(p => p.ProblemType == ClaimProblemType.MissingPhone);
    }

    [Fact]
    public void RealNameMissingWhenNotFilledAtAll()
    {
        var projectInfo = Mock.ProjectInfo.WithProfileRequirementSettings(
            ProjectProfileRequirementSettings.AllNotRequired with { RequireRealName = MandatoryStatus.Required });
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);

        Filter.GetProblems(claim, projectInfo).ShouldContain(p => p.ProblemType == ClaimProblemType.MissingRealname);
    }

    [Fact]
    public void RealNameNotMissingWhenFilledCorrectly()
    {
        var projectInfo = Mock.ProjectInfo.WithProfileRequirementSettings(
            ProjectProfileRequirementSettings.AllNotRequired with { RequireRealName = MandatoryStatus.Required });
        Mock.Player.BornName = "Иван";
        Mock.Player.SurName = "Иванов";
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);

        Filter.GetProblems(claim, projectInfo).ShouldNotContain(p => p.ProblemType == ClaimProblemType.MissingRealname);
    }
}
