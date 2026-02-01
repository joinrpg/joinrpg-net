using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.ProjectCommon.Projects;

namespace JoinRpg.Web.Models;

public static class ProjectLinkViewModelBuilder
{
    public static IEnumerable<ProjectLinkViewModel> ToLinkViewModels(this IEnumerable<ProjectShortInfo> projects)
        => projects.OrderByDisplayPriority().Select(p => new ProjectLinkViewModel(p.ProjectId, p.ProjectName));
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

    [DisplayName("Правила подачи заявок"), UIHint("MarkdownString")]
    public string ClaimApplyRules { get; set; }

    [ReadOnly(true)]
    public string OriginalName { get; set; }
    [Display(Name = "Включить систему поселения")]
    public bool EnableAccomodation { get; set; }

    public bool Active { get; set; }
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

