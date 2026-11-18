using JoinRpg.DomainTypes;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Web.CharacterGroups.GroupReport;

public interface IGroupReportClient
{
    Task<GroupReportViewModel?> GetReport(CharacterGroupIdentification groupId);
}

/// <param name="CheckInInProgress">Идёт регистрация — показывать колонки «В игре» и «Заехало»</param>
public record GroupReportViewModel(
    CharacterGroupIdentification GroupId,
    bool CheckInInProgress,
    IReadOnlyList<GroupReportRowViewModel> Rows);

/// <summary>
/// Строка, у которой <see cref="Group"/>.CharacterGroupId совпадает с
/// <see cref="GroupReportViewModel.GroupId"/> — итоговая строка по всему дереву (отображается как «итого»).
/// </summary>
public record GroupReportRowViewModel(
    CharacterGroupLinkSlimViewModel Group,
    int TotalCharacters,
    int TotalSlots,
    bool Unlimited,
    int TotalNpcCharacters,
    int TotalCharactersWithPlayers,
    int TotalFreeSlots,
    int TotalInGameCharacters,
    int TotalActiveClaims,
    int TotalAcceptedClaims,
    int TotalCheckedInClaims,
    int TotalDiscussedClaims);
