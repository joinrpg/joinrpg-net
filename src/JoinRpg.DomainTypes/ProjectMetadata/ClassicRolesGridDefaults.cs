namespace JoinRpg.DomainTypes.ProjectMetadata;

/// <summary>
/// Параметры «классической» сетки ролей (страница <c>GameGroups/Index</c>, роут
/// <c>~/{projectId}/roles/{characterGroupId?}</c>), которую мы строим на лету и НЕ сохраняем в БД.
/// Настройка транзиентная — у неё нет id (<see cref="ProjectRolesList.ProjectRolesListId"/> == null).
/// Режим дерева повторяет старую некастомизируемую сетку: иерархия групп с отступами, описание
/// персонажа под именем, доступ публичный (приватное отсекается по мастер-доступу в сервисе).
/// </summary>
public static class ClassicRolesGridDefaults
{
    /// <summary>
    /// Собирает транзиентную настройку классической сетки от группы <paramref name="groupId"/>.
    /// Если <paramref name="groupId"/> == null (в роуте не был указан характерGroupId), сетка
    /// строится от корня проекта, не используя спецгруппы — см. doc-комментарий
    /// <see cref="ProjectRolesList.CharacterGroupId"/>.
    /// </summary>
    public static ProjectRolesList Build(
        CharacterGroupIdentification? groupId,
        string groupName,
        ProjectFieldIdentification? descriptionField)
        => new(
            ProjectRolesListId: null, // транзиентная сетка, в БД не сохраняется
            Name: groupName,
            CharacterGroupId: groupId,
            PublicMode: true, // как старая [AllowAnonymous] Index; приватное отсекает canViewPrivate
            Fields: descriptionField is { } field ? [field] : [],
            ContactsColumn: ProjectRolesListVisibilityMode.None,
            GroupsColumn: ProjectRolesListVisibilityMode.None,
            GroupsViewMode: RolesGridGroupsViewMode.Tree,
            ShowRolesFilter: ShowRolesFilter.All);

    /// <summary>
    /// Собирает транзиентную настройку сетки горячих ролей (страница <c>GameGroups/Hot</c>, роут
    /// <c>~/{projectId}/roles/hot</c>) — плоский список без групп, только горячие роли.
    /// </summary>
    public static ProjectRolesList BuildHot(
        CharacterGroupIdentification? groupId,
        ProjectFieldIdentification? descriptionField)
        => new(
            ProjectRolesListId: null, // транзиентная сетка, в БД не сохраняется
            Name: "Горячие роли",
            CharacterGroupId: groupId,
            PublicMode: true,
            Fields: descriptionField is { } field ? [field] : [],
            ContactsColumn: ProjectRolesListVisibilityMode.None,
            GroupsColumn: ProjectRolesListVisibilityMode.None,
            GroupsViewMode: RolesGridGroupsViewMode.None,
            ShowRolesFilter: ShowRolesFilter.HotOnly);
}
