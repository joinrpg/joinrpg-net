using System.ComponentModel.DataAnnotations;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Web.Models;

public enum ProjectFieldViewType
{
    [Display(Name = "Строка", Order = 1)]
    String,
    [Display(Name = "Текст", Order = 2)]
    Text,
    [Display(Name = "Выбор", Order = 3)]
    Dropdown,
    [Display(Name = "Чекбокс", Order = 5)]
    Checkbox,
    [Display(Name = "Мультивыбор", Order = 4)]
    MultiSelect,
    [Display(Name = "Заголовок", Order = 6)]
    Header,
    [Display(Name = "Число", Order = 7)]
    Number,
    [Display(
        Name = "Логин/идентификатор",
        Description = "Латинские буквы и цифры. Если вам нужен логин во внешнюю систему, адрес игровой электронной почты, блога etc",
        Order = 8)]
    Login,
    [Display(
        Name = "Место проведения мероприятия",
        Order = 9)]
    ScheduleRoomField,
    [Display(
        Name = "Время проведения мероприятия",
        Order = 10)]
    ScheduleTimeSlotField,
    [Display(
        Name = "PIN-код",
        Description = "Автоматически сгенерированный пароль персонажа. Если вам нужен пароль для внешней системы",
        Order = 11)]
    PinCode,
    [Display(
    Name = "Uri",
    Description = "Ссылка на страницу на сайте, гугл-док, чаршит etc",
    Order = 12)]
    Uri,
}

public static class ProjectFieldViewTypeHelper
{
    /// <summary>
    /// Returns true if field supports price calculations
    /// </summary>
    public static bool SupportsPricing(this ProjectFieldViewType self)
        => ((ProjectFieldType)self).SupportsPricing();

    /// <summary>
    /// Returns true if price could be entered for field, not for it's values
    /// </summary>
    public static bool SupportsPricingOnField(this ProjectFieldViewType self)
        => ((ProjectFieldType)self).SupportsPricingOnField();

    /// <summary>
    /// Returns true if field has predefined values
    /// </summary>
    public static bool HasValuesList(this ProjectFieldViewType self)
        => ((ProjectFieldType)self).HasValuesList();
}

public enum FieldBoundToViewModel
{
    [Display(
         Name = "персонажу",
         Description = "Все, что связано с персонажем, его умения, особенности, предыстория. Выбирайте этот вариант по умолчанию.")]
    Character,

    [Display(
         Name = "заявке",
         Description = "всё, что связано с конкретным игроком: пожелания по завязкам, направлению игры и т.п. После отклонения принятой заявки они не будут доступны новому игроку на этой роли.")]
    Claim,
}

