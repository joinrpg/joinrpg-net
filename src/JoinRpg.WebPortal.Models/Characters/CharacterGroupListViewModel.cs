using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Helpers;
using JoinRpg.Markdown;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Web.Models.Characters;

/// <summary>
/// Дерево ролей для публичного JSON (<c>GameGroupsJson</c>). Работает поверх доменных типов
/// (ADR013): группы приходят из <see cref="ProjectInfo"/>, персонажи — агрегатами.
/// </summary>
public static class CharacterGroupListViewModel
{
    /// <param name="groupFullInfos">
    /// Описания групп: в <see cref="ProjectInfo"/> их нет, поэтому они грузятся отдельно.
    /// Группа без записи тут просто останется без описания.
    /// </param>
    public static IEnumerable<CharacterGroupListItemViewModel> GetGroups(
        CharacterGroupInfo root,
        IReadOnlyCollection<CharacterInfo> characters,
        IReadOnlyDictionary<CharacterGroupIdentification, CharacterGroupFullInfo> groupFullInfos,
        UserIdentification? currentUserId,
        ProjectInfo projectInfo)
        => new CharacterGroupHierarchyBuilder(root, characters, groupFullInfos, currentUserId, projectInfo)
            .Generate()
            .WhereNotNull();

    /// <summary>
    /// Горячие роли поддерева — плоским списком.
    /// </summary>
    /// <remarks>
    /// Дерево тут не строится: описания групп, пути и вложенность для этого ответа не нужны,
    /// а нужна только видимость группы — она и считается обходом <see cref="ProjectInfo"/>.
    /// </remarks>
    public static IEnumerable<CharacterViewModel> GetHotCharacters(
        CharacterGroupInfo root,
        IReadOnlyCollection<CharacterInfo> characters,
        UserIdentification? currentUserId,
        ProjectInfo projectInfo)
    {
        var visibleGroups = CollectVisibleGroups(root, currentUserId, projectInfo);

        return characters
            .Where(character => character.IsActive && IsPublic(character) && character.CharacterTypeInfo.IsHot)
            .Where(character => character.DirectGroupIds.Any(visibleGroups.Contains))
            .Select(character => BuildCharacter(character, isFirstCopy: true, currentUserId, projectInfo));
    }

    /// <summary>Группы поддерева, доступные этому пользователю.</summary>
    private static HashSet<CharacterGroupIdentification> CollectVisibleGroups(
        CharacterGroupInfo root,
        UserIdentification? currentUserId,
        ProjectInfo projectInfo)
        => [.. projectInfo.GetChildGroupsIncludingThis(root.Id)
            .Where(group => group.IsActive && IsVisible(group, currentUserId, projectInfo))
            .Select(group => group.Id)];

    /// <summary>Зеркало <c>WorldObjectExtensions.IsVisible</c> для группы поверх метаданных.</summary>
    private static bool IsVisible(CharacterGroupInfo group, UserIdentification? currentUserId, ProjectInfo projectInfo)
        => group.IsPublic || projectInfo.PublishPlot || projectInfo.HasMasterAccess(currentUserId);

    /// <summary>Зеркало <c>WorldObjectExtensions.IsVisible</c> для персонажа поверх агрегата.</summary>
    private static bool IsVisible(CharacterInfo character, UserIdentification? currentUserId)
        => IsPublic(character)
            || character.ProjectInfo.PublishPlot
            || character.ProjectInfo.HasMasterAccess(currentUserId);

    /// <summary>
    /// Публичность в смысле колонки <c>Character.IsPublic</c>.
    /// </summary>
    /// <remarks>
    /// Именно так, а не через <see cref="CharacterInfo.IsPublic"/>: тот означает
    /// <c>CharacterVisibility.Public</c>, а у публичного персонажа со скрытым игроком видимость —
    /// <c>PlayerHidden</c>. Он публичный, просто игрок не показывается; иначе такие персонажи
    /// исчезли бы из публичного JSON целиком.
    /// </remarks>
    private static bool IsPublic(CharacterInfo character)
        => character.CharacterTypeInfo.CharacterVisibility != CharacterVisibility.Private;

