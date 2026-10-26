using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain.Problems.ClaimProblemFilters;
using JoinRpg.Domain.Problems.CommonProblemFilters;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Test;

/// <summary>
/// Какие проблемы заявки можно показать игроку (<see cref="ClaimProblem.VisibleToPlayer"/>).
/// </summary>
public class ClaimProblemVisibleToPlayerTest
{
    private MockedProject Mock { get; } = new MockedProject();

    private enum PlayerVisibility
    {
        /// <summary>Работа мастеров с заявкой — игроку не показываем.</summary>
        Hidden,
        /// <summary>Профиль самого игрока — показываем.</summary>
        Profile,
        /// <summary>Проблема поля — показываем, только если игрок видит поле.</summary>
        IfPlayerSeesField,
    }

    /// <summary>
    /// Явное решение по каждому типу. Ветки по умолчанию нет нарочно: новое значение
    /// <see cref="ClaimProblemType"/> роняет тест-страж, пока его здесь не классифицируют.
    /// </summary>
    private static PlayerVisibility Decision(ClaimProblemType problemType) => problemType switch
    {
        ClaimProblemType.NoResponsibleMaster => PlayerVisibility.Hidden,
        ClaimProblemType.InvalidResponsibleMaster => PlayerVisibility.Hidden,
        ClaimProblemType.ClaimNeverAnswered => PlayerVisibility.Hidden,
        ClaimProblemType.ClaimNoDecision => PlayerVisibility.Hidden,
        ClaimProblemType.ClaimActiveButCharacterHasApprovedClaim => PlayerVisibility.Hidden,
        ClaimProblemType.FinanceModerationRequired => PlayerVisibility.Hidden,
        ClaimProblemType.TooManyMoney => PlayerVisibility.Hidden,
        ClaimProblemType.ClaimDiscussionStopped => PlayerVisibility.Hidden,
        ClaimProblemType.NoCharacterOnApprovedClaim => PlayerVisibility.Hidden,
        // Игроку про недоплаченный взнос знать полезно, но взнос он видит в блоке финансов.
        ClaimProblemType.FeePaidPartially => PlayerVisibility.Hidden,
        ClaimProblemType.UnApprovedClaimPayment => PlayerVisibility.Hidden,
        ClaimProblemType.ClaimWorkStopped => PlayerVisibility.Hidden,
        ClaimProblemType.NoParentGroup => PlayerVisibility.Hidden,
        ClaimProblemType.GroupIsBroken => PlayerVisibility.Hidden,
        ClaimProblemType.FieldIsEmpty => PlayerVisibility.IfPlayerSeesField,
        // «Поле не должно иметь значения» и «выбран неактивный вариант» — тоже проблемы поля:
        // приходят как FieldRelatedProblem и наследуют его правило.
        ClaimProblemType.FieldShouldNotHaveValue => PlayerVisibility.IfPlayerSeesField,
        ClaimProblemType.InActiveVariant => PlayerVisibility.IfPlayerSeesField,
        ClaimProblemType.MissingTelegram => PlayerVisibility.Profile,
        ClaimProblemType.MissingVkontakte => PlayerVisibility.Profile,
        ClaimProblemType.MissingPhone => PlayerVisibility.Profile,
        ClaimProblemType.MissingRealname => PlayerVisibility.Profile,
        ClaimProblemType.MissingPassport => PlayerVisibility.Profile,
        ClaimProblemType.MissingRegistrationAddress => PlayerVisibility.Profile,
        ClaimProblemType.SensitiveDataNotAllowed => PlayerVisibility.Profile,
        ClaimProblemType.MissingBirthDate => PlayerVisibility.Profile,
        _ => throw new ArgumentOutOfRangeException(nameof(problemType), problemType,
            "Не решено, видна ли проблема игроку: классифицируйте новый тип в ClaimProblemVisibleToPlayerTest.Decision"),
    };

