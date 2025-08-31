using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectCommon.Projects;
using JoinRpg.WebComponents;

namespace JoinRpg.Web.Models;

public static class ProjectLinkViewModelBuilder
{
    [Obsolete("Не надо для этих целей грузить проект, можно например ProjectHeaderDto")]
    public static IEnumerable<ProjectLinkViewModel> ToLinkViewModels(
        this IEnumerable<DataModel.Project> projects) =>
        projects.Select(p => new ProjectLinkViewModel(new(p.ProjectId), p.ProjectName));
}

public class EditProjectViewModel
{
    public int ProjectId { get; set; }

    [DisplayName("Название проекта"), Required,
 StringLength(60,
     ErrorMessage = "Название проекта должно быть длиной от 5 до 60 букв.",
     MinimumLength = 5)]
    public string ProjectName { get; set; }

    [DisplayName("Анонс проекта"), UIHint("MarkdownString")]
    public string ProjectAnnounce { get; set; }

    [Display(Name = "Заявки открыты?")]
    public bool IsAcceptingClaims { get; set; }

    [DisplayName("Правила подачи заявок"), UIHint("MarkdownString")]
    public string ClaimApplyRules { get; set; }

    [Display(Name = "Проверять, что игрок играет только одного персонажа",
        Description =
            "Если эта опция включена, при принятии заявки какого-то игрока на одну роль все другие заявки этого игрока будут автоматически отклонены.")]
    public bool StrictlyOneCharacter { get; set; }

    [Display(Name = "Автоматически принимать заявки",
        Description = "Сразу после подачи заявки joinrpg попытается автоматически принять ее, если это возможно. Удобно для конвентов.")]
    public bool AutoAcceptClaims { get; set; }

    [ReadOnly(true)]
    public string OriginalName { get; set; }
    [Display(Name = "Включить систему поселения")]
    public bool EnableAccomodation { get; set; }

    public bool Active { get; set; }

    [Display(Name = "Шаблон персонажа по умолчанию", Description = "Кнопка «заявиться» будет идти именно на этот шаблон")]
    public int? DefaultTemplateCharacterId { get; set; }
}

public class CloseProjectViewModel
{
    public int ProjectId { get; set; }

    [ReadOnly(true)]
    public string OriginalName { get; set; }

    [Display(Name = "Опубликовать сюжет всем",
        Description =
            "Cюжет игры будет раскрыт всем для всеобщего просмотра и послужит обмену опытом среди мастеров.")]
    public bool PublishPlot { get; set; }

    public bool IsMaster { get; set; }
}

public class ProjectDetailsViewModel(ProjectInfo project, ProjectDetails details, IReadOnlyCollection<ClaimWithPlayer> claims)
{
    public int ProjectId { get; } = project.ProjectId;

    [Display(Name = "Проект активен?")]
    public bool IsActive { get; } = project.IsActive;
    [Display(Name = "Дата создания")]
    public DateOnly CreatedDate { get; } = project.CreateDate;
    public IEnumerable<UserLinkViewModel> Masters { get; } = project.Masters.Select(acl => acl.ToUserLinkViewModel());

    [DisplayName("Анонс проекта")]
    public JoinHtmlString ProjectAnnounce { get; } = details.ProjectDescription.ToHtmlString();

    public bool HasMyClaims { get; } = claims.Count > 0;

    [DisplayName("Название проекта")]
    public string ProjectName { get; } = project.ProjectName;

    [Display(Name = "Заявки открыты?")]
    public bool IsAcceptingClaims { get; } = project.ProjectStatus == ProjectLifecycleStatus.ActiveClaimsOpen;
}

public class ProjectListItemViewModel(ProjectWithClaimCount p)
{
    public bool IsMaster { get; } = p.HasMasterAccess;
    public bool IsActive { get; } = p.Active;
    public int ClaimCount { get; } = p.ActiveClaimsCount;

    public bool PublishPlot { get; } = p.PublishPlot;

    public int ProjectId { get; set; } = p.ProjectId;

    [DisplayName("Название проекта"), Required]
    public string ProjectName { get; set; } = p.ProjectName;

    [Display(Name = "Заявки открыты?")]
    public bool IsAcceptingClaims { get; } = p.IsAcceptingClaims;

    public bool HasMyClaims { get; } = p.HasMyClaims;

    public static IOrderedEnumerable<T> OrderByDisplayPriority<T>(
        IEnumerable<T> collectionToSort,
        Func<T, ProjectListItemViewModel?> getProjectFunc)
    {
        return collectionToSort
            .OrderByDescending(p => getProjectFunc(p)?.IsActive)
            .ThenByDescending(p => getProjectFunc(p)?.IsMaster)
            .ThenByDescending(p => getProjectFunc(p)?.ClaimCount);
    }
}
