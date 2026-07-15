namespace JoinRpg.Services.Impl.Projects;

/// <summary>
/// Параметры публичной сетки «Горячие роли», которую мы автоматически создаём для Larp-проектов
/// (при создании проекта — <see cref="CreateProjectService"/>, а для существующих —
/// <see cref="AddHotRolesListJob"/>).
/// </summary>
internal static class HotRolesListDefaults
{
    public const string Name = "Горячие роли";

    /// <summary>
    /// Собирает доменную модель сетки «Горячие роли»: публичная, от корневой группы «Все роли»,
    /// показывает только горячие роли, с колонкой «Описание персонажа» (если поле описания задано).
    /// </summary>
    public static ProjectRolesList Build(ProjectIdentification projectId, ProjectFieldIdentification? descriptionField)
        => new(
            new ProjectRolesListIdentification(projectId, -1), // id сгенерирует БД
            Name: Name,
            CharacterGroupId: null,
            PublicMode: true,
            Fields: descriptionField is { } field ? [field] : [],
            ContactsColumn: ProjectRolesListVisibilityMode.None,
            GroupsColumn: ProjectRolesListVisibilityMode.None,
            GroupsViewMode: RolesGridGroupsViewMode.None,
            ShowRolesFilter: ShowRolesFilter.HotOnly);
}