    [Theory]
    [ClassData(typeof(EnumTheoryDataGenerator<ClaimProblemType>))]
    public void EveryProblemTypeHasPlayerVisibilityDecision(ClaimProblemType problemType)
    {
        if (problemType.GetAttribute<ObsoleteAttribute>() is not null)
        {
            return;
        }

        switch (Decision(problemType))
        {
            case PlayerVisibility.Hidden:
                new ClaimProblem(problemType, ProblemSeverity.Error).VisibleToPlayer.ShouldBeFalse();
                break;
            case PlayerVisibility.Profile:
                new ProfileRelatedProblem(problemType, ProblemSeverity.Warning).VisibleToPlayer.ShouldBeTrue();
                break;
            case PlayerVisibility.IfPlayerSeesField:
                new FieldRelatedProblem(problemType, ProblemSeverity.Warning, Mock.CharacterFieldInfo).VisibleToPlayer.ShouldBeTrue();
                new FieldRelatedProblem(problemType, ProblemSeverity.Warning, Mock.MasterOnlyFieldInfo).VisibleToPlayer.ShouldBeFalse();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(problemType));
        }
    }

    [Fact]
    public void EmptyMasterOnlyFieldIsHiddenFromPlayer()
        => EmptyFieldProblem(ProjectFieldVisibility.MasterOnly).VisibleToPlayer.ShouldBeFalse();

    [Fact]
    public void EmptyFieldVisibleToPlayerIsShownToPlayer()
        => EmptyFieldProblem(ProjectFieldVisibility.PlayerAndMaster).VisibleToPlayer.ShouldBeTrue();

    [Fact]
    public void MissingProfileDataIsShownToPlayer()
    {
        var everythingRequired = new ProjectProfileRequirementSettings(
            MandatoryStatus.Required, MandatoryStatus.Required, MandatoryStatus.Required, MandatoryStatus.Required,
            MandatoryStatus.Required, MandatoryStatus.Required, MandatoryStatus.Required);

        var problems = ProfileProblems(everythingRequired, sensitiveDataAllowed: true);

        problems.ShouldContain(p => p.ProblemType == ClaimProblemType.MissingPassport);
        problems.ShouldContain(p => p.ProblemType == ClaimProblemType.MissingPhone);
        problems.ShouldAllBe(p => p.VisibleToPlayer);
    }

    [Fact]
    public void SensitiveDataNotAllowedIsShownToPlayer()
    {
        var passportRequested = ProjectProfileRequirementSettings.AllNotRequired with { RequirePassport = MandatoryStatus.Recommended };

        ProfileProblems(passportRequested, sensitiveDataAllowed: false)
            .ShouldContain(p => p.ProblemType == ClaimProblemType.SensitiveDataNotAllowed && p.VisibleToPlayer);
    }

    /// <summary>
    /// Проблема незаполненного обязательного поля — от настоящего фильтра.
    /// </summary>
    private FieldRelatedProblem EmptyFieldProblem(ProjectFieldVisibility visibility)
    {
        var field = Mock.CreateField("Обязательное поле", visibility, mandatoryStatus: MandatoryStatus.Required);
        var character = Mock.GetCharacterInfo(Mock.Character, Mock.ProjectInfo);

        return new FieldNotSetFilter().CheckField(character, new FieldWithValue(field, value: null)).ShouldHaveSingleItem();
    }

    private ClaimProblem[] ProfileProblems(ProjectProfileRequirementSettings settings, bool sensitiveDataAllowed)
    {
        var projectInfo = Mock.ProjectInfo.WithProfileRequirementSettings(settings);
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);
        claim.PlayerAllowedSenstiveData = sensitiveDataAllowed;
        var character = Mock.GetCharacterInfo(Mock.Character, projectInfo);
        var context = new ClaimInfo(new ClaimInCharacter(character, claim.GetId()), Mock.PlayerInfo);

        return [.. new ClaimContactsMissingFilter().GetProblems(context)];
    }
}
