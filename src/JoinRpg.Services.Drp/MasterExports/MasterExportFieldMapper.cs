using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.Services.Drp.MasterExports;

/// <summary>
/// Сопоставляет колонки выгрузки мастера (Excel-файл списка персонажей или заявок) с полями проекта
/// и превращает отображаемый текст значения обратно в хранимое значение поля.
/// </summary>
/// <remarks>
/// Выгрузка пишет заголовок колонки = имя поля, значение = <see cref="FieldWithValue.DisplayString"/>
/// (см. <c>CustomExporter.FieldColumn</c>). Разбор — обратная операция: <see cref="FieldWithValue.DisplayString"/>
/// от <see cref="MasterExportFieldValue.StoredValue"/> даёт исходный текст.
/// </remarks>
public static class MasterExportFieldMapper
{
    private const string CheckboxOnPrefix = "☑";
    private const string CheckboxOffPrefix = "☐";

    /// <summary>
    /// Сопоставляет заголовки с полями проекта — по имени, с обрезкой пробелов. Сначала точное совпадение,
    /// затем без учёта регистра (с пометкой). Учитываются поля проекта, включая выключенные
    /// и привязанные к заявке, — выгрузка пишет и их; кроме полей без значения (заголовков разделов):
    /// их в выгрузке нет, и одноимённый раздел не должен делать поле неоднозначным.
    /// </summary>
    /// <returns>По одному сопоставлению на заголовок, в том же порядке.</returns>
    public static IReadOnlyList<MasterExportColumnMapping> MapColumns(ProjectInfo project, IReadOnlyCollection<string> headers)
    {
        var headerCounts = headers
            .GroupBy(h => h.Trim(), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        var fields = project.UnsortedFields.Where(f => f.CanHaveValue).ToList();

        return [.. headers.Select(header => MapColumn(fields, header, headerCounts[header.Trim()] > 1))];
    }

    private static MasterExportColumnMapping MapColumn(IReadOnlyCollection<ProjectFieldInfo> fields, string header, bool duplicated)
    {
        var name = header.Trim();
        var warnings = new List<MasterExportFieldWarning>();

        var candidates = fields.Where(f => string.Equals(f.Name.Trim(), name, StringComparison.Ordinal)).ToList();
        if (candidates.Count == 0)
        {
            candidates = [.. fields.Where(f => string.Equals(f.Name.Trim(), name, StringComparison.OrdinalIgnoreCase))];
            if (candidates.Count > 0)
            {
                warnings.Add(MasterExportFieldWarning.ColumnCaseMismatch);
            }
        }

        switch (candidates)
        {
            case []:
                return new(header, null, [MasterExportFieldWarning.ColumnNotMapped]);
            case [_, _, ..]:
                warnings.Add(MasterExportFieldWarning.ColumnAmbiguous);
                return new(header, null, warnings);
        }

        var field = candidates[0];
        if (duplicated)
        {
            warnings.Add(MasterExportFieldWarning.ColumnDuplicated);
        }
        warnings.AddRange(GetFieldStateWarnings(field));
        return new(header, field.Id, warnings);
    }

    /// <summary>
    /// Превращает текст ячейки выгрузки в хранимое значение поля.
    /// </summary>
    /// <param name="field">Поле, с которым сопоставлена колонка.</param>
    /// <param name="displayText">
    /// Текст ячейки. Маркеры обрезки («(обрезано) », «...») здесь уже сняты разборщиком файла —
    /// приходит чистый текст, а факт обрезки передаётся в <paramref name="truncated"/>.
    /// </param>
    /// <param name="truncated">
    /// Ячейка обрезана при выгрузке (маркер снят разборщиком) — значение неполное и по умолчанию не применяется.
    /// </param>
    public static MasterExportFieldValue MapValue(ProjectFieldInfo field, string displayText, bool truncated)
    {
        var warnings = GetFieldStateWarnings(field);
        if (truncated)
        {
            warnings.Add(MasterExportFieldWarning.Truncated);
        }

        var storedValue = field.CanHaveValue ? ParseValue(field, displayText, warnings) : null;

        // Любая пометка — мастер решает сам
        return new(field.Id, storedValue, ApplyByDefault: warnings.Count == 0, warnings);
    }

    private static List<MasterExportFieldWarning> GetFieldStateWarnings(ProjectFieldInfo field)
    {
        var warnings = new List<MasterExportFieldWarning>();
        if (!field.IsActive)
        {
            warnings.Add(MasterExportFieldWarning.FieldInactive);
        }
        if (!field.CanHaveValue)
        {
            warnings.Add(MasterExportFieldWarning.FieldCannotHaveValue);
        }
        return warnings;
    }

    private static string? ParseValue(ProjectFieldInfo field, string text, List<MasterExportFieldWarning> warnings)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (field.Type == ProjectFieldType.Checkbox)
        {
            return ParseCheckbox(text.Trim(), warnings);
        }

        if (field.HasValueList)
        {
            return ParseVariants(field, text, warnings);
        }

        if (field.Type.IsUserLink())
        {
            return ParseUserIds(field, text.Trim(), warnings);
        }

        return text;
    }

