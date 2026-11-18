using JoinRpg.Domain;
using JoinRpg.Domain.Problems;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Web.Claims;

namespace JoinRpg.Web.Models.Claims;

/// <summary>
/// Данные для виджета «Проблемы заявки» (<see cref="ClaimProblemsPanel"/>) глазами конкретного зрителя.
/// </summary>
/// <param name="Problems">Мастеру — все проблемы заявки, владельцу — только видимые игроку.</param>
/// <param name="HasHiddenCheckInBlockers">Регистрацию держат поля, которых игрок не видит (только для игрока).</param>
/// <param name="OwnerActions">Задано, если смотрит владелец заявки: ему доступны кнопки профиля.</param>
/// <param name="ShowAutoAcceptButton">
/// Показать владельцу кнопку «Автопринять мою заявку»: заявка — кандидат на автоприём, и все его
/// блокеры уже сняты (что показывать, когда не сняты, — знают сами проблемы, см.
/// <see cref="ClaimProblem.IsAutoAcceptBlocker"/>).
/// </param>
public record ClaimProblemsPanelModel(
    IReadOnlyCollection<ProblemViewModel> Problems,
    bool HasHiddenCheckInBlockers,
    RequestedSensitiveDataView? OwnerActions,
    bool ShowAutoAcceptButton)
{
    public bool IsEmpty => Problems.Count == 0 && !HasHiddenCheckInBlockers && !ShowAutoAcceptButton;

    /// <param name="checkIn">
    /// Регистрация той же заявки: пометка «держит регистрацию» ставится, только если регистрация
    /// ещё предстоит (<see cref="ClaimCheckInValidator.CheckInAhead"/>), а игроку — ещё и при настройке
    /// <see cref="DomainTypes.ProjectMetadata.ProjectCheckInSettings.ShowCheckInBlockersToPlayers"/>.
    /// </param>
    public static ClaimProblemsPanelModel Create(
        ClaimInfo claimInfo,
        IClaimProblemValidator problemValidator,
        ClaimCheckInValidator checkIn,
        bool hasMasterAccess,
        bool isMyClaim)
    {
        ArgumentNullException.ThrowIfNull(claimInfo);
        ArgumentNullException.ThrowIfNull(problemValidator);
        ArgumentNullException.ThrowIfNull(checkIn);

        var ownerActions = isMyClaim
            ? RequestedSensitiveDataView.From(claimInfo.ProjectInfo.ProfileRequirementSettings)
            : null;
        var checkInAhead = checkIn.CheckInAhead;

        if (hasMasterAccess)
        {
            return new(ToViewModels(problemValidator.Validate(claimInfo), checkInAhead), HasHiddenCheckInBlockers: false, ownerActions, ShowAutoAcceptButton: false);
        }

        if (isMyClaim)
        {
            var forPlayer = problemValidator.ValidateForPlayer(claimInfo);
            // Без настройки игроку не говорим, что мешает регистрации, — и пометку «держит регистрацию» тоже не ставим.
            var markBlockers = checkInAhead && claimInfo.ProjectInfo.ProjectCheckInSettings.ShowCheckInBlockersToPlayers;
            // Мастеру кнопка не нужна — у него есть «Принять».
            var showAutoAcceptButton = claimInfo.IsAutoAcceptCandidate()
                && !problemValidator.Validate(claimInfo).Any(problem => problem.IsAutoAcceptBlocker);
            return new(ToViewModels(forPlayer.Problems, markBlockers), forPlayer.HasHiddenCheckInBlockers, ownerActions, showAutoAcceptButton);
        }

        return new([], HasHiddenCheckInBlockers: false, OwnerActions: null, ShowAutoAcceptButton: false);
    }

    private static List<ProblemViewModel> ToViewModels(IEnumerable<ClaimProblem> problems, bool markBlockers)
        => [.. problems.Select(p => new ProblemViewModel(p) with { BlocksCheckIn = p.BlocksCheckIn && markBlockers })];
}
