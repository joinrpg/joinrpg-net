using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Web.Models.Characters;

/// <summary>
/// Горячие роли — плоским списком, для публичного JSON (<c>GameGroupsJson/HotJson</c>).
/// </summary>
/// <remarks>
/// Отдельно от <see cref="CharacterGroupListViewModel"/> именно потому, что дерево тут не нужно:
/// описания групп, пути и вложенность в этот ответ не попадают. Видимость групп тоже не считается:
/// публичный персонаж в непубличной группе — нонсенс, и такой конфигурации не должно
/// существовать (issue #4878). Поддерево задаёт вызывающий — тем, каких персонажей он загрузил.
/// </remarks>
public static class HotCharactersViewModel
{
    public static IEnumerable<CharacterViewModel> GetHotCharacters(
        IReadOnlyCollection<CharacterInfo> characters,
        UserIdentification? currentUserId)
        => characters
            .Where(character => character.IsActive
                && character.CharacterTypeInfo.IsNamePublic
                && character.CharacterTypeInfo.IsHot)
            .Select(character => CharacterViewModelBuilder.Build(character, isFirstCopy: true, currentUserId));
}