    private static CharacterViewModel BuildCharacter(
        CharacterInfo character,
        bool isFirstCopy,
        UserIdentification? currentUserId,
        ProjectInfo projectInfo)
        => new()
        {
            CharacterId = character.Id.CharacterId,
            CharacterName = character.CharacterName,
            IsFirstCopy = isFirstCopy,
            ApplyStatus = new CharacterApplyViewModel(
                character.Id,
                character.GetBusyStatus(),
                character.CharacterTypeInfo.SlotLimit,
                character.CharacterTypeInfo.IsHot,
                ClaimValidator.IsAvailableForPlayer(character, projectInfo)),
            Description = character.Description.ToHtmlString(),
            IsPublic = IsPublic(character),
            IsActive = character.IsActive,
            ActiveClaimsCount = character.ActiveClaimsCount,
            PlayerLink = character.GetCharacterPlayerLinkViewModel(currentUserId),
            HasEditRolesAccess = projectInfo.HasEditRolesAccess(currentUserId),
            ProjectId = character.Id.ProjectId.Value,
        };

    private class CharacterGroupHierarchyBuilder(
        CharacterGroupInfo root,
        IReadOnlyCollection<CharacterInfo> characters,
        IReadOnlyDictionary<CharacterGroupIdentification, CharacterGroupFullInfo> groupFullInfos,
        UserIdentification? currentUserId,
        ProjectInfo projectInfo)
    {
        private readonly ILookup<CharacterGroupIdentification, CharacterInfo> charactersByGroup = characters
            .SelectMany(character => character.DirectGroupIds.Select(groupId => (groupId, character)))
            .ToLookup(x => x.groupId, x => x.character);

        private HashSet<CharacterIdentification> AlreadyOutputedChars { get; } = [];

        private IList<CharacterGroupListItemViewModel> Results { get; } = [];

        public IList<CharacterGroupListItemViewModel> Generate()
        {
            _ = GenerateFrom(root, 0, []);
            return Results;
        }

        private CharacterGroupListItemViewModel? GenerateFrom(CharacterGroupInfo group, int deepLevel, List<CharacterGroupInfo> pathToTop)
        {
            if (!IsVisible(group, currentUserId, projectInfo))
            {
                return null;
            }
            var prevCopy = Results.FirstOrDefault(cg => cg.FirstCopy && cg.CharacterGroupId == group.Id.CharacterGroupId);

            var vm = new CharacterGroupListItemViewModel
            {
                CharacterGroupId = group.Id.CharacterGroupId,
                DeepLevel = deepLevel,
                Name = group.Name,
                FirstCopy = prevCopy == null,
                ActiveCharacters =
                prevCopy?.ActiveCharacters ??
                GenerateCharacters(group)
                  .ToList(),
                Description = (groupFullInfos.GetValueOrDefault(group.Id)?.Description).ToHtmlString(),
                Path = pathToTop.Select(g => Results.First(item => item.CharacterGroupId == g.Id.CharacterGroupId)),
                IsPublic = group.IsPublic,
                GroupType = group.GroupType,
                ProjectId = group.Id.ProjectId.Value,
                RootGroupId = root.Id.CharacterGroupId,
            };

            if (root.Id == group.Id)
            {
                vm.First = true;
                vm.Last = true;
            }

            if (vm.IsSpecial)
            {
                var variant = projectInfo.GetVariantByGroupIdOrDefault(group.Id);

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

            // DirectChildGroupIds уже упорядочены по ChildGroupsOrdering — это делает
            // CharacterGroupDictionaryBuilder при сборке метаданных проекта.
            var childGroups = group.DirectChildGroupIds
                .Select(id => projectInfo.GetGroupById(id.CharacterGroupId))
                .OrderBy(g => g.IsSpecial)
                .Where(g => g.IsActive && IsVisible(g, currentUserId, projectInfo))
                .ToList();
            var pathForChildren = pathToTop.Union([group]).ToList();

            vm.ChildGroups = childGroups
                .Select(childGroup => GenerateFrom(childGroup, deepLevel + 1, pathForChildren))
                .WhereNotNull()
                .ToList();

            _ = vm.ChildGroups
                .Where(x => !x.IsSpecial)
                .MarkFirstAndLast();

            return vm;
        }

        private IEnumerable<CharacterViewModel> GenerateCharacters(CharacterGroupInfo group)
        {
            // Порядок тот же, что давал GetOrderedCharacters: сохранённый порядок группы поверх
            // идентификаторов персонажей.
            var characters = charactersByGroup[group.Id]
                .OrderByStoredOrder(character => character.Id.CharacterId, group.ChildCharactersOrdering)
                .Where(character => character.IsActive && IsVisible(character, currentUserId));

            return characters.Select(GenerateCharacter);
        }

        private CharacterViewModel GenerateCharacter(CharacterInfo character)
        {
            var vm = BuildCharacter(character, AlreadyOutputedChars.Add(character.Id), currentUserId, projectInfo);
            return vm;
        }
    }
}
