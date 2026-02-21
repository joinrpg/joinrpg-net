using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.PrimitiveTypes.Characters;

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
