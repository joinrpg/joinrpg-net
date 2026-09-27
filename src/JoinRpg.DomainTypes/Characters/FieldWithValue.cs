using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Helpers;

namespace JoinRpg.DomainTypes.Characters;

public sealed class FieldWithValue
{
    private IReadOnlyList<int> SelectedIds { get; set; } = [];

    public FieldWithValue(ProjectFieldInfo field, string? value)
    {
        Field = field;
        Value = value;
    }

    public ProjectFieldInfo Field { get; }

    public string? Value
    {
        get;
        set
        {
            field = value;
            if (Field.HasValueList)
            {
                SelectedIds = Value?.ParseToIntList() ?? [];
            }
        }
    }

    public string DisplayString
    {
        get
        {
            if (Field.Type == ProjectFieldType.Checkbox)
            {
                return Value?.StartsWith(CheckboxValueOn) == true ? "☑️" : "☐";
            }

            if (Field.HasValueList)
            {
                return
                    Field.Variants.Where(dv =>
                            SelectedIds.Contains(dv.Id.ProjectFieldVariantId))
                        .Select(dv => dv.Label)
                        .JoinStrings(", ");
            }

            return Value ?? "";
        }
    }

    /// <summary>
    /// Идентификаторы пользователей для полей-ссылок на пользователя (ADR017).
    /// Отдельно от <see cref="SelectedIds"/>: те заполняются только у полей со списком вариантов.
    /// </summary>
    /// <remarks>
    /// Читает терпимо: мусор в уже сохранённом значении не должен ронять показ страницы.
    /// Строгая проверка — при сохранении, в <see cref="NormalizeValueBeforeAssign"/>.
    /// </remarks>
    public IReadOnlyList<UserIdentification> UserIds
        => Field.Type.IsUserLink() ? ParseUserIds(Value, strict: false) : [];

    public bool HasEditableValue => !string.IsNullOrWhiteSpace(Value);

    public bool HasViewableValue => !string.IsNullOrWhiteSpace(Value) || !Field.CanHaveValue;

    public IEnumerable<ProjectFieldVariant> GetPossibleValues(AccessArguments modelAccessArguments)
        => Field.GetPossibleVariants(modelAccessArguments, SelectedIds);

    public IEnumerable<(ProjectFieldVariant variant, bool selected)> GetPossibleVariantsWithSelection(AccessArguments modelAccessArguments)
        => Field.GetPossibleVariants(modelAccessArguments, SelectedIds)
        .Select(variant => (variant, SelectedIds.Contains(variant.Id.ProjectFieldVariantId)));

    public IEnumerable<ProjectFieldVariant> GetDropdownValues() => Field.SortedVariants.Where(v => SelectedIds.Contains(v.Id.ProjectFieldVariantId));

    public IEnumerable<CharacterGroupIdentification> GetSpecialGroupsToApply() => Field.HasSpecialGroup ? GetDropdownValues().Select(c => c.CharacterGroupId).WhereNotNull() : [];

    public override string ToString() => $"{Field.Name}={Value}";

    public const string CheckboxValueOn = "on";

    /// <summary>
    /// Нормализует значение перед присваиванием полю. Для полей с вариантами проверяет корректность идентификаторов вариантов.
    /// </summary>
    public string? NormalizeValueBeforeAssign(string? toAssign)
    {
        var normalized = Field.Type switch
        {
            ProjectFieldType.Checkbox => toAssign?.StartsWith(CheckboxValueOn) == true
                                ? CheckboxValueOn
                                : "",
            _ => string.IsNullOrEmpty(toAssign) ? null : toAssign,
        };

        if (normalized is not null && Field.HasValueList)
        {
            var newIds = normalized.ParseToIntList();
            var existingIds = Value?.ParseToIntList() ?? [];
            Field.ValidateVariantList(newIds, existingIds);
        }

        if (normalized is not null && Field.Type.IsUserLink())
        {
            var userIds = ParseUserIds(normalized, strict: true);
            // У мультивыбора идентификаторов может быть сколько угодно, у одиночного — не больше одного
            if (!Field.Type.IsMultiUserLink() && userIds.Count > 1)
            {
                throw new FieldUserValueInvalidException(Field.Id, normalized);
            }
            normalized = userIds.Count == 0 ? null : userIds.Select(id => id.Value.ToString()).JoinStrings(",");
        }

        return normalized;
    }

    /// <summary>
    /// Разбирает значение поля-ссылки на пользователя. Дубликаты отбрасываются, порядок сохраняется.
    /// </summary>
    /// <param name="value">Сырое значение поля — идентификаторы через запятую</param>
    /// <param name="strict">
    /// true — нераспознанный кусок значения бросает <see cref="FieldUserValueInvalidException"/>,
    /// false — молча пропускается.
    /// </param>
    private List<UserIdentification> ParseUserIds(string? value, bool strict)
    {
        var result = new List<UserIdentification>();
        if (string.IsNullOrWhiteSpace(value))
        {
            return result;
        }

        foreach (var part in value.Split(','))
        {
            var trimmed = part.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            // UserIdentification.TryParse не принимает неположительные значения
            if (!UserIdentification.TryParse(trimmed, null, out var userId))
            {
                if (strict)
                {
                    throw new FieldUserValueInvalidException(Field.Id, value);
                }
                continue;
            }

            if (!result.Contains(userId))
            {
                result.Add(userId);
            }
        }

        return result;
    }

    public int GetCurrentFee()
    {
        if (!Field.SupportsPricing)
        {
            return 0;
        }
        return Field.Type
        switch
        {
            ProjectFieldType.Checkbox => HasEditableValue ? Field.Price : 0,
            ProjectFieldType.Number => TryConvertToInt() * Field.Price,
            ProjectFieldType.Dropdown => GetDropdownValues().Sum(v => v.Price),
            ProjectFieldType.MultiSelect => GetDropdownValues().Sum(v => v.Price),

            _ => throw new NotSupportedException("Can't calculate pricing"),
        };
    }

    private int TryConvertToInt() => int.TryParse(Value, out var result) ? result : 0;

}
