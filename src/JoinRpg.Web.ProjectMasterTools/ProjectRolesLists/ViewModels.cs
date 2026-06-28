using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.ProjectMasterTools.ProjectRolesLists;

public record ProjectRolesListItemViewModel(ProjectRolesList RolesList, string? CharacterGroupName);

public record ProjectRolesListViewModel(List<ProjectRolesListItemViewModel> Items, bool HasEditAccess);

public class AddProjectRolesListViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Укажите название сетки ролей")]
    [Display(Name = "Название")]
    public string Name { get; set; } = "";

    [Display(Name = "Группа персонажей", Description = "Какую групу персонажей считать корнем этой сетки ролей. Если указать = пусто, то будут отображаться все персонажи")]
    public CharacterGroupIdentification? CharacterGroupId { get; set; }

    [Display(Name = "Показывать в меню игрокам")]
    public bool PublicMode { get; set; } = false;

    [Display(Name = "Колонка контактов")]
    public ContactsColumnVisibilityModeView ContactsColumn { get; set; } = ContactsColumnVisibilityModeView.None;

    [Display(Name = "Колонка групп")]
    public ProjectRolesListVisibilityModeView GroupsColumn { get; set; } = ProjectRolesListVisibilityModeView.None;

    [Display(Name = "Показывать дочерние группы в сетке ролей",
        Description = "Группирует персонажей по дочерним группам.")]
    public bool ShowCharacterGroups { get; set; }

    [Display(Name = "Колонки полей")]
    public IReadOnlyList<ProjectFieldIdentification> Fields { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PublicMode && ContactsColumn == ContactsColumnVisibilityModeView.All)
        {
            yield return new ValidationResult(
                "В публичной сетке ролей нельзя показывать все контакты",
                [nameof(ContactsColumn)]);
        }
    }

    public ProjectRolesList ToDomain(ProjectIdentification projectId, int temporaryId = -1)
    {
        return new ProjectRolesList(
            ProjectRolesListId: new ProjectRolesListIdentification(projectId, temporaryId),
            Name: Name,
            CharacterGroupId: CharacterGroupId,
            PublicMode: PublicMode,
            Fields: Fields,
            ContactsColumn: (ProjectRolesListVisibilityMode)ContactsColumn,
            GroupsColumn: ToDomainVisibilityMode(GroupsColumn),
            ShowCharacterGroups: ShowCharacterGroups);
    }

    protected static ProjectRolesListVisibilityMode ToDomainVisibilityMode(ProjectRolesListVisibilityModeView view)
        => (ProjectRolesListVisibilityMode)view;

    protected static ProjectRolesListVisibilityModeView ToViewVisibilityMode(ProjectRolesListVisibilityMode domain)
        => (ProjectRolesListVisibilityModeView)domain;
}

public class EditProjectRolesListViewModel : AddProjectRolesListViewModel
{
    public ProjectRolesListIdentification ProjectRolesListId { get; set; } = null!;


    public static EditProjectRolesListViewModel FromDomain(ProjectRolesList domain)
    {
        return new EditProjectRolesListViewModel
        {
            ProjectRolesListId = domain.ProjectRolesListId,
            Name = domain.Name,
            CharacterGroupId = domain.CharacterGroupId,
            PublicMode = domain.PublicMode,
            ContactsColumn = (ContactsColumnVisibilityModeView)domain.ContactsColumn,
            GroupsColumn = ToViewVisibilityMode(domain.GroupsColumn),
            Fields = domain.Fields,
            ShowCharacterGroups = domain.ShowCharacterGroups,
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
            ContactsColumn: (ProjectRolesListVisibilityMode)ContactsColumn,
            GroupsColumn: ToDomainVisibilityMode(GroupsColumn),
            ShowCharacterGroups: ShowCharacterGroups);
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

public enum ContactsColumnVisibilityModeView
{
    [Display(Name = "Только имя игрока", Description = "Показывать колонку с именем игрока (ссылкой на профиль), без контактов")]
    None,

    [Display(Name = "Только публичные контакты", Description = "Показывать имя игрока и публично доступные контакты")]
    PublicOnly,

    [Display(Name = "Все контакты", Description = "Показывать имя игрока и все контакты (просмотр от имени мастера)")]
    All
}
