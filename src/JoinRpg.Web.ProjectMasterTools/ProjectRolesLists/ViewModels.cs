using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.ProjectMasterTools.ProjectRolesLists;

public record ProjectRolesListItemViewModel(ProjectRolesList RolesList, string? CharacterGroupName);

public record ProjectRolesListViewModel(List<ProjectRolesListItemViewModel> Items, bool HasEditAccess);

public class AddProjectRolesListViewModel
{
    [Required(ErrorMessage = "Укажите название сетки ролей")]
    [Display(Name = "Название")]
    public string Name { get; set; } = "";

    [Display(Name = "Группа персонажей (опционально)")]
    public CharacterGroupIdentification? CharacterGroupId { get; set; }

    [Display(Name = "Публичный режим")]
    public bool PublicMode { get; set; } = false;

    [Display(Name = "Колонка контактов")]
    public ProjectRolesListVisibilityMode ContactsColumn { get; set; } = ProjectRolesListVisibilityMode.None;

    [Display(Name = "Колонка групп")]
    public ProjectRolesListVisibilityMode GroupsColumn { get; set; } = ProjectRolesListVisibilityMode.None;

    // Поля (колонки) пока не настраиваем
    public IReadOnlyList<ProjectFieldIdentification> Fields => [];

    public DomainTypes.ProjectMetadata.ProjectRolesList ToDomain(ProjectIdentification projectId, int temporaryId = -1)
    {
        return new DomainTypes.ProjectMetadata.ProjectRolesList(
            ProjectRolesListId: new ProjectRolesListIdentification(projectId, temporaryId),
            Name: Name,
            CharacterGroupId: CharacterGroupId,
            PublicMode: PublicMode,
            Fields: Fields,
            ContactsColumn: ContactsColumn,
            GroupsColumn: GroupsColumn);
    }
}
