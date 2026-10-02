using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain.Problems;
using JoinRpg.Domain.Problems.ClaimProblemFilters;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Domain.Test.Problems;

/// <summary>
/// Фильтр «в профиле не хватает контактов» поверх доменных сущностей (ADR013).
/// </summary>
/// <remarks>
/// Раньше фильтр читал контакты прямо из EF-полей <c>claim.Player.Extra</c>, и тест подменял их
/// там же. Теперь профиль приходит готовым <see cref="UserInfo"/>, поэтому случаи задаются
/// значениями этой записи — набор проверяемых случаев тот же.
/// </remarks>
public class ClaimContactsMissingFilterTest
{
    private MockedProject Mock { get; } = new MockedProject();
    private ClaimContactsMissingFilter Filter { get; } = new ClaimContactsMissingFilter();

    private ProjectInfo WithRequirement(Func<ProjectProfileRequirementSettings, ProjectProfileRequirementSettings> setup)
        => Mock.ProjectInfo.WithProfileRequirementSettings(setup(ProjectProfileRequirementSettings.AllNotRequired));

    /// <param name="player">
    /// Профиль игрока; по умолчанию — <c>MockedProject.PlayerInfo</c>, в котором из контактов не
    /// заполнено ничего.
    /// </param>
    private ClaimProblemContext MakeContext(
        ProjectInfo projectInfo,
        bool sensitiveDataAllowed = false,
        UserInfo? player = null)
    {
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);
        claim.PlayerAllowedSenstiveData = sensitiveDataAllowed;

        // Агрегат собирается на тех же метаданных, что проверяет тест: конструктор CharacterInfo
        // требует ровно тот экземпляр ProjectInfo, к которому привязаны слои полей, а
        // WithProfileRequirementSettings возвращает новый.
        var character = Mock.GetCharacterInfo(Mock.Character, projectInfo);

        return new ClaimProblemContext(character, character.GetClaimById(claim.GetId()), player ?? Mock.PlayerInfo);
    }

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
        var projectInfo = WithRequirement(s => s with { RequirePhone = MandatoryStatus.Required });

        Filter.GetProblems(MakeContext(projectInfo))
            .ShouldContain(p => p.ProblemType == ClaimProblemType.MissingPhone);
    }

    [Fact]
    public void PhoneMissingWhenTooShort()
    {
        var projectInfo = WithRequirement(s => s with { RequirePhone = MandatoryStatus.Required });
        var player = Mock.PlayerInfo with { PhoneNumber = "123" };

        Filter.GetProblems(MakeContext(projectInfo, player: player))
            .ShouldContain(p => p.ProblemType == ClaimProblemType.MissingPhone);
    }

    [Fact]
    public void PhoneNotMissingWhenFilledCorrectly()
    {
        var projectInfo = WithRequirement(s => s with { RequirePhone = MandatoryStatus.Required });
        var player = Mock.PlayerInfo with { PhoneNumber = "+79991234567" };

        Filter.GetProblems(MakeContext(projectInfo, player: player))
            .ShouldNotContain(p => p.ProblemType == ClaimProblemType.MissingPhone);
    }

    [Fact]
    public void RealNameMissingWhenNotFilledAtAll()
    {
        var projectInfo = WithRequirement(s => s with { RequireRealName = MandatoryStatus.Required });

        Filter.GetProblems(MakeContext(projectInfo))
            .ShouldContain(p => p.ProblemType == ClaimProblemType.MissingRealname);
    }

    [Fact]
    public void RealNameNotMissingWhenFilledCorrectly()
    {
        var projectInfo = WithRequirement(s => s with { RequireRealName = MandatoryStatus.Required });
        var player = Mock.PlayerInfo with
        {
            UserFullName = new UserFullName(
                PrefferedName.FromOptional("Player"),
                BornName.FromOptional("Иван"),
                SurName.FromOptional("Иванов"),
                FatherName: null),
        };

        Filter.GetProblems(MakeContext(projectInfo, player: player))
            .ShouldNotContain(p => p.ProblemType == ClaimProblemType.MissingRealname);
    }

    [Fact]
    public void PassportMissingWhenAllowedButNotFilled()
    {
        var projectInfo = WithRequirement(s => s with { RequirePassport = MandatoryStatus.Required });

        Filter.GetProblems(MakeContext(projectInfo, sensitiveDataAllowed: true))
            .ShouldContain(p => p.ProblemType == ClaimProblemType.MissingPassport);
    }

    [Fact]
    public void PassportNotMissingWhenAllowedAndFilled()
    {
        var projectInfo = WithRequirement(s => s with { RequirePassport = MandatoryStatus.Required });
        var player = Mock.PlayerInfo with { PassportData = "1234 567890" };

        Filter.GetProblems(MakeContext(projectInfo, sensitiveDataAllowed: true, player: player))
            .ShouldNotContain(p => p.ProblemType == ClaimProblemType.MissingPassport);
    }

    [Fact]
    public void RegistrationAddressMissingWhenAllowedButNotFilled()
    {
        var projectInfo = WithRequirement(s => s with { RequireRegistrationAddress = MandatoryStatus.Required });

        Filter.GetProblems(MakeContext(projectInfo, sensitiveDataAllowed: true))
            .ShouldContain(p => p.ProblemType == ClaimProblemType.MissingRegistrationAddress);
    }

    [Fact]
    public void SensitiveDataNotAllowedWhenConsentNotGiven()
    {
        var projectInfo = WithRequirement(s => s with { RequirePassport = MandatoryStatus.Required });

        var problems = Filter.GetProblems(MakeContext(projectInfo, sensitiveDataAllowed: false));

        problems.ShouldContain(p => p.ProblemType == ClaimProblemType.SensitiveDataNotAllowed);
        problems.ShouldNotContain(p => p.ProblemType == ClaimProblemType.MissingPassport);
        problems.ShouldNotContain(p => p.ProblemType == ClaimProblemType.MissingRegistrationAddress);
    }

    [Fact]
    public void NoSensitiveDataProblemsWhenNotRequiredAtAllRegardlessOfConsent()
    {
        var projectInfo = WithRequirement(s => s);

        var problems = Filter.GetProblems(MakeContext(projectInfo, sensitiveDataAllowed: false));

        problems.ShouldNotContain(p => p.ProblemType == ClaimProblemType.SensitiveDataNotAllowed);
        problems.ShouldNotContain(p => p.ProblemType == ClaimProblemType.MissingPassport);
        problems.ShouldNotContain(p => p.ProblemType == ClaimProblemType.MissingRegistrationAddress);
    }
}
