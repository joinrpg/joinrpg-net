using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Web.Models.Characters;

/// <summary>
/// Горячие роли поддерева — плоским списком, для публичного JSON (<c>GameGroupsJson/HotJson</c>).
/// </summary>
/// <remarks>
/// Отдельно от <see cref="CharacterGroupListViewModel"/> именно потому, что дерево тут не нужно:
/// описания групп, пути и вложенность в этот ответ не попадают, нужна только видимость группы.
/// </remarks>
public static class HotCharactersViewModel
{
    public static IEnumerable<CharacterViewModel> GetHotCharacters(
        CharacterGroupInfo root,
        IReadOnlyCollection<CharacterInfo> characters,
        UserIdentification? currentUserId,
        ProjectInfo projectInfo)
    {
        var visibleGroups = CollectVisibleGroups(root, currentUserId, projectInfo);

        return characters
            .Where(character => character.IsActive
                && character.CharacterTypeInfo.IsNamePublic
                && character.CharacterTypeInfo.IsHot)
            .Where(character => character.DirectGroupIds.Any(visibleGroups.Contains))
            .Select(character => CharacterViewModelBuilder.Build(character, isFirstCopy: true, currentUserId, projectInfo));
    }

    /// <summary>Группы поддерева, доступные этому пользователю.</summary>
    private static HashSet<CharacterGroupIdentification> CollectVisibleGroups(
        CharacterGroupInfo root,
        UserIdentification? currentUserId,
        ProjectInfo projectInfo)
        => [.. projectInfo.GetChildGroupsIncludingThis(root.Id)
            .Where(group => group.IsActive && CharacterViewModelBuilder.IsVisible(group, currentUserId, projectInfo))
            .Select(group => group.Id)];
}
