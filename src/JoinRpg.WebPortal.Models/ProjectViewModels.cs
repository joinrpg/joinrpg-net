using JoinRpg.Web.ProjectCommon.Projects;

namespace JoinRpg.Web.Models;

public static class ProjectLinkViewModelBuilder
{
    public static IEnumerable<ProjectLinkViewModel> ToLinkViewModels(this IEnumerable<ProjectPersonalizedInfo> projects)
        => projects.OrderByDisplayPriority().Select(p => new ProjectLinkViewModel(p.ProjectId, p.ProjectName));
}

public class EditProjectViewModel
{
    public int ProjectId { get; set; }

    [ReadOnly(true)]
    public string OriginalName { get; set; }

    public bool Active { get; set; }

    /// <summary>Заблокирован на время восстановления данных (ADR023).</summary>
    public bool Blocked { get; set; }
}

public class UnblockProjectViewModel
{
    public int ProjectId { get; set; }

    [ReadOnly(true)]
    public string OriginalName { get; set; } = "";

    [ReadOnly(true)]
    public bool IsBlocked { get; set; }
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

