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
    /// Собирает транзиентную настройку классической сетки от группы <paramref name="groupId"/>
    /// (конкретный id корня сетки, обычно уже резолвленный из nullable-параметра роута).
    /// </summary>
    public static ProjectRolesList Build(
        CharacterGroupIdentification groupId,
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
}
