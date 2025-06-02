using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.ProjectMasterTools.Settings;
public class ProjectPublishSettingsViewModel
{
    public required ProjectIdentification ProjectId { get; set; }
    public required ProjectName ProjectName { get; init; }
    public required ProjectLifecycleStatus ProjectStatus { get; init; }

    [Display(Name = "Опубликовать сюжет всем",
    Description =
        "Cюжет игры будет раскрыт всем для всеобщего просмотра и послужит обмену опытом среди мастеров.")]
    public required bool PublishEnabled { get; set; }

    [Display(Name = "Кому разрешить клонировать проект")]
    public required ProjectCloneSettingsView CloneSettings { get; set; }

}
