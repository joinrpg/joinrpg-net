using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.ProjectMasterTools.Settings;

public enum ProjectCloneSettingsView
{
    [Display(Name = "Никому", Description = "Копировать проект запрещено")]
    CloneDisabled = 0,
    [Display(Name = "Мастерам", Description = "Любой мастер проекта может его скопировать")]
    CanBeClonedByMaster = 1,
    [Display(Name = "Всем", Description = "Любой пользователь joinrpg может его скопировать (в том числе увидеть скрытых персонажей и сюжеты)")]
    CanBeClonedByAnyone = 2,
}
