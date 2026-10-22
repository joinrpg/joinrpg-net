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

/// <summary>Откуда строка бейджа берёт текст</summary>
public enum BadgeLineSource
{
    [Display(Name = "Поле проекта")]
    Field,
    [Display(Name = "Текст")]
    Text,
}

public class EditBadgeLineViewModel
{
    public BadgeLineSource Source { get; set; } = BadgeLineSource.Field;

    public ProjectFieldIdentification? FieldId { get; set; }

    public string? StaticText { get; set; }

    public int TopMm { get; set; } = 10;

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
                ProjectFieldId: line.Source == BadgeLineSource.Field ? line.FieldId?.ProjectFieldId : null,
                StaticText: line.Source == BadgeLineSource.Text ? line.StaticText : null,
                SlotLabel: null,
                line.TopMm,
                line.MinFontPt,
                line.MaxFontPt,
                line.Bold))]);

    public static EditBadgeTemplateViewModel FromDomain(ProjectBadgeTemplate template)
        => new()
        {
            Name = template.Name,
            WidthMm = template.Layout.WidthMm,
            HeightMm = template.Layout.HeightMm,
            BackgroundUrl = template.Layout.BackgroundUrl?.AbsoluteUri,
            Lines = [.. template.Layout.Lines.Select(line => new EditBadgeLineViewModel
            {
                Source = line.ProjectFieldId is null ? BadgeLineSource.Text : BadgeLineSource.Field,
                FieldId = line.ProjectFieldId is int fieldId ? new ProjectFieldIdentification(template.Id.ProjectId, fieldId) : null,
                StaticText = line.StaticText,
                TopMm = line.TopMm,
                MinFontPt = line.MinFontPt,
                MaxFontPt = line.MaxFontPt,
                Bold = line.Bold,
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
