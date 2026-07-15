using System.Text.Json.Serialization;
using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.DomainTypes.ProjectMetadata;

[method: JsonConstructor]
[TypedEntityId]
public partial record ProjectRolesListIdentification(ProjectIdentification ProjectId, int ProjectRolesListId) : IProjectEntityId;

public enum ProjectRolesListVisibilityMode { None, PublicOnly, All }

public enum ShowRolesFilter { All, VacantOnly, HotOnly }

/// <summary>Режим показа групп в сетке ролей</summary>
public enum RolesGridGroupsViewMode
{
    /// <summary>Плоская таблица без групп</summary>
    None = 0,
    /// <summary>Таблица с секциями-группами</summary>
    Sections = 1,
    /// <summary>Иерархическое дерево</summary>
    Tree = 2,
}

/// <summary>
/// Это класс соответствует настройке страницы «сетки ролей»
/// </summary>
/// <param name="ProjectRolesListId"></param>
/// <param name="Name">Как она называется в меню</param>
/// <param name="CharacterGroupId">От какой группы она строится. Если null, то строится от верха, не используя спецгруппы</param>
/// <param name="PublicMode">Доступна ли эта сетка ролей публично, или только через вводные</param>
/// <param name="Fields">Список полей, для которых в этой сетке ролей есть колонки</param>
/// <param name="ContactsColumn">Показывать ли специальную колонку с контактами (нет, только публичные контакты, все контакты)</param>
/// <param name="GroupsColumn">Показывать ли специальную колонку «интересные группы» (нет, только публичные группы, все группы)</param>
/// <param name="GroupsViewMode">Как показывать группы: не показывать, секциями в таблице, деревом</param>
/// <param name="ShowRolesFilter">Какие роли показывать: все, только вакантные, только горячие</param>
public record class ProjectRolesList(
    ProjectRolesListIdentification ProjectRolesListId,
    string Name,
    CharacterGroupIdentification? CharacterGroupId,
    bool PublicMode,
    IReadOnlyList<ProjectFieldIdentification> Fields,
    ProjectRolesListVisibilityMode ContactsColumn,
    ProjectRolesListVisibilityMode GroupsColumn,
    RolesGridGroupsViewMode GroupsViewMode,
    ShowRolesFilter ShowRolesFilter
    )
{
    //TODO проверять на валидность, например, что в публичной сетке ролей не может быть колонки со всеми контактами
}

