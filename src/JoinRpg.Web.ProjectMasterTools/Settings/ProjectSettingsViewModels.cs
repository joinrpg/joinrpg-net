using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.ProjectMasterTools.Settings;

public class ProjectMainSettingsViewModel
{
    public required ProjectIdentification ProjectId { get; set; }
    public required ProjectLifecycleStatus ProjectStatus { get; init; }

    [Display(Name = "Название проекта")]
    [Required(ErrorMessage = "Укажите название проекта")]
    [StringLength(60, MinimumLength = 5, ErrorMessage = "Название проекта должно быть длиной от 5 до 60 букв.")]
    public required string ProjectName { get; set; }

    [Display(Name = "Анонс проекта",
        Description = "Не надо дублировать в этом поле данные из КогдаИгры — даты, МГ, регион и ссылки на сайт и соцсети.")]
    public required string ProjectAnnounce { get; set; }

    [Display(Name = "Включить систему поселения")]
    public required bool EnableAccommodation { get; set; }
}

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

    [Display(Name = "Требовать дату рождения")]
    public required MandatorySenstiveContactsView BirthDate { get; set; }

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
    Description = "Сразу после подачи заявки на шаблон joinrpg попытается автоматически её принять, если это возможно. Удобно для конвентов.")]
    public required bool AutoAcceptClaims { get; set; }

    [Display(Name = "Проверять, что игрок играет только одного персонажа",
    Description =
        "Если эта опция включена, при принятии заявки какого-то игрока на одну роль все другие заявки этого игрока будут автоматически отклонены.")]
    public required bool StrictlyOneCharacter { get; set; }

    [Display(Name = "Правила подачи заявок",
        Description = "Показываются игроку при подаче заявки")]
    public required string ClaimApplyRules { get; set; }
}

public class ProjectTimeZoneSettingsViewModel
{
    public required ProjectIdentification ProjectId { get; set; }
    public required ProjectLifecycleStatus ProjectStatus { get; init; }

    [Display(Name = "Часовой пояс проекта",
        Description = "В этом часовом поясе показываются время и даты игры (расписание и т. п.)")]
    [Required(ErrorMessage = "Выберите часовой пояс")]
    public required string? TimeZoneId { get; set; }
}
