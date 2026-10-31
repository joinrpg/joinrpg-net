using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain.Problems;
using JoinRpg.Domain.Problems.ClaimProblemFilters;
using JoinRpg.Domain.Problems.CommonProblemFilters;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Test;

/// <summary>
/// Какие проблемы заявки видит сам игрок (<see cref="IClaimProblemValidator.ValidateForPlayer"/>).
/// </summary>
public class ClaimProblemsForPlayerTest
{
    private MockedProject Mock { get; } = new MockedProject();

    private ProjectFieldInfo PlayerField { get; set; } = null!;
    private ProjectFieldInfo MasterField { get; set; } = null!;

    public enum ClaimState { NotApproved, Approved, CheckedIn }

    private PlayerClaimProblems ValidateForPlayer(bool showCheckInBlockersToPlayers, ClaimState state = ClaimState.Approved)
    {
        Mock.Project.Details.EnableCheckInModule = showCheckInBlockersToPlayers;

        // Поля заявки, а не персонажа: их проверяют при любом статусе заявки, поля персонажа — только
        // у принятой. Именно AddField: он кладёт поле в Project и пересобирает метаданные, подхватывая флаг.
        PlayerField = AddRequiredClaimField(field =>
        {
            field.CanPlayerView = true;
            field.CanPlayerEdit = true;
            field.ShowOnUnApprovedClaims = true;
        });
        MasterField = AddRequiredClaimField(field => field.CanPlayerView = false);

        var projectInfo = Mock.ProjectInfo.WithProfileRequirementSettings(
            ProjectProfileRequirementSettings.AllNotRequired with { RequirePhone = MandatoryStatus.Required });

        var claim = state switch
        {
            ClaimState.NotApproved => Mock.CreateClaim(Mock.Character, Mock.Player),
            ClaimState.Approved => Mock.CreateApprovedClaim(Mock.Character, Mock.Player),
            ClaimState.CheckedIn => Mock.CreateCheckedInClaim(Mock.Character, Mock.Player),
            _ => throw new ArgumentOutOfRangeException(nameof(state)),
        };
        var character = Mock.GetCharacterInfo(Mock.Character, projectInfo);
        var context = new ClaimInfo(new ClaimInCharacter(character, claim.GetId()), Mock.PlayerInfo);

        IClaimProblemValidator validator = new ClaimProblemValidator(
            [new ClaimContactsMissingFilter(), new FixedProblemsFilter()],
            [new FieldNotSetFilter()]);

        return validator.ValidateForPlayer(context);
    }

    private ProjectFieldInfo AddRequiredClaimField(Action<ProjectField> setup)
        => Mock.AddField(field =>
        {
            field.FieldType = ProjectFieldType.String;
            field.FieldBoundTo = FieldBoundTo.Claim;
            field.MandatoryStatus = MandatoryStatus.Required;
            setup(field);
        });

    private static bool IsEmptyField(ClaimProblem problem, ProjectFieldInfo field)
        => problem is FieldRelatedProblem { ProblemType: ClaimProblemType.FieldIsEmpty } fieldProblem && fieldProblem.Field.Id == field.Id;

    [Fact]
    public void FlagOffShowsOnlyProfileProblems()
    {
        var result = ValidateForPlayer(showCheckInBlockersToPlayers: false);

        result.Problems.ShouldNotContain(p => p is FieldRelatedProblem);
        result.Problems.ShouldNotContain(p => p.ProblemType == ClaimProblemType.FeePaidPartially);
        result.Problems.ShouldContain(p => p.ProblemType == ClaimProblemType.MissingPhone);
        result.HasHiddenCheckInBlockers.ShouldBeFalse();
    }

    [Fact]
    public void FlagOnShowsFee()
        => ValidateForPlayer(showCheckInBlockersToPlayers: true)
            .Problems.ShouldContain(p => p.ProblemType == ClaimProblemType.FeePaidPartially);

    [Fact]
    public void FlagOnShowsEmptyPlayerField()
        => ValidateForPlayer(showCheckInBlockersToPlayers: true)
            .Problems.ShouldContain(p => IsEmptyField(p, PlayerField));

    [Fact]
    public void FlagOnHidesEmptyMasterFieldButReportsHiddenBlocker()
    {
        var result = ValidateForPlayer(showCheckInBlockersToPlayers: true);

        result.Problems.ShouldNotContain(p => IsEmptyField(p, MasterField));
        result.HasHiddenCheckInBlockers.ShouldBeTrue();
    }

    /// <summary>
    /// Регистрация предстоит только принятой заявке: на непринятой мастерское поле держит её лишь
    /// формально, а зарегистрированной держать уже нечего — совет обратиться к мастеру ввёл бы
    /// игрока в заблуждение.
    /// </summary>
    [Theory]
    [InlineData(ClaimState.NotApproved)]
    [InlineData(ClaimState.CheckedIn)]
    public void NoCheckInAheadDoesNotReportHiddenBlocker(ClaimState state)
        => ValidateForPlayer(showCheckInBlockersToPlayers: true, state)
            .HasHiddenCheckInBlockers.ShouldBeFalse();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MasterSideProblemsAreNeverShown(bool showCheckInBlockersToPlayers)
        => ValidateForPlayer(showCheckInBlockersToPlayers)
            .Problems.ShouldNotContain(p => p.ProblemType == ClaimProblemType.NoResponsibleMaster);

    /// <summary>
    /// Проблема автоприёма — объяснение игроку, почему его заявка ещё не принята автоматически:
    /// она видна ему и без настройки, показывающей проблемы регистрации.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AutoAcceptProblemIsShownRegardlessOfFlag(bool showCheckInBlockersToPlayers)
        => ValidateForPlayer(showCheckInBlockersToPlayers)
            .Problems.ShouldContain(p => p.ProblemType == ClaimProblemType.AutoAcceptSlotExhausted);

    /// <summary>
    /// Мастерская проблема, деньги игрока и проблема автоприёма — без настоящих данных
    /// о мастерах, платежах и лимитах шаблонов.
    /// </summary>
    private sealed class FixedProblemsFilter : IClaimProblemFilter
    {
        public IEnumerable<ClaimProblem> GetProblems(ClaimInfo context)
            => [
                new ClaimProblem(ClaimProblemType.NoResponsibleMaster, ProblemSeverity.Error),
                new ClaimProblem(ClaimProblemType.FeePaidPartially, ProblemSeverity.Warning),
                new AutoAcceptRelatedProblem(ClaimProblemType.AutoAcceptSlotExhausted, ProblemSeverity.Warning),
            ];
    }
}
