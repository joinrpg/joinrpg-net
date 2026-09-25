using JoinRpg.DomainTypes.Characters;
using JoinRpg.Helpers;
using JoinRpg.Markdown;

namespace JoinRpg.Web.Models.Characters;

/// <summary>
/// Дерево ролей для публичного JSON (<c>GameGroupsJson/IndexJson</c>). Работает поверх доменных
/// типов (ADR013): группы приходят из <see cref="ProjectInfo"/>, персонажи — агрегатами.
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
            if (!CharacterViewModelBuilder.IsVisible(group, currentUserId, projectInfo))
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

            // Порядок дочерних групп уже учтён в метаданных: CharacterGroupDictionaryBuilder
            // сортирует DirectChildGroupIds по ChildGroupsOrdering. Спецгруппы показываем
            // последними, как это делала старая сетка.
            // Видимость тут не проверяем: невидимую группу отсечёт сам GenerateFrom, вернув null.
            var childGroups = projectInfo.GetDirectChildGroups(group.Id)
                .Where(g => g.IsActive)
                .OrderBy(g => g.IsSpecial)
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
                .Where(character => character.IsActive && CharacterViewModelBuilder.IsVisible(character, currentUserId));

            return characters.Select(GenerateCharacter);
        }

        private CharacterViewModel GenerateCharacter(CharacterInfo character)
            => CharacterViewModelBuilder.Build(
                character,
                isFirstCopy: AlreadyOutputedChars.Add(character.Id),
                currentUserId,
                projectInfo);
    }
}
