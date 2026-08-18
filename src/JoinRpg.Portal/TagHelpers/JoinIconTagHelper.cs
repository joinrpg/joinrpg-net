using JoinRpg.WebComponents;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JoinRpg.Portal.TagHelpers;

/// <summary>
/// Иконка Joinrpg в MVC-разметке: <c>&lt;join-icon icon="Delete" title="Удалить" /&gt;</c>.
/// </summary>
/// <remarks>
/// Что рисовать, решает <see cref="JoinIconMarkup"/> — тот же код, что и у Blazor-компонента <c>JoinIcon</c>.
/// Тег-хелпер живёт в Portal, а не в JoinRpg.WebComponents, потому что в Razor Class Library,
/// на которую ссылается Blazor WebAssembly, нельзя тянуть ASP.NET Core MVC.
/// Все прочие атрибуты, написанные на теге, попадают в итоговую разметку.
/// </remarks>
[HtmlTargetElement("join-icon", TagStructure = TagStructure.WithoutEndTag)]
public class JoinIconTagHelper : TagHelper
{
    /// <summary>Какую иконку показываем.</summary>
    [HtmlAttributeName("icon")]
    public JoinIconType Icon { get; set; }

    /// <summary>Всплывающая подсказка.</summary>
    [HtmlAttributeName("title")]
    public string? Title { get; set; }

    /// <summary>Размер иконки. По умолчанию — как у окружающего текста.</summary>
    [HtmlAttributeName("size")]
    public SizeStyleEnum? Size { get; set; }

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var html = JoinIconMarkup.Describe(Icon, Size);

        output.TagName = html.TagName;
        output.TagMode = html.ImageUrl is null ? TagMode.StartTagAndEndTag : TagMode.SelfClosing;

        output.Attributes.SetAttribute("class", html.CssClass);
        if (html.InlineStyle is not null)
        {
            output.Attributes.SetAttribute("style", html.InlineStyle);
        }
        if (!string.IsNullOrWhiteSpace(Title))
        {
            output.Attributes.SetAttribute("title", Title);
        }

        if (html.ImageUrl is not null)
        {
            output.Attributes.SetAttribute("src", html.ImageUrl);
            output.Attributes.SetAttribute("alt", "");
            return;
        }

        output.Attributes.SetAttribute("aria-hidden", "true");
        if (html.TextContent is not null)
        {
            _ = output.Content.SetContent(html.TextContent);
        }
    }
}
