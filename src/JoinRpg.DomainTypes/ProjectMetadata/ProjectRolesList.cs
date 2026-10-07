using System.Text.Json.Serialization;
using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.DomainTypes.ProjectMetadata;

[method: JsonConstructor]
[TypedEntityId]
public partial record ProjectRolesListIdentification(ProjectIdentification ProjectId, int ProjectRolesListId) : IProjectEntityId;

public enum ProjectRolesListVisibilityMode { None, PublicOnly, All }

public enum ShowRolesFilter { All, VacantOnly, HotOnly }

/// <summary>Режим колонки «Игрок» в сетке ролей</summary>
/// <remarks>Значения хранятся в БД числами — не перенумеровывать.</remarks>
public enum PlayerColumnMode
{
    /// <summary>Колонки нет — вместе с ней пропадает и кнопка «Заявиться»</summary>
    None = 0,
    /// <summary>Имя игрока и контакты, которые он открыл в профиле</summary>
    PublicOnly = 1,
    /// <summary>Имя игрока и все контакты (просмотр от имени мастера)</summary>
    All = 2,
    /// <summary>Только имя игрока, без контактов</summary>
    NameOnly = 3,
}

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
/// <param name="ProjectRolesListId">Идентификатор сохранённой настройки. <c>null</c> — транзиентная
/// настройка, построенная «на лету» и не сохранённая в БД (например, классическая сетка
/// <c>GameGroups/Index</c>, см. <see cref="ClassicRolesGridDefaults"/>)</param>
/// <param name="Name">Как она называется в меню</param>
/// <param name="CharacterGroupId">От какой группы она строится. Если null, то строится от верха, не используя спецгруппы</param>
/// <param name="PublicMode">Доступна ли эта сетка ролей публично, или только через вводные</param>
/// <param name="Fields">Список полей, для которых в этой сетке ролей есть колонки</param>
/// <param name="ContactsColumn">Колонка «Игрок»: не показывать, только имя, имя с публичными контактами, имя со всеми контактами</param>
/// <param name="GroupsColumn">Показывать ли специальную колонку «интересные группы» (нет, только публичные группы, все группы)</param>
/// <param name="GroupsViewMode">Как показывать группы: не показывать, секциями в таблице, деревом</param>
/// <param name="ShowRolesFilter">Какие роли показывать: все, только вакантные, только горячие</param>
[method: JsonConstructor]
public record class ProjectRolesList(
    ProjectRolesListIdentification? ProjectRolesListId,
    string Name,
    CharacterGroupIdentification? CharacterGroupId,
    bool PublicMode,
    IReadOnlyList<ProjectFieldIdentification> Fields,
    PlayerColumnMode ContactsColumn,
    ProjectRolesListVisibilityMode GroupsColumn,
    RolesGridGroupsViewMode GroupsViewMode,
    ShowRolesFilter ShowRolesFilter
    )
{
    public ProjectRolesList(
        ProjectIdentification projectId,
        string name,
        IReadOnlyList<ProjectFieldIdentification> fields,
        RolesGridGroupsViewMode groupsViewMode,
        ShowRolesFilter filter = ShowRolesFilter.All
        ) : this(
            new ProjectRolesListIdentification(projectId, -1), // id сгенерирует БД
            Name: name,
            CharacterGroupId: null,
            PublicMode: true,
            Fields: fields,
            ContactsColumn: PlayerColumnMode.NameOnly,
            GroupsColumn: ProjectRolesListVisibilityMode.None,
            GroupsViewMode: groupsViewMode,
            ShowRolesFilter: filter)
    {

    }
}

