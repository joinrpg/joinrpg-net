using JoinRpg.DomainTypes;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Web.CharacterGroups.GroupReport;

public interface IGroupReportClient
{
    Task<GroupReportViewModel?> GetReport(CharacterGroupIdentification groupId);
}

public record GroupReportViewModel(
    CharacterGroupIdentification GroupId,
    bool CheckinModuleEnabled,
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
