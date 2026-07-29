using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Web.CharacterGroups.GroupReport;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.WebPortal.Managers.CharacterGroups;

internal static class GroupReportViewModelBuilder
{
    public static GroupReportViewModel Build(CharacterGroup rootGroup, bool checkinModuleEnabled)
    {
        var rows = new List<GroupReportRowViewModel> { BuildRow(rootGroup) };
        rows.AddRange(rootGroup.ChildGroups.Where(cg => cg.IsActive).Select(BuildRow));

        return new GroupReportViewModel(rootGroup.GetId(), checkinModuleEnabled, rows);
    }

    private static GroupReportRowViewModel BuildRow(CharacterGroup characterGroup)
    {
        var flatChilds = characterGroup.FlatTree(model => model.ChildGroups.Where(c => c.IsActive)).Distinct().ToList();
        var flatCharacters = flatChilds.SelectMany(c => c.Characters).Where(c => c.IsActive).Distinct().ToList();

        return new GroupReportRowViewModel(
            Group: new CharacterGroupLinkSlimViewModel(characterGroup.GetId(), characterGroup.CharacterGroupName, characterGroup.IsPublic, characterGroup.IsActive),
            TotalCharacters: flatCharacters.Count,
            TotalSlots: flatCharacters.Sum(CharacterSlotCount),
            Unlimited: flatCharacters.Any(c => c.CharacterType == CharacterType.Slot && c.CharacterSlotLimit is null),
            TotalNpcCharacters: flatCharacters.Count(c => c.CharacterType == CharacterType.NonPlayer),
            TotalCharactersWithPlayers: flatCharacters.Count(c => c.ApprovedClaim != null),
            TotalFreeSlots: flatCharacters.Sum(CalculateFreeCount),
            TotalInGameCharacters: flatCharacters.Count(c => c.InGame),
            TotalActiveClaims: flatCharacters.Sum(c => c.Claims.Count(claim => claim.ClaimStatus.IsActive())),
            TotalAcceptedClaims: flatCharacters.Count(c => c.ApprovedClaim != null),
            TotalCheckedInClaims: flatCharacters.Count(c => c.ApprovedClaim?.CheckInDate != null),
            TotalDiscussedClaims: flatCharacters.Where(c => c.ApprovedClaim == null)
                .Sum(c => c.Claims.Count(claim => claim.ClaimStatus.IsActive())));
    }

    private static int CharacterSlotCount(Character c) =>
        c.CharacterType switch
        {
            CharacterType.Slot => c.CharacterSlotLimit ?? 1,
            CharacterType.Player => 1,
            CharacterType.NonPlayer => 1,
            _ => throw new InvalidOperationException(),
        };

    private static int CalculateFreeCount(Character c) =>
        (c.CharacterType, c.ApprovedClaim) switch
        {
            (_, not null) => 0,
            (CharacterType.Slot, null) => c.CharacterSlotLimit ?? 0,
            (CharacterType.NonPlayer, null) => 0,
            (CharacterType.Player, null) => 1,
            _ => throw new InvalidOperationException(),
        };
}
