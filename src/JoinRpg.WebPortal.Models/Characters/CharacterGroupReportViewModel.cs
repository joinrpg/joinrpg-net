using JoinRpg.DataModel;
using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Web.Models.Characters;

public static class CharacterGroupReportViewModel
{
    public static IEnumerable<CharacterGroupReportItemViewModel> GetGroups(CharacterGroup field) => new CharacterGroupHierarchyBuilder(field).Generate().WhereNotNull();

    //TODO: unit tests
    private class CharacterGroupHierarchyBuilder
    {
        private CharacterGroup Root { get; }

        private IList<CharacterGroupReportItemViewModel> Results { get; } =
            new List<CharacterGroupReportItemViewModel>();

        public CharacterGroupHierarchyBuilder(CharacterGroup root) => Root = root;

        public IList<CharacterGroupReportItemViewModel> Generate()
        {
            GenerateFrom(Root, 0);
            foreach (var characterGroup in Root.ChildGroups.Where(cg => cg.IsActive))
            {
                GenerateFrom(characterGroup, 1);
            }

            return Results;
        }

        private void GenerateFrom(CharacterGroup characterGroup, int deepLevel)
        {
            var vm = new CharacterGroupReportItemViewModel
            {
                CharacterGroupId = characterGroup.CharacterGroupId,
                DeepLevel = deepLevel,
                Name = characterGroup.CharacterGroupName,
                ActiveClaimsCount = characterGroup.Claims.Count(c => c.ClaimStatus.IsActive()),
                IsPublic = characterGroup.IsPublic,
            };

            Results.Add(vm);

            var flatChilds = characterGroup.FlatTree(model => model.ChildGroups.Where(c => c.IsActive)).Distinct().ToList();

            var flatCharacters = flatChilds.SelectMany(c => c.Characters).Where(c => c.IsActive).Distinct().ToList();

            vm.TotalSlots = CountSlotsForGroups(flatChilds) + CountSlotsForCharacters(flatCharacters);
            vm.TotalFreeSlots = CountSlotsForGroups(flatChilds) + CountFreeSlotsForCharacters(flatCharacters);

            vm.TotalCharacters = flatCharacters.Count;
            vm.TotalNpcCharacters = flatCharacters.Count(c => c.CharacterType == CharacterType.NonPlayer);
            vm.TotalCharactersWithPlayers = flatCharacters.Count(c => c.ApprovedClaim != null);
            vm.TotalInGameCharacters = flatCharacters.Count(c => c.InGame);

            vm.TotalDiscussedClaims =
                flatCharacters.Where(c => c.ApprovedClaim == null)
                    .Sum(c => c.Claims.Count(claim => claim.ClaimStatus.IsActive())) +

                flatChilds.Sum(c => c.Claims.Count());
            vm.TotalActiveClaims =
                flatCharacters.Sum(c => c.Claims.Count(claim => claim.ClaimStatus.IsActive())) +
                flatChilds.Sum(c => c.Claims.Count());
            vm.TotalAcceptedClaims = flatCharacters.Count(c => c.ApprovedClaim != null);
            vm.TotalCheckedInClaims =
                flatCharacters.Count(c => c.ApprovedClaim?.CheckInDate != null);
            vm.Unlimited = characterGroup.AvaiableDirectSlots == -1 ||
                           flatChilds.Any(c => c.AvaiableDirectSlots == -1) || flatCharacters.Any(c => c.CharacterType == CharacterType.Slot && c.CharacterSlotLimit is null);
        }

        private static int CountSlotsForCharacters(List<Character> flatCharacters) => flatCharacters.Sum(c => CharacterSlotCount(c));

        private static int CountFreeSlotsForCharacters(List<Character> flatCharacters) => flatCharacters.Sum(c => CalculateFreeCount(c));
        private static int CountSlotsForGroups(List<CharacterGroup> flatChilds) => flatChilds.Sum(c => GroupSlotCount(c));
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

        private static int GroupSlotCount(CharacterGroup c) => c.AvaiableDirectSlots == -1 ? 0 : c.AvaiableDirectSlots;
    }
}
