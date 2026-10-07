using System.ComponentModel.DataAnnotations;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Web.ProjectMasterTools.ProjectRolesLists;

public record ProjectRolesListItemViewModel(ProjectRolesList RolesList, CharacterGroupLinkSlimViewModel? CharacterGroup, bool IsDefault);

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

    [Display(Name = "Колонка «Игрок»")]
    public PlayerColumnModeView ContactsColumn { get; set; } = PlayerColumnModeView.NameOnly;

    [Display(Name = "Колонка групп")]
    public ProjectRolesListVisibilityModeView GroupsColumn { get; set; } = ProjectRolesListVisibilityModeView.None;

    [Display(Name = "Показ групп")]
    public RolesGridGroupsViewModeView GroupsViewMode { get; set; } = RolesGridGroupsViewModeView.None;

    [Display(Name = "Показывать роли")]
    public ShowRolesFilterView ShowRolesFilter { get; set; } = ShowRolesFilterView.All;

    [Display(Name = "Колонки полей")]
    public IReadOnlyList<ProjectFieldIdentification> Fields { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PublicMode && ContactsColumn == PlayerColumnModeView.All)
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
            ContactsColumn: ToDomainPlayerColumn(ContactsColumn),
            GroupsColumn: ToDomainVisibilityMode(GroupsColumn),
            GroupsViewMode: (RolesGridGroupsViewMode)GroupsViewMode,
            ShowRolesFilter: (ShowRolesFilter)ShowRolesFilter);
    }

    // Порядок вариантов в форме не совпадает с числами в БД (NameOnly добавлен последним),
    // поэтому соответствие явное, а не приведение через int.
    protected static PlayerColumnMode ToDomainPlayerColumn(PlayerColumnModeView view) => view switch
    {
        PlayerColumnModeView.None => PlayerColumnMode.None,
        PlayerColumnModeView.NameOnly => PlayerColumnMode.NameOnly,
        PlayerColumnModeView.PublicOnly => PlayerColumnMode.PublicOnly,
        PlayerColumnModeView.All => PlayerColumnMode.All,
        _ => throw new ArgumentOutOfRangeException(nameof(view), view, null),
    };

    protected static PlayerColumnModeView ToViewPlayerColumn(PlayerColumnMode domain) => domain switch
    {
        PlayerColumnMode.None => PlayerColumnModeView.None,
        PlayerColumnMode.NameOnly => PlayerColumnModeView.NameOnly,
        PlayerColumnMode.PublicOnly => PlayerColumnModeView.PublicOnly,
        PlayerColumnMode.All => PlayerColumnModeView.All,
        _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, null),
    };

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
            ProjectRolesListId = domain.ProjectRolesListId!,
            Name = domain.Name,
            CharacterGroupId = domain.CharacterGroupId,
            PublicMode = domain.PublicMode,
            ContactsColumn = ToViewPlayerColumn(domain.ContactsColumn),
            GroupsColumn = ToViewVisibilityMode(domain.GroupsColumn),
            Fields = domain.Fields,
            GroupsViewMode = (RolesGridGroupsViewModeView)domain.GroupsViewMode,
            ShowRolesFilter = (ShowRolesFilterView)domain.ShowRolesFilter,
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
            ContactsColumn: ToDomainPlayerColumn(ContactsColumn),
            GroupsColumn: ToDomainVisibilityMode(GroupsColumn),
            GroupsViewMode: (RolesGridGroupsViewMode)GroupsViewMode,
            ShowRolesFilter: (ShowRolesFilter)ShowRolesFilter);
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

public enum PlayerColumnModeView
{
    [Display(Name = "Не показывать", Description = "Колонки «Игрок» не будет. Кнопка «Заявиться» тоже не будет показываться — подходит, например, для сетки мероприятий со своим полем «Ведущий»")]
    None,

    [Display(Name = "Только имя игрока", Description = "Показывать колонку с именем игрока (ссылкой на профиль), без контактов")]
    NameOnly,

    [Display(Name = "Только публичные контакты", Description = "Показывать имя игрока и публично доступные контакты")]
    PublicOnly,

    [Display(Name = "Все контакты", Description = "Показывать имя игрока и все контакты (просмотр от имени мастера)")]
    All
}

public enum RolesGridGroupsViewModeView
{
    [Display(Name = "Не показывать группы", Description = "Плоская таблица без групп")]
    None,

    [Display(Name = "Секции в таблице", Description = "Персонажи группируются по дочерним группам")]
    Sections,

    [Display(Name = "Иерархическое дерево", Description = "Полное дерево групп с отступами, как классическая сетка ролей")]
    Tree,
}

public enum ShowRolesFilterView
{
    [Display(Name = "Все роли", Description = "Показывать все роли")]
    All,

    [Display(Name = "Только вакантные", Description = "Роли без одобренного игрока (включая роли в обсуждении)")]
    VacantOnly,

    [Display(Name = "Только горячие", Description = "Роли, помеченные мастером как горячие")]
    HotOnly,
}
