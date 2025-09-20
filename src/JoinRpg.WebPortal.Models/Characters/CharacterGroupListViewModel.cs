using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Markdown;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Web.Models.Characters;

public static class CharacterGroupListViewModel
{
    public static IEnumerable<CharacterGroupListItemViewModel> GetGroups(CharacterGroup field, int? currentUserId, ProjectInfo projectInfo)
        => new CharacterGroupHierarchyBuilder(field, currentUserId, projectInfo).Generate().WhereNotNull();

    //TODO: unit tests
    private class CharacterGroupHierarchyBuilder(CharacterGroup root, int? currentUserId, ProjectInfo projectInfo)
    {
        private IList<int> AlreadyOutputedChars { get; } = [];

        private IList<CharacterGroupListItemViewModel> Results { get; } = [];

        public IList<CharacterGroupListItemViewModel> Generate()
        {
            _ = GenerateFrom(root, 0, []);
            return Results;
        }

        private CharacterGroupListItemViewModel? GenerateFrom(CharacterGroup characterGroup, int deepLevel, List<CharacterGroup> pathToTop)
        {
            if (!characterGroup.IsVisible(currentUserId))
            {
                return null;
            }
            var prevCopy = Results.FirstOrDefault(cg => cg.FirstCopy && cg.CharacterGroupId == characterGroup.CharacterGroupId);

            var vm = new CharacterGroupListItemViewModel
            {
                CharacterGroupId = characterGroup.CharacterGroupId,
                DeepLevel = deepLevel,
                Name = characterGroup.CharacterGroupName,
                FirstCopy = prevCopy == null,
                ActiveCharacters =
                prevCopy?.ActiveCharacters ??
                GenerateCharacters(characterGroup)
                  .ToList(),
                Description = characterGroup.Description.ToHtmlString(),
                Path = pathToTop.Select(cg => Results.First(item => item.CharacterGroupId == cg.CharacterGroupId)),
                IsPublic = characterGroup.IsPublic,
                IsSpecial = characterGroup.IsSpecial,
                IsRootGroup = characterGroup.IsRoot,
                ProjectId = characterGroup.ProjectId,
                RootGroupId = root.CharacterGroupId,
            };

            if (root == characterGroup)
            {
                vm.First = true;
                vm.Last = true;
            }

            if (vm.IsSpecial)
            {
                var variant = characterGroup.GetBoundFieldDropdownValueOrDefault();

                if (variant != null)
                {
                    vm.BoundExpression = $"{variant.ProjectField.FieldName} = {variant.Label}";
                }
            }

            Results.Add(vm);

            if (prevCopy != null)
            {
                vm.ChildGroups = prevCopy.ChildGroups;
                return vm;
            }

            var childGroups = characterGroup.GetOrderedChildGroups().OrderBy(g => g.IsSpecial).Where(g => g.IsActive && g.IsVisible(currentUserId)).ToList();
            var pathForChildren = pathToTop.Union([characterGroup]).ToList();

            vm.ChildGroups = childGroups
                .Select(childGroup => GenerateFrom(childGroup, deepLevel + 1, pathForChildren))
                .WhereNotNull()
                .ToList();

            _ = vm.ChildGroups
                .Where(x => !x.IsSpecial)
                .MarkFirstAndLast();

            return vm;
        }

        private IEnumerable<CharacterViewModel> GenerateCharacters(CharacterGroup characterGroup)
        {
            var characters = characterGroup.GetOrderedCharacters().Where(c => c.IsActive && c.IsVisible(currentUserId)).ToArray();

            return characters.Select(character => GenerateCharacter(character, characterGroup, characters));
        }

        private CharacterViewModel GenerateCharacter(Character arg, CharacterGroup group, IReadOnlyList<Character> siblings)
        {
            var acceptingClaims = arg.IsAcceptingClaims(projectInfo);
            var vm = new CharacterViewModel
            {
                CharacterId = arg.CharacterId,
                CharacterName = arg.CharacterName,
                IsFirstCopy = !AlreadyOutputedChars.Contains(arg.CharacterId),
                IsAvailable = acceptingClaims,
                SlotLimit = arg.CharacterSlotLimit,
                Description = arg.Description.ToHtmlString(),
                IsPublic = arg.IsPublic,
                IsActive = arg.IsActive,
                ActiveClaimsCount = arg.Claims.Count(claim => claim.ClaimStatus.IsActive()),
                PlayerLink = arg.GetCharacterPlayerLinkViewModel(currentUserId),
                HasEditRolesAccess = HasEditRolesAccess,
                ProjectId = arg.ProjectId,
                FirstInGroup = siblings[0] == arg,
                LastInGroup = siblings[^1] == arg,
                ParentCharacterGroupId = group.CharacterGroupId,
                RootGroupId = root.CharacterGroupId,
                IsHot = arg.IsHot && acceptingClaims,
            };
            if (vm.IsFirstCopy)
            {
                AlreadyOutputedChars.Add(vm.CharacterId);
            }
            return vm;
        }

        private bool HasEditRolesAccess { get; } = root.HasEditRolesAccess(currentUserId);
    }
}
