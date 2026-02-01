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

public class ProjectClaimSettingsViewModel
{
    public required ProjectIdentification ProjectId { get; set; }
    public required ProjectName ProjectName { get; init; }
    public required ProjectLifecycleStatus ProjectStatus { get; init; }

    [Display(Name = "Принимать заявки",
        Description = "Игроки смогут отсылать заявки на проект.")]
    public required bool IsAcceptingClaims { get; set; }

    [Display(Name = "Публичный проект",
        Description = "Показывать этот проект на главной и разрешить рекламировать этот проект другим образом.")]
    public required bool IsPublicProject { get; set; }
    [Display(Name = "Шаблон персонажа по умолчанию", Description = "Кнопка «заявиться» будет идти именно на этот шаблон")]
    public required CharacterIdentification? DefaultTemplateCharacterId { get; set; }

    [Display(Name = "Автоматически принимать заявки",
    Description = "Сразу после подачи заявки joinrpg попытается автоматически принять ее, если это возможно. Удобно для конвентов.")]
    public required bool AutoAcceptClaims { get; set; }

    [Display(Name = "Проверять, что игрок играет только одного персонажа",
    Description =
        "Если эта опция включена, при принятии заявки какого-то игрока на одну роль все другие заявки этого игрока будут автоматически отклонены.")]
    public required bool StrictlyOneCharacter { get; set; }
}
