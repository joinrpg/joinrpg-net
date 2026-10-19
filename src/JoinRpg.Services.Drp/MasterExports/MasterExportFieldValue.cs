using JoinRpg.DomainTypes;

namespace JoinRpg.Services.Drp.MasterExports;

/// <summary>
/// Значение поля, восстановленное из текста ячейки выгрузки мастера.
/// </summary>
/// <param name="Field">Поле проекта.</param>
/// <param name="StoredValue">
/// Значение в том виде, в каком его хранит поле: для чекбокса «on», для списков — id вариантов через запятую.
/// null — пусто. Отображение этого значения (<c>FieldWithValue.DisplayString</c>) даёт исходный текст ячейки.
/// </param>
/// <param name="ApplyByDefault">Применять ли значение, если мастер ничего не поменял.</param>
/// <param name="Warnings">Пометки разбора.</param>
public record MasterExportFieldValue(
    ProjectFieldIdentification Field,
    string? StoredValue,
    bool ApplyByDefault,
    IReadOnlyList<MasterExportFieldWarning> Warnings);
