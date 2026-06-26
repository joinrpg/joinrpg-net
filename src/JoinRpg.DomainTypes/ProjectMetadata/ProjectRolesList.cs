using System.Text.Json.Serialization;
using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.DomainTypes.ProjectMetadata;

[method: JsonConstructor]
[TypedEntityId]
public partial record ProjectRolesListIdentification(ProjectIdentification ProjectId, int ProjectRolesListId) : IProjectEntityId;

public enum ProjectRolesListVisibilityMode { None, PublicOnly, All }

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
public record class ProjectRolesList(
    ProjectRolesListIdentification ProjectRolesListId,
    string Name,
    CharacterGroupIdentification? CharacterGroupId,
    bool PublicMode,
    IReadOnlyList<ProjectFieldIdentification> Fields,
    ProjectRolesListVisibilityMode ContactsColumn,
    ProjectRolesListVisibilityMode GroupsColumn
    )
{
    //TODO проверять на валидность, например, что в публичной сетке ролей не может быть колонки со всеми контактами
}