    private static string? ParseCheckbox(string text, List<MasterExportFieldWarning> warnings)
    {
        // «☑️» — символ и вариационный селектор; табличный редактор мог селектор потерять
        if (text.StartsWith(CheckboxOnPrefix, StringComparison.Ordinal))
        {
            return FieldWithValue.CheckboxValueOn;
        }
        if (!text.StartsWith(CheckboxOffPrefix, StringComparison.Ordinal))
        {
            warnings.Add(MasterExportFieldWarning.InvalidCheckboxValue);
        }
        return null;
    }

    private static string? ParseVariants(ProjectFieldInfo field, string text, List<MasterExportFieldWarning> warnings)
    {
        var result = MasterExportVariantParser.Parse(field, text);
        if (result.HasUnknown)
        {
            warnings.Add(MasterExportFieldWarning.VariantNotFound);
        }
        if (result.IsAmbiguous)
        {
            warnings.Add(MasterExportFieldWarning.VariantAmbiguous);
        }
        if (result.Variants.Any(v => !v.IsActive))
        {
            warnings.Add(MasterExportFieldWarning.VariantInactive);
        }
        // Комната и слот расписания — тоже мультивыбор, одиночный выбор только у выпадающего списка
        if (field.Type == ProjectFieldType.Dropdown && result.Variants.Count > 1)
        {
            warnings.Add(MasterExportFieldWarning.MultipleVariantsForSingleChoice);
        }

        return result.Variants.Count == 0
            ? null
            : string.Join(",", result.Variants.Select(v => v.Id.ProjectFieldVariantId));
    }

    private static string? ParseUserIds(ProjectFieldInfo field, string text, List<MasterExportFieldWarning> warnings)
    {
        // Выгрузка пишет сырые id через запятую — оставляем как есть, только проверяем
        var ids = new List<UserIdentification>();
        var valid = true;
        foreach (var part in text.Split(','))
        {
            var trimmed = part.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }
            if (UserIdentification.TryParse(trimmed, null, out var id))
            {
                ids.Add(id);
            }
            else
            {
                valid = false;
            }
        }

        // Повтор одного и того же id — не ошибка, домен сам схлопнет дубликаты
        var distinct = ids.Distinct().ToList();
        if (!field.Type.IsMultiUserLink() && distinct.Count > 1)
        {
            // Одиночное поле с несколькими пользователями домен отвергнет — непонятно, кого оставить
            warnings.Add(MasterExportFieldWarning.InvalidUserIds);
            return null;
        }

        if (valid)
        {
            return text;
        }

        warnings.Add(MasterExportFieldWarning.InvalidUserIds);
        return distinct.Count == 0 ? null : string.Join(",", distinct.Select(id => id.Value));
    }
}
