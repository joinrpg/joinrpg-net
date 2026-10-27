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
public record ClaimProblemsPanelModel(
    IReadOnlyCollection<ProblemViewModel> Problems,
    bool HasHiddenCheckInBlockers,
    RequestedSensitiveDataView? OwnerActions)
{
    public bool IsEmpty => Problems.Count == 0 && !HasHiddenCheckInBlockers;

    /// <param name="checkIn">
    /// Регистрация той же заявки: пометка «держит регистрацию» ставится, только если регистрация
    /// ещё предстоит (<see cref="ClaimCheckInValidator.CheckInAhead"/>), а игроку — ещё и при настройке
    /// <see cref="DomainTypes.ProjectMetadata.ProjectClaimSettings.ShowCheckInBlockersToPlayers"/>.
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
            return new(ToViewModels(problemValidator.Validate(claimInfo), checkInAhead), HasHiddenCheckInBlockers: false, ownerActions);
        }

        if (isMyClaim)
        {
            var forPlayer = problemValidator.ValidateForPlayer(claimInfo);
            // Без настройки игроку не говорим, что мешает регистрации, — и пометку «держит регистрацию» тоже не ставим.
            var markBlockers = checkInAhead && claimInfo.ProjectInfo.ClaimSettings.ShowCheckInBlockersToPlayers;
            return new(ToViewModels(forPlayer.Problems, markBlockers), forPlayer.HasHiddenCheckInBlockers, ownerActions);
        }

        return new([], HasHiddenCheckInBlockers: false, OwnerActions: null);
    }

    private static List<ProblemViewModel> ToViewModels(IEnumerable<ClaimProblem> problems, bool markBlockers)
        => [.. problems.Select(p => new ProblemViewModel(p) with { BlocksCheckIn = p.BlocksCheckIn && markBlockers })];
}
