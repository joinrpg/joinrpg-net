using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain.Problems.ClaimProblemFilters;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Domain.Test.Problems;

public class ClaimContactsMissingFilterTest
{
    private MockedProject Mock { get; } = new MockedProject();
    private ClaimContactsMissingFilter Filter { get; } = new ClaimContactsMissingFilter();

    [Fact]
    public void ToClaimProblemTypeIsDefinedForEveryUserProfileItemType()
    {
        foreach (var itemType in Enum.GetValues<UserProfileItemType>())
        {
            Should.NotThrow(() => ClaimContactsMissingFilter.ToClaimProblemType(itemType));
        }
    }

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

    [Fact]
    public void PassportMissingWhenAllowedButNotFilled()
    {
        var projectInfo = Mock.ProjectInfo.WithProfileRequirementSettings(
            ProjectProfileRequirementSettings.AllNotRequired with { RequirePassport = MandatoryStatus.Required });
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);
        claim.PlayerAllowedSenstiveData = true;

        Filter.GetProblems(claim, projectInfo).ShouldContain(p => p.ProblemType == ClaimProblemType.MissingPassport);
    }

    [Fact]
    public void PassportNotMissingWhenAllowedAndFilled()
    {
        var projectInfo = Mock.ProjectInfo.WithProfileRequirementSettings(
            ProjectProfileRequirementSettings.AllNotRequired with { RequirePassport = MandatoryStatus.Required });
        Mock.Player.Extra = new UserExtra { PassportData = "1234 567890" };
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);
        claim.PlayerAllowedSenstiveData = true;

        Filter.GetProblems(claim, projectInfo).ShouldNotContain(p => p.ProblemType == ClaimProblemType.MissingPassport);
    }

    [Fact]
    public void RegistrationAddressMissingWhenAllowedButNotFilled()
    {
        var projectInfo = Mock.ProjectInfo.WithProfileRequirementSettings(
            ProjectProfileRequirementSettings.AllNotRequired with { RequireRegistrationAddress = MandatoryStatus.Required });
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);
        claim.PlayerAllowedSenstiveData = true;

        Filter.GetProblems(claim, projectInfo).ShouldContain(p => p.ProblemType == ClaimProblemType.MissingRegistrationAddress);
    }

    [Fact]
    public void SensitiveDataNotAllowedWhenConsentNotGiven()
    {
        var projectInfo = Mock.ProjectInfo.WithProfileRequirementSettings(
            ProjectProfileRequirementSettings.AllNotRequired with { RequirePassport = MandatoryStatus.Required });
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);
        claim.PlayerAllowedSenstiveData = false;

        var problems = Filter.GetProblems(claim, projectInfo);

        problems.ShouldContain(p => p.ProblemType == ClaimProblemType.SensitiveDataNotAllowed);
        problems.ShouldNotContain(p => p.ProblemType == ClaimProblemType.MissingPassport);
        problems.ShouldNotContain(p => p.ProblemType == ClaimProblemType.MissingRegistrationAddress);
    }

    [Fact]
    public void NoSensitiveDataProblemsWhenNotRequiredAtAllRegardlessOfConsent()
    {
        var projectInfo = Mock.ProjectInfo.WithProfileRequirementSettings(ProjectProfileRequirementSettings.AllNotRequired);
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);
        claim.PlayerAllowedSenstiveData = false;

        var problems = Filter.GetProblems(claim, projectInfo);

        problems.ShouldNotContain(p => p.ProblemType == ClaimProblemType.SensitiveDataNotAllowed);
        problems.ShouldNotContain(p => p.ProblemType == ClaimProblemType.MissingPassport);
        problems.ShouldNotContain(p => p.ProblemType == ClaimProblemType.MissingRegistrationAddress);
    }
}
