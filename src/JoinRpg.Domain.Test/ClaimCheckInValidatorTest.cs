using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain.Problems;
using JoinRpg.Domain.Problems.ClaimProblemFilters;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Domain.Test;

/// <summary>
/// Светофор регистрации учитывает профиль игрока, а не только поля заявки (#5708).
/// </summary>
public class ClaimCheckInValidatorTest
{
    private MockedProject Mock { get; } = new MockedProject();

    private ProjectInfo WithRequirement(Func<ProjectProfileRequirementSettings, ProjectProfileRequirementSettings> setup)
        => Mock.ProjectInfo.WithProfileRequirementSettings(setup(ProjectProfileRequirementSettings.AllNotRequired));

    /// <summary>
    /// «Запросить» паспорт в настройках проекта сохраняется как Recommended.
    /// </summary>
    private ProjectInfo PassportRequested() => WithRequirement(s => s with { RequirePassport = MandatoryStatus.Recommended });

    private ClaimCheckInValidator MakeValidator(ProjectInfo projectInfo, bool sensitiveDataAllowed = false, UserInfo? player = null)
    {
        var claim = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);
        claim.PlayerAllowedSenstiveData = sensitiveDataAllowed;

        var character = Mock.GetCharacterInfo(Mock.Character, projectInfo);
        var context = new ClaimInfo(new ClaimInCharacter(character, claim.GetId()), player ?? Mock.PlayerInfo);

        return new ClaimCheckInValidator(context, new ContactsOnlyValidator());
    }

    [Fact]
    public void NothingRequiredCanCheckIn()
        => MakeValidator(WithRequirement(s => s)).CanCheckInInPrinciple.ShouldBeTrue();

    [Fact]
    public void PassportRequestedAllowedButEmptyBlocksCheckIn()
    {
        var validator = MakeValidator(PassportRequested(), sensitiveDataAllowed: true);

        validator.CanCheckInInPrinciple.ShouldBeFalse();
        validator.Problems.ShouldContain(p => p.ProblemType == ClaimProblemType.MissingPassport);
    }

    [Fact]
    public void PassportRequestedButNotAllowedBlocksCheckIn()
    {
        var validator = MakeValidator(PassportRequested(), sensitiveDataAllowed: false, player: Mock.PlayerInfo with { PassportData = "4505 123456, выдан 01.01.2010" });

        validator.CanCheckInInPrinciple.ShouldBeFalse();
        validator.Problems.ShouldContain(p => p.ProblemType == ClaimProblemType.SensitiveDataNotAllowed);
    }

    [Fact]
    public void PassportRequestedAllowedAndFilledCanCheckIn()
        => MakeValidator(PassportRequested(), sensitiveDataAllowed: true, player: Mock.PlayerInfo with { PassportData = "4505 123456, выдан 01.01.2010" })
            .CanCheckInInPrinciple.ShouldBeTrue();

    [Fact]
    public void RegistrationAddressRequestedAndEmptyBlocksCheckIn()
    {
        var projectInfo = WithRequirement(s => s with { RequireRegistrationAddress = MandatoryStatus.Recommended });

        MakeValidator(projectInfo, sensitiveDataAllowed: true).Problems
            .ShouldContain(p => p.ProblemType == ClaimProblemType.MissingRegistrationAddress);
    }

    [Fact]
    public void RequiredPhoneMissingBlocksCheckIn()
    {
        var validator = MakeValidator(WithRequirement(s => s with { RequirePhone = MandatoryStatus.Required }));

        validator.CanCheckInInPrinciple.ShouldBeFalse();
        validator.Problems.ShouldContain(p => p.ProblemType == ClaimProblemType.MissingPhone);
    }

    [Fact]
    public void RecommendedPhoneMissingDoesNotBlockCheckIn()
    {
        var validator = MakeValidator(WithRequirement(s => s with { RequirePhone = MandatoryStatus.Recommended }));

        validator.CanCheckInInPrinciple.ShouldBeTrue();
        validator.Problems.ShouldBeEmpty();
    }

    [Fact]
    public void MasterSideProblemsDoNotBlockCheckIn()
        => new ClaimProblem(ClaimProblemType.NoResponsibleMaster, ProblemSeverity.Error).BlocksCheckIn.ShouldBeFalse();

    [Fact]
    public void ProfileProblemsArePlayerFixable()
        => MakeValidator(PassportRequested(), sensitiveDataAllowed: false).Problems.ShouldAllBe(p => p.PlayerCanFix);

    /// <summary>
    /// Проблемы профиля — от настоящего фильтра, полей нет: пометку «держит регистрацию» ставит сама
    /// проблема, а отбор по ней — <see cref="IClaimProblemValidator.ValidateCheckInBlockers"/> по умолчанию.
    /// </summary>
    private sealed class ContactsOnlyValidator : IClaimProblemValidator
    {
        public IEnumerable<ClaimProblem> Validate(ClaimInfo context, ProblemSeverity minimalSeverity = ProblemSeverity.Hint)
            => new ClaimContactsMissingFilter().GetProblems(context);

        public IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(ClaimInfo context) => [];

        public IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(ClaimInfo context, IEnumerable<ProjectFieldIdentification> fields) => [];
    }
}
