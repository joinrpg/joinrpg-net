using System.Net;
using System.Text;

namespace JoinRpg.WebComponents;

/// <summary>
/// Как отрисовать иконку: результат <see cref="JoinIconMarkup.Describe"/>.
/// </summary>
/// <param name="TagName">Имя html-тега — <c>span</c> либо <c>img</c>.</param>
/// <param name="CssClass">Css-классы иконки, включая цвет.</param>
/// <param name="InlineStyle">Значение атрибута style или <c>null</c>.</param>
/// <param name="TextContent">Содержимое тега для юникодных иконок, иначе <c>null</c>.</param>
/// <param name="ImageUrl">Адрес картинки для иконок-картинок, иначе <c>null</c>.</param>
public readonly record struct JoinIconHtml(
    string TagName,
    string CssClass,
    string? InlineStyle,
    string? TextContent,
    string? ImageUrl);

/// <summary>
/// Разметка иконки, общая для Blazor-компонента <c>JoinIcon</c> и тег-хелпера MVC.
/// </summary>
/// <remarks>
/// Прикладной код должен использовать компонент <c>JoinIcon</c> (в .razor) или
/// тег-хелпер <c>&lt;join-icon /&gt;</c> (в .cshtml). Публичным этот класс сделан только
/// затем, чтобы тег-хелпер жил в JoinRpg.Portal (в RCL нельзя тянуть ASP.NET Core MVC)
/// и при этом не знал ничего про иконочный набор.
/// </remarks>
public static class JoinIconMarkup
{
    /// <summary>
    /// Как отрисовать иконку.
    /// </summary>
    /// <param name="icon">Иконка.</param>
    /// <param name="size">Размер иконки.</param>
    public static JoinIconHtml Describe(JoinIconType icon, SizeStyleEnum? size = null)
    {
        var definition = JoinIconDefinitions.Get(icon);
        var isImage = definition.Kind == JoinIconKind.Image;
        return new JoinIconHtml(
            TagName: isImage ? "img" : "span",
            CssClass: BuildCssClass(definition),
            InlineStyle: BuildSizeStyle(size),
            TextContent: definition.Kind == JoinIconKind.Text ? definition.Value : null,
            ImageUrl: isImage ? definition.Value : null);
    }

    private static string BuildCssClass(JoinIconDefinition definition)
    {
        var cssClass = definition.Kind == JoinIconKind.Glyph
            ? $"join-icon {JoinIconDefinitions.GlyphFontCssClass} {definition.Value}"
            : "join-icon";

        // Цвет — часть смысла иконки, а не оформление на месте использования:
        // нужен другой цвет — заводите новое значение JoinIconType.
        // BootstrapStyle для null отдаёт text-default, чего у иконок сегодня нигде нет.
        return definition.Variation is null
            ? cssClass
            : cssClass + " " + BootstrapStyle.Build("text", definition.Variation, size: null);
    }

    /// <summary>
    /// Значение атрибута style для заданного размера. <c>null</c> для размера по умолчанию.
    /// </summary>
    /// <remarks>
    /// Размер задаётся инлайном, а не через scoped css: та же разметка строится тег-хелпером,
    /// на который изоляция стилей Blazor не распространяется.
    /// </remarks>
    private static string? BuildSizeStyle(SizeStyleEnum? size) => size switch
    {
        null or SizeStyleEnum.Medium => null,
        SizeStyleEnum.Large => "font-size: 1.5em;",
        SizeStyleEnum.Small => "font-size: 0.85em;",
        SizeStyleEnum.ExtraSmall => "font-size: 0.75em;",
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, "Некорректный размер иконки"),
    };

    /// <summary>
    /// Готовый html иконки. Используется в тестах; тег-хелпер строит разметку по <see cref="Describe"/>,
    /// чтобы не терять произвольные атрибуты, написанные на теге.
    /// </summary>
    /// <param name="icon">Иконка.</param>
    /// <param name="size">Размер иконки.</param>
    /// <param name="title">Всплывающая подсказка.</param>
    public static string BuildHtml(
        JoinIconType icon,
        SizeStyleEnum? size = null,
        string? title = null)
    {
        var html = Describe(icon, size);

        var builder = new StringBuilder();
        builder.Append('<').Append(html.TagName);
        builder.Append(" class=\"").Append(html.CssClass).Append('"');
        if (html.InlineStyle is not null)
        {
            builder.Append(" style=\"").Append(html.InlineStyle).Append('"');
        }
        if (!string.IsNullOrWhiteSpace(title))
        {
            builder.Append(" title=\"").Append(WebUtility.HtmlEncode(title)).Append('"');
        }

        if (html.ImageUrl is not null)
        {
            builder.Append(" src=\"").Append(html.ImageUrl).Append("\" alt=\"\" />");
            return builder.ToString();
        }

        builder.Append(" aria-hidden=\"true\">");
        // Не кодируем: текст берётся из JoinIconDefinitions, это наши собственные символы.
        // Кодирование дало бы &#215; там, где Blazor рендерит ×, и разметка перестала бы совпадать.
        builder.Append(html.TextContent);
        builder.Append("</").Append(html.TagName).Append('>');
        return builder.ToString();
    }
}
