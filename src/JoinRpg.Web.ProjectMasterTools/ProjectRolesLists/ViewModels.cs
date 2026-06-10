using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.ProjectMasterTools.ProjectRolesLists;

public record ProjectRolesListItemViewModel(ProjectRolesList RolesList, string? CharacterGroupName);

public record ProjectRolesListViewModel(List<ProjectRolesListItemViewModel> Items, bool HasEditAccess);

public class AddProjectRolesListViewModel
{
    [Required(ErrorMessage = "Укажите название сетки ролей")]
    [Display(Name = "Название")]
    public string Name { get; set; } = "";

    [Display(Name = "Группа персонажей", Description = "Какую групу персонажей считать корнем этой сетки ролей. Если указать = пусто, то будут отображаться все персонажи")]
    public CharacterGroupIdentification? CharacterGroupId { get; set; }

    [Display(Name = "Показывать в меню игрокам")]
    public bool PublicMode { get; set; } = false;

    [Display(Name = "Колонка контактов")]
    public ProjectRolesListVisibilityModeView ContactsColumn { get; set; } = ProjectRolesListVisibilityModeView.None;

    [Display(Name = "Колонка групп")]
    public ProjectRolesListVisibilityModeView GroupsColumn { get; set; } = ProjectRolesListVisibilityModeView.None;

    // Поля (колонки) пока не настраиваем
    public IReadOnlyList<ProjectFieldIdentification> Fields => [];

    public ProjectRolesList ToDomain(ProjectIdentification projectId, int temporaryId = -1)
    {
        return new ProjectRolesList(
            ProjectRolesListId: new ProjectRolesListIdentification(projectId, temporaryId),
            Name: Name,
            CharacterGroupId: CharacterGroupId,
            PublicMode: PublicMode,
            Fields: Fields,
            ContactsColumn: ToDomainVisibilityMode(ContactsColumn),
            GroupsColumn: ToDomainVisibilityMode(GroupsColumn));
    }

    protected static ProjectRolesListVisibilityMode ToDomainVisibilityMode(ProjectRolesListVisibilityModeView view)
        => (ProjectRolesListVisibilityMode)view;

    protected static ProjectRolesListVisibilityModeView ToViewVisibilityMode(ProjectRolesListVisibilityMode domain)
        => (ProjectRolesListVisibilityModeView)domain;
}

public class EditProjectRolesListViewModel : AddProjectRolesListViewModel
{
    public ProjectRolesListIdentification ProjectRolesListId { get; set; } = null!;

    public new IReadOnlyList<ProjectFieldIdentification> Fields { get; set; } = [];

    public static EditProjectRolesListViewModel FromDomain(ProjectRolesList domain)
    {
        return new EditProjectRolesListViewModel
        {
            ProjectRolesListId = domain.ProjectRolesListId,
            Name = domain.Name,
            CharacterGroupId = domain.CharacterGroupId,
            PublicMode = domain.PublicMode,
            ContactsColumn = ToViewVisibilityMode(domain.ContactsColumn),
            GroupsColumn = ToViewVisibilityMode(domain.GroupsColumn),
            Fields = domain.Fields,
        };
    }

    public ProjectRolesList ToDomain()
    {
        return new ProjectRolesList(
            ProjectRolesListId: ProjectRolesListId,
            Name: Name,
            CharacterGroupId: CharacterGroupId,
            PublicMode: PublicMode,
            Fields: Fields,
            ContactsColumn: ToDomainVisibilityMode(ContactsColumn),
            GroupsColumn: ToDomainVisibilityMode(GroupsColumn));
    }
}

public enum ProjectRolesListVisibilityModeView
{
    [Display(Name = "Не показывать", Description = "Колонка не будет отображаться")]
    None,

    [Display(Name = "Только публичные", Description = "Показывать только публично доступные")]
    PublicOnly,

    [Display(Name = "Все", Description = "Показывать все (просмотр от имени мастера)")]
    All
}
