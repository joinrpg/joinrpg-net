using System.Text.Json.Serialization;
using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.DomainTypes.ProjectMetadata.Badges;

[method: JsonConstructor]
[TypedEntityId]
public partial record ProjectBadgeTemplateIdentification(ProjectIdentification ProjectId, int ProjectBadgeTemplateId) : IProjectEntityId;

/// <summary>
/// Шаблон бейджа проекта: прямоугольник заданного размера с фоновой картинкой и строками поверх неё.
/// </summary>
/// <param name="Id">Идентификатор шаблона</param>
/// <param name="Name">Как шаблон называется в списках (например, «Игрок» или «Мастер»)</param>
/// <param name="Layout">Размеры, фон и строки бейджа</param>
/// <param name="IsActive">Удалённый шаблон выключается, а не стирается — его можно восстановить</param>
public record ProjectBadgeTemplate(
    ProjectBadgeTemplateIdentification Id,
    string Name,
    BadgeLayout Layout,
    bool IsActive);

/// <summary>
/// Раскладка бейджа. Общая для шаблонов проекта и общих шаблонов сайта.
/// </summary>
/// <param name="WidthMm">Ширина бейджа, мм</param>
/// <param name="HeightMm">Высота бейджа, мм</param>
/// <param name="BackgroundUrl">Ссылка на фоновую картинку (внешняя, копию не храним). <c>null</c> — без фона</param>
/// <param name="Lines">Строки бейджа. Каждая выровнена по центру по горизонтали</param>
public record BadgeLayout(
    int WidthMm,
    int HeightMm,
    Uri? BackgroundUrl,
    IReadOnlyList<BadgeLine> Lines);

/// <summary>
/// Строка бейджа. Значение берётся из поля проекта (<see cref="ProjectFieldId"/>) или из
/// статического текста (<see cref="StaticText"/>) — ровно из одного источника. Строка общего шаблона
/// ещё не привязана к полю: тогда оба источника пустые, а <see cref="SlotLabel"/> подсказывает,
/// какое поле к ней привязать при копировании в проект.
/// </summary>
/// <param name="ProjectFieldId">Id поля проекта (без проекта — проект задаёт сам шаблон)</param>
/// <param name="StaticText">Неизменный текст, одинаковый на всех бейджах</param>
/// <param name="SlotLabel">Подпись строки в редакторе, например «Имя персонажа»</param>
/// <param name="TopMm">Отступ верхнего края строки от верха бейджа, мм</param>
/// <param name="MinFontPt">Минимальный размер шрифта, пт. Если и на нём текст не влезает — переносится</param>
/// <param name="MaxFontPt">Максимальный размер шрифта, пт. Короткий текст печатается им</param>
/// <param name="Bold">Полужирный шрифт</param>
public record BadgeLine(
    int? ProjectFieldId,
    string? StaticText,
    string? SlotLabel,
    int TopMm,
    int MinFontPt,
    int MaxFontPt,
    bool Bold);
