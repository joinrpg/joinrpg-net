using JoinRpg.DataModel;
using JoinRpg.DataModel.Extensions;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.Domain.Problems;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.Models.Claims;

namespace JoinRpg.WebPortal.Models.Test;

/// <summary>
/// Что попадает в виджет «Проблемы заявки» в зависимости от того, кто смотрит.
/// </summary>
public class ClaimProblemsPanelModelTest
{
    private readonly MockedProject mock = new();
    private readonly ClaimInfo claimInfo;
    private readonly FixedProblemsValidator validator;
    private readonly ProjectFieldInfo masterField;
    private readonly ProjectFieldInfo playerField;

    public ClaimProblemsPanelModelTest()
    {
        // Флаг ShowCheckInBlockersToPlayers читается из сущности проекта — пересобираем метаданные до заведения поля.
        mock.Project.Details.EnableCheckInModule = true;
        mock.ReInitProjectInfo();
        masterField = mock.CreateField("Мастерское", ProjectFieldVisibility.MasterOnly, mandatoryStatus: MandatoryStatus.Required);
        playerField = mock.CreateField("Игроцкое", ProjectFieldVisibility.PlayerAndMaster,
            canPlayerEdit: true, showOnUnApprovedClaims: true, mandatoryStatus: MandatoryStatus.Required);

        claimInfo = ToClaimInfo(mock.CreateApprovedClaim(mock.Character, mock.Player));

        validator = new FixedProblemsValidator([
            new ClaimProblem(ClaimProblemType.NoResponsibleMaster, ProblemSeverity.Error),
            new ProfileRelatedProblem(ClaimProblemType.MissingPhone, ProblemSeverity.Warning),
            new FieldRelatedProblem(ClaimProblemType.FieldIsEmpty, ProblemSeverity.Warning, masterField),
        ]);
    }

    private ClaimInfo ToClaimInfo(Claim claim)
        => new(new ClaimInCharacter(mock.GetCharacterInfo(claim.Character, mock.ProjectInfo), claim.GetId()), mock.PlayerInfo);

    private ClaimProblemsPanelModel Create(bool hasMasterAccess, bool isMyClaim) => Create(claimInfo, hasMasterAccess, isMyClaim);

