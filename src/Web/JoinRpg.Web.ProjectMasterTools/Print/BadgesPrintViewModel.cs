using JoinRpg.Common.WebComponents.Badges;

namespace JoinRpg.Web.ProjectMasterTools.Print;

/// <summary>Страница печати бейджей по одному шаблону</summary>
/// <param name="TemplateName">Название шаблона — в заголовке страницы</param>
/// <param name="Badges">Строки каждого бейджа, по одному набору на персонажа</param>
/// <param name="Warnings">Что не так с шаблоном (например, удалённое поле) — показывается на экране, но не печатается</param>
public record BadgesPrintViewModel(
    string TemplateName,
    int WidthMm,
    int HeightMm,
    Uri? BackgroundUrl,
    IReadOnlyList<IReadOnlyList<PrintableBadgeLine>> Badges,
    IReadOnlyList<string> Warnings);
