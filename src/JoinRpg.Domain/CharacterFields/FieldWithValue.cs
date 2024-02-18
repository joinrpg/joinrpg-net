using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Domain;

public class FieldWithValue
{
    private string? _value;

    private IReadOnlyList<int> SelectedIds { get; set; } = new List<int>();

    public FieldWithValue(ProjectFieldInfo field, string? value)
    {
        Field = field;
        Value = value;
    }

    public ProjectFieldInfo Field { get; }

    public string? Value
    {
        get => _value;
        set
        {
            _value = value;
            if (Field.HasValueList)
            {
                SelectedIds = Value.ToIntList();
            }
        }
    }

    public string DisplayString => GetDisplayValue(Value, SelectedIds);

    //TODO: there is a bug here (note Value used instead of value)
    protected string GetDisplayValue(string? value, IReadOnlyList<int> selectedIDs)
    {
        if (Field.Type == ProjectFieldType.Checkbox)
        {
            return Value?.StartsWith(CheckboxValueOn) == true ? "☑️" : "☐";
        }

        if (Field.HasValueList)
        {
            return
                Field.Variants.Where(dv =>
                        selectedIDs.Contains(dv.Id.ProjectFieldVariantId))
                    .Select(dv => dv.Label)
                    .JoinStrings(", ");
        }

        return value ?? "";
    }

    public bool HasEditableValue => !string.IsNullOrWhiteSpace(Value);

    public bool HasViewableValue => !string.IsNullOrWhiteSpace(Value) || !Field.CanHaveValue;

    public IEnumerable<ProjectFieldVariant> GetPossibleValues(AccessArguments modelAccessArguments)
        => Field.GetPossibleVariants(modelAccessArguments, SelectedIds);

    public IEnumerable<(ProjectFieldVariant variant, bool selected)> GetPossibleVariantsWithSelection(AccessArguments modelAccessArguments)
        => Field.GetPossibleVariants(modelAccessArguments, SelectedIds)
        .Select(variant => (variant, SelectedIds.Contains(variant.Id.ProjectFieldVariantId)));

    public IEnumerable<ProjectFieldVariant> GetDropdownValues() => Field.SortedVariants.Where(v => SelectedIds.Contains(v.Id.ProjectFieldVariantId));

    public IEnumerable<int> GetSpecialGroupsToApply() => Field.HasSpecialGroup ? GetDropdownValues().Select(c => c.CharacterGroupId).WhereNotNull() : Enumerable.Empty<int>();

    public override string ToString() => $"{Field.Name}={Value}";

    /// <summary>
    /// Returns value as integer with respect to field type.
    /// If current value could not be converted, returns default(int)
    /// </summary>
    public int ToInt()
    {
        if (!int.TryParse(Value, out var result))
        {
            result = default;
        }

        return result;
    }

    public const string CheckboxValueOn = "on";

}
