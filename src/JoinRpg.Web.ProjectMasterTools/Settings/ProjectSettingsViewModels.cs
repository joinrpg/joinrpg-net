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

public class ProjectContactsSettingsViewModel
{
    public required ProjectIdentification ProjectId { get; set; }
    public required ProjectName ProjectName { get; init; }
    public required ProjectLifecycleStatus ProjectStatus { get; init; }

    [Display(Name = "Требовать телеграмм")]
    public required MandatoryContactsView Telegram { get; set; }
    [Display(Name = "Требовать номер телефона")]
    public required MandatoryContactsView Phone { get; set; }
    [Display(Name = "Требовать ВК")]
    public required MandatoryContactsView Vkontakte { get; set; }
    [Display(Name = "Требовать ФИО")]
    public required MandatoryContactsView Fio { get; set; }

    [Display(Name = "Требовать паспортные данные")]
    public required MandatorySenstiveContactsView Passport { get; set; }

    [Display(Name = "Требовать адрес регистрации")]
    public required MandatorySenstiveContactsView RegistrationAddress { get; set; }

}
