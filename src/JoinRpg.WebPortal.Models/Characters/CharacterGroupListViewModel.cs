using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Helpers;
using JoinRpg.Markdown;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Web.Models.Characters;

public static class CharacterGroupListViewModel
{
    /// <param name="characters">
    /// Персонажи поддерева <paramref name="field"/> доменными агрегатами (ADR013). Дерево групп
    /// пока берётся из EF: порядок дочерних групп (<c>ChildGroupsOrdering</c>) в <c>ProjectInfo</c>
    /// не переехал, а менять порядок в этом JSON нельзя — им пользуются внешние сайты игр.
    /// </param>
    public static IEnumerable<CharacterGroupListItemViewModel> GetGroups(
        CharacterGroup field,
        IReadOnlyCollection<CharacterInfo> characters,
        UserIdentification? currentUserId,
        ProjectInfo projectInfo)
        => new CharacterGroupHierarchyBuilder(field, characters, currentUserId, projectInfo).Generate().WhereNotNull();

    private class CharacterGroupHierarchyBuilder(
        CharacterGroup root,
        IReadOnlyCollection<CharacterInfo> characters,
        UserIdentification? currentUserId,
        ProjectInfo projectInfo)
    {
        private readonly ILookup<CharacterGroupIdentification, CharacterInfo> charactersByGroup = characters
            .SelectMany(character => character.DirectGroupIds.Select(groupId => (groupId, character)))
            .ToLookup(x => x.groupId, x => x.character);

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
                Description = ((MarkdownString?)characterGroup.Description).ToHtmlString(),
                Path = pathToTop.Select(cg => Results.First(item => item.CharacterGroupId == cg.CharacterGroupId)),
                IsPublic = characterGroup.IsPublic,
                GroupType = ProjectInfo.GetGroupById(characterGroup.GetId()).GroupType,
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
                var variant = ProjectInfo.GetVariantByGroupIdOrDefault(characterGroup.GetId());

                if (variant != null)
                {
                    vm.BoundExpression = $"{variant.ParentFieldName} = {variant.Label}";
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
            var groupInfo = ProjectInfo.GetGroupById(characterGroup.GetId());

            // Порядок тот же, что давал GetOrderedCharacters: сохранённый порядок группы поверх
            // идентификаторов персонажей.
            var characters = charactersByGroup[groupInfo.Id]
                .OrderByStoredOrder(character => character.Id.CharacterId, groupInfo.ChildCharactersOrdering)
                .Where(character => character.IsActive && IsVisible(character));

            return characters.Select(GenerateCharacter);
        }

        /// <summary>
        /// Зеркало <c>WorldObjectExtensions.IsVisible</c> для персонажа поверх агрегата.
        /// </summary>
        private bool IsVisible(CharacterInfo character)
            => IsPublic(character) || projectInfo.PublishPlot || projectInfo.HasMasterAccess(currentUserId);

        /// <summary>
        /// Публичность в смысле колонки <c>Character.IsPublic</c>.
        /// </summary>
        /// <remarks>
        /// Именно так, а не через <c>CharacterInfo.IsPublic</c>: тот означает
        /// <c>CharacterVisibility.Public</c>, а у публичного персонажа со скрытым игроком
        /// видимость — <c>PlayerHidden</c>. Он публичный, просто игрок не показывается; иначе
        /// такие персонажи исчезли бы из публичного JSON.
        /// </remarks>
        private static bool IsPublic(CharacterInfo character)
            => character.CharacterTypeInfo.CharacterVisibility != CharacterVisibility.Private;

        private CharacterViewModel GenerateCharacter(CharacterInfo arg)
        {
            var vm = new CharacterViewModel
            {
                CharacterId = arg.Id.CharacterId,
                CharacterName = arg.CharacterName,
                IsFirstCopy = !AlreadyOutputedChars.Contains(arg.Id.CharacterId),
                ApplyStatus = new CharacterApplyViewModel(
                    arg.Id,
                    arg.GetBusyStatus(),
                    arg.CharacterTypeInfo.SlotLimit,
                    arg.CharacterTypeInfo.IsHot,
                    ClaimValidator.IsAvailableForPlayer(arg, projectInfo)),
                Description = arg.Description.ToHtmlString(),
                IsPublic = IsPublic(arg),
                IsActive = arg.IsActive,
                ActiveClaimsCount = arg.ActiveClaimsCount,
                PlayerLink = arg.GetCharacterPlayerLinkViewModel(currentUserId),
                HasEditRolesAccess = HasEditRolesAccess,
                ProjectId = arg.Id.ProjectId.Value,
            };
            if (vm.IsFirstCopy)
            {
                AlreadyOutputedChars.Add(vm.CharacterId);
            }
            return vm;
        }

        private bool HasEditRolesAccess { get; } = projectInfo.HasEditRolesAccess(currentUserId);
        public ProjectInfo ProjectInfo { get; } = projectInfo;
    }
}
