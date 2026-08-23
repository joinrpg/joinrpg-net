using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace JoinRpg.Common.WebComponents;

/// <summary>
/// Как отрисовать иконку: результат <see cref="JoinIconMarkup.Describe"/>.
/// </summary>
/// <param name="CssClass">Css-классы иконки, включая цвет.</param>
/// <param name="InlineStyle">Значение атрибута style или <c>null</c>.</param>
/// <param name="SymbolHref">Адрес символа в спрайте для вложенного тега <c>use</c>.</param>
public readonly record struct JoinIconHtml(
    string CssClass,
    string? InlineStyle,
    string SymbolHref);

/// <summary>
/// Разметка иконки, общая для Blazor-компонента <c>JoinIcon</c> и тег-хелпера MVC.
/// Иконка — это всегда <c>&lt;svg&gt;</c> со ссылкой на символ спрайта, других способов нет.
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
    /// Адрес спрайта с иконками. Версия сборки в адресе — чтобы добавленная иконка
    /// не потерялась из-за закэшированного браузером старого спрайта.
    /// </summary>
    public static readonly string SpriteUrl =
        "/_content/JoinRpg.Common.WebComponents/icons/join-icons.svg?v="
        + Uri.EscapeDataString(
            typeof(JoinIconMarkup).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion
            ?? "0");

    /// <summary>
    /// Скрипт, объявляющий перечисленные иконки для функции <c>joinIcon</c> из join-obsolete-icons.js.
    /// </summary>
    /// <remarks>
    /// Нужен только тем страницам, где остался старый скрипт, строящий разметку строками.
    /// Иконки перечисляются явно, чтобы страница не тащила таблицу, которая ей не нужна.
    /// </remarks>
    /// <param name="icons">Иконки, которые понадобятся скриптам страницы.</param>
    public static string BuildScript(params JoinIconType[] icons)
    {
        var symbols = icons.ToDictionary(
            icon => icon.ToString(),
            icon => JoinIconDefinitions.Get(icon).IconName);

        return $"window.joinIconSprite={JsonSerializer.Serialize(SpriteUrl)};"
            + $"window.joinIconSymbols={JsonSerializer.Serialize(symbols)};";
    }

    /// <summary>
    /// Как отрисовать иконку.
    /// </summary>
    /// <param name="icon">Иконка.</param>
    /// <param name="size">Размер иконки.</param>
    public static JoinIconHtml Describe(JoinIconType icon, SizeStyleEnum? size = null)
    {
        var definition = JoinIconDefinitions.Get(icon);
        return new JoinIconHtml(
            CssClass: BuildCssClass(definition),
            InlineStyle: BuildSizeStyle(size),
            SymbolHref: SpriteUrl + "#" + definition.IconName);
    }

    private static string BuildCssClass(JoinIconDefinition definition)
    {
        // Цвет — часть смысла иконки, а не оформление на месте использования:
        // нужен другой цвет — заводите новое значение JoinIconType.
        // BootstrapStyle для null отдаёт text-default, чего у иконок сегодня нигде нет.
        return definition.Variation is null
            ? "join-icon"
            : "join-icon " + BootstrapStyle.Build("text", definition.Variation, size: null);
    }

    /// <summary>
    /// Значение атрибута style для заданного размера. <c>null</c> для размера по умолчанию.
    /// </summary>
    /// <remarks>
    /// Размер задаётся инлайном, а не через scoped css: та же разметка строится тег-хелпером,
    /// на который изоляция стилей Blazor не распространяется. Работает потому,
    /// что размеры самой иконки в join-icons.css заданы в em.
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
        builder.Append("<svg class=\"").Append(html.CssClass).Append('"');
        if (html.InlineStyle is not null)
        {
            builder.Append(" style=\"").Append(html.InlineStyle).Append('"');
        }
        builder.Append(BuildAccessibilityAttributes(title)).Append('>');
        builder.Append(BuildContent(html, title));
        builder.Append("</svg>");
        return builder.ToString();
    }

    /// <summary>
    /// Атрибуты доступности. Без подсказки иконка декоративна и прячется от скринридера,
    /// с подсказкой — становится картинкой с названием.
    /// </summary>
    /// <param name="title">Всплывающая подсказка.</param>
    public static string BuildAccessibilityAttributes(string? title)
        => string.IsNullOrWhiteSpace(title)
            ? " aria-hidden=\"true\" focusable=\"false\""
            : " role=\"img\" focusable=\"false\"";

    /// <summary>
    /// Содержимое тега svg: ссылка на символ спрайта и, если задана, подсказка.
    /// </summary>
    /// <remarks>
    /// Подсказка — вложенный тег <c>title</c>, а не атрибут: у svg атрибут title браузеры не показывают.
    /// </remarks>
    /// <param name="html">Описание иконки.</param>
    /// <param name="title">Всплывающая подсказка.</param>
    public static string BuildContent(JoinIconHtml html, string? title)
    {
        var titleTag = string.IsNullOrWhiteSpace(title)
            ? ""
            : "<title>" + WebUtility.HtmlEncode(title) + "</title>";
        return titleTag + "<use href=\"" + html.SymbolHref + "\"></use>";
    }
}
