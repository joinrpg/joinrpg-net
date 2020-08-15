using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{

    public enum ProjectFieldViewType
    {
        [Display(Name = "Строка", Order = 1), UsedImplicitly]
        String,
        [Display(Name = "Текст", Order = 2), UsedImplicitly]
        Text,
        [Display(Name = "Выбор", Order = 3), UsedImplicitly]
        Dropdown,
        [Display(Name = "Чекбокс", Order = 5), UsedImplicitly]
        Checkbox,
        [Display(Name = "Мультивыбор", Order = 4), UsedImplicitly]
        MultiSelect,
        [Display(Name = "Заголовок", Order = 6), UsedImplicitly]
        Header,
        [Display(Name = "Число", Order = 7), UsedImplicitly]
        Number,
        [Display(
            Name = "Логин/идентификатор",
            Description = "Латинские буквы и цифры. Если вам нужен логин во внешнюю систему, адрес игровой электронной почты, блога etc",
            Order = 8)]
        [UsedImplicitly]
        Login,
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
        [UsedImplicitly]
        Character,

        [Display(
             Name = "заявке",
             Description = "всё, что связано с конкретным игроком: пожелания по завязкам, направлению игры и т.п. После отклонения принятой заявки они не будут доступны новому игроку на этой роли.")]
        [UsedImplicitly]
        Claim,
    }

    public enum MandatoryStatusViewType
    {
        [Display(Name = "Опциональное"), UsedImplicitly]
        Optional,

        [Display(Name = "Рекомендованное",
            Description = "При незаполненном поле будет выдаваться предупреждение, а заявка или персонаж — помечаться как проблемные"),
            UsedImplicitly]
        Recommended,

        [Display(Name = "Обязательное",
            Description = "Сохранение с незаполенным полем будет невозможно."),
            UsedImplicitly]
        Required,
    }

}
