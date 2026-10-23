using System.ComponentModel.DataAnnotations;
using JoinRpg.DomainTypes.ProjectMetadata.Badges;

namespace JoinRpg.Web.ProjectMasterTools.Badges;

public record BadgeTemplateListItemViewModel(
    ProjectBadgeTemplateIdentification Id,
    string Name,
    int WidthMm,
    int HeightMm,
    bool IsActive);

/// <param name="HasEditAccess">Можно ли создавать, править и удалять шаблоны</param>
public record BadgeTemplateListViewModel(
    IReadOnlyList<BadgeTemplateListItemViewModel> Items,
    bool HasEditAccess);

/// <summary>От какого края бейджа отсчитывается отступ строки — подписи для формы</summary>
public enum BadgeLineEdgeView
{
    [Display(Name = "от верха")]
    Top,
    [Display(Name = "от низа")]
    Bottom,
}

/// <summary>
/// Строка формы. Без поля текст печатается как есть; с полем печатается значение поля, а текст —
/// пример для превью.
/// </summary>
public class EditBadgeLineViewModel
{
    public ProjectFieldIdentification? FieldId { get; set; }

    public string? StaticText { get; set; }

    public int OffsetMm { get; set; } = 10;

    public BadgeLineEdgeView Edge { get; set; } = BadgeLineEdgeView.Top;

    public int MinFontPt { get; set; } = 10;

    public int MaxFontPt { get; set; } = 24;

    public bool Bold { get; set; }
}

/// <summary>
/// Форма шаблона бейджа. Проверки — те же <see cref="BadgeLayoutRules"/>, что и на сервере: форма лишь
/// показывает их до отправки.
/// </summary>
public class EditBadgeTemplateViewModel : IValidatableObject
{
    [Display(Name = "Название")]
    public string Name { get; set; } = "";

    [Display(Name = "Ширина, мм")]
    public int WidthMm { get; set; } = 90;

    [Display(Name = "Высота, мм")]
    public int HeightMm { get; set; } = 55;

    [Display(Name = "Фоновая картинка", Description = "Ссылка на картинку (http:// или https://). Картинка растягивается на весь бейдж, лишнее обрезается.")]
    public string? BackgroundUrl { get; set; }

    public List<EditBadgeLineViewModel> Lines { get; set; } = [];

    public BadgeLayout ToLayout()
        => new(
            WidthMm,
            HeightMm,
            ParseBackground(),
            [.. Lines.Select(line => new BadgeLine(
                ProjectFieldId: line.FieldId?.ProjectFieldId,
                StaticText: string.IsNullOrWhiteSpace(line.StaticText) ? null : line.StaticText,
                SlotLabel: null,
                line.OffsetMm,
                line.MinFontPt,
                line.MaxFontPt,
                line.Bold,
                line.Edge == BadgeLineEdgeView.Bottom ? BadgeLineAnchor.Bottom : BadgeLineAnchor.Top))]);

    /// <summary>
    /// Новый шаблон: сразу с подписью сайта у нижнего края — мастер может её убрать или поменять.
    /// </summary>
    public static EditBadgeTemplateViewModel CreateNew()
        => new()
        {
            Lines =
            [
                new EditBadgeLineViewModel
                {
                    StaticText = "при помощи joinrpg.ru",
                    OffsetMm = 5,
                    Edge = BadgeLineEdgeView.Bottom,
                    MinFontPt = 6,
                    MaxFontPt = 6,
                },
            ],
        };

    public static EditBadgeTemplateViewModel FromDomain(ProjectBadgeTemplate template)
        => new()
        {
            Name = template.Name,
            WidthMm = template.Layout.WidthMm,
            HeightMm = template.Layout.HeightMm,
            BackgroundUrl = template.Layout.BackgroundUrl?.AbsoluteUri,
            Lines = [.. template.Layout.Lines.Select(line => new EditBadgeLineViewModel
            {
                FieldId = line.ProjectFieldId is int fieldId ? new ProjectFieldIdentification(template.Id.ProjectId, fieldId) : null,
                StaticText = line.StaticText,
                OffsetMm = line.OffsetMm,
                MinFontPt = line.MinFontPt,
                MaxFontPt = line.MaxFontPt,
                Bold = line.Bold,
                Edge = line.Anchor == BadgeLineAnchor.Bottom ? BadgeLineEdgeView.Bottom : BadgeLineEdgeView.Top,
            })],
        };

    // Неразборчивая ссылка в макет не попадает (ParseBackground даёт null) — о ней говорит отдельная ошибка,
    // иначе шаблон молча сохранился бы без фона.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(BackgroundUrl) && ParseBackground() is null)
        {
            yield return new ValidationResult("Ссылка на фон должна быть полным адресом http:// или https://", [nameof(BackgroundUrl)]);
        }
        foreach (var error in BadgeLayoutRules.GetErrors(Name, ToLayout()))
        {
            yield return new ValidationResult(error);
        }
    }

    private Uri? ParseBackground()
        => Uri.TryCreate(BackgroundUrl?.Trim(), UriKind.Absolute, out var uri) ? uri : null;
}