    private ClaimProblemsPanelModel Create(ClaimInfo info, bool hasMasterAccess, bool isMyClaim)
        => ClaimProblemsPanelModel.Create(info, validator, new ClaimCheckInValidator(info, validator), hasMasterAccess, isMyClaim);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ApprovedClaimMarksCheckInBlockers(bool hasMasterAccess)
    {
        var model = Create(hasMasterAccess, isMyClaim: true);

        model.Problems.Single(p => p.ProblemType == ClaimProblemType.MissingPhone).BlocksCheckIn.ShouldBeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NotApprovedClaimHasNoCheckInBlockers(bool hasMasterAccess)
    {
        var model = Create(ToClaimInfo(mock.CreateClaim(mock.Character, mock.Player)), hasMasterAccess, isMyClaim: true);

        model.Problems.ShouldNotBeEmpty();
        model.Problems.ShouldAllBe(p => !p.BlocksCheckIn);
    }

    [Fact]
    public void CheckedInClaimHasNoCheckInBlockers()
    {
        var model = Create(ToClaimInfo(mock.CreateCheckedInClaim(mock.CreateCharacter("Зарегистрированный"), mock.Player)), hasMasterAccess: true, isMyClaim: false);

        model.Problems.ShouldNotBeEmpty();
        model.Problems.ShouldAllBe(p => !p.BlocksCheckIn);
    }

    [Fact]
    public void PlayerOnApprovedClaimSeesOwnFieldAsCheckInBlocker()
    {
        IClaimProblemValidator fieldValidator = new FixedProblemsValidator([
            new FieldRelatedProblem(ClaimProblemType.FieldIsEmpty, ProblemSeverity.Warning, playerField),
        ]);

        var model = ClaimProblemsPanelModel.Create(claimInfo, fieldValidator,
            new ClaimCheckInValidator(claimInfo, fieldValidator), hasMasterAccess: false, isMyClaim: true);

        model.Problems.ShouldHaveSingleItem().BlocksCheckIn.ShouldBeTrue();
    }

    /// <summary>
    /// Без настройки «что мешает регистрации» игрок видит проблему профиля, но не пометку, что она
    /// держит регистрацию; мастеру пометка остаётся.
    /// </summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void FlagOffHidesCheckInMarkFromPlayerOnly(bool hasMasterAccess, bool expectedMark)
    {
        mock.Project.Details.EnableCheckInModule = false;
        mock.ReInitProjectInfo();
        var info = ToClaimInfo(mock.CreateApprovedClaim(mock.CreateCharacter("Без настройки"), mock.Player));

        var model = Create(info, hasMasterAccess, isMyClaim: true);

        model.Problems.Single(p => p.ProblemType == ClaimProblemType.MissingPhone).BlocksCheckIn.ShouldBe(expectedMark);
    }

    [Fact]
    public void PlayerOnCheckedInClaimGetsNoHiddenHint()
    {
        IClaimProblemValidator fieldValidator = new FixedProblemsValidator([
            new FieldRelatedProblem(ClaimProblemType.FieldIsEmpty, ProblemSeverity.Warning, masterField),
        ]);
        var checkedIn = ToClaimInfo(mock.CreateCheckedInClaim(mock.CreateCharacter("Зарегистрированный"), mock.Player));

        var model = ClaimProblemsPanelModel.Create(checkedIn, fieldValidator,
            new ClaimCheckInValidator(checkedIn, fieldValidator), hasMasterAccess: false, isMyClaim: true);

        model.HasHiddenCheckInBlockers.ShouldBeFalse();
    }

    [Fact]
    public void MasterSeesAllProblemsWithoutHiddenHint()
    {
        var model = Create(hasMasterAccess: true, isMyClaim: false);

        model.Problems.Select(p => p.ProblemType).ShouldBe(
            [ClaimProblemType.NoResponsibleMaster, ClaimProblemType.MissingPhone, ClaimProblemType.FieldIsEmpty],
            ignoreOrder: true);
        model.HasHiddenCheckInBlockers.ShouldBeFalse();
        model.OwnerActions.ShouldBeNull();
    }

    [Fact]
    public void PlayerSeesOwnProblemsAndHiddenHint()
    {
        var model = Create(hasMasterAccess: false, isMyClaim: true);

        model.Problems.Select(p => p.ProblemType).ShouldBe([ClaimProblemType.MissingPhone]);
        model.HasHiddenCheckInBlockers.ShouldBeTrue();
        model.OwnerActions.ShouldNotBeNull();
    }

    [Fact]
    public void MasterOwnClaimGetsAllProblemsAndOwnerActions()
    {
        var model = Create(hasMasterAccess: true, isMyClaim: true);

        model.Problems.ShouldContain(p => p.ProblemType == ClaimProblemType.NoResponsibleMaster);
        model.OwnerActions.ShouldNotBeNull();
    }

    [Fact]
    public void StrangerSeesNothing()
        => Create(hasMasterAccess: false, isMyClaim: false).IsEmpty.ShouldBeTrue();

    /// <summary>
    /// Отдаёт заданные проблемы; выборку для игрока считает настоящий <see cref="IClaimProblemValidator.ValidateForPlayer"/>.
    /// </summary>
    private sealed class FixedProblemsValidator(IReadOnlyCollection<ClaimProblem> problems) : IClaimProblemValidator
    {
        public IEnumerable<ClaimProblem> Validate(ClaimInfo context, ProblemSeverity minimalSeverity = ProblemSeverity.Hint)
            => problems;

        public IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(ClaimInfo context) => throw new NotSupportedException();

        public IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(ClaimInfo context, IEnumerable<ProjectFieldIdentification> fields)
            => throw new NotSupportedException();
    }
}
