using System.Diagnostics.CodeAnalysis;
using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Domain.CharacterFields;

internal abstract class FieldSaveStrategyBase(Claim? claim,
    Character? character,
    int currentUserId,
    IFieldDefaultValueGenerator generator,
    ProjectInfo projectInfo,
    AccessArguments accessArguments)
{
    protected Claim? Claim { get; } = claim;
    protected Character? Character { get; } = character;

    protected ProjectInfo ProjectInfo { get; } = projectInfo;

    private List<FieldWithPreviousAndNewValue> UpdatedFields { get; } = [];

    public virtual void Save(Dictionary<int, FieldWithValue> fields) => SerializeFields(fields);

    protected abstract void SerializeFields(Dictionary<int, FieldWithValue> fields);

    protected abstract IReadOnlyCollection<FieldWithValue> GetFields();

    private void EnsureEditAccess(FieldWithValue field)
    {
        var editAccess = field.Field.HasEditAccess(accessArguments);
        if (!editAccess)
        {
            throw new NoAccessToProjectException(ProjectInfo, currentUserId);
        }
    }

    /// <summary>
    /// Returns true is the value has changed
    /// </summary>
    public bool AssignFieldValue(FieldWithValue field, string? newValue)
    {
        if (field.Value == newValue)
        {
            return false;
        }

        var existingField = UpdatedFields.FirstOrDefault(uf => uf.Field == field.Field);
        if (existingField != null)
        {
            existingField.Value = newValue;
        }
        else
        {
            UpdatedFields.Add(
                new FieldWithPreviousAndNewValue(field.Field, newValue, field.Value));
        }

        field.Value = newValue;

        return true;
    }

    public string? GenerateDefaultValue(FieldWithValue field)
    {
        return field.Field.BoundTo switch
        {
            FieldBoundTo.Character => generator.CreateDefaultValue(Character, field),
            FieldBoundTo.Claim => generator.CreateDefaultValue(Claim, field),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    protected abstract void SetCharacterNameFromPlayer();

    private static string? NormalizeValueBeforeAssign(FieldWithValue field, string? toAssign)
    {
        return field.Field.Type switch
        {
            ProjectFieldType.Checkbox => toAssign?.StartsWith(FieldWithValue.CheckboxValueOn) == true
                                ? FieldWithValue.CheckboxValueOn
                                : "",
            _ => string.IsNullOrEmpty(toAssign) ? null : toAssign,
        };
    }

    public void GenerateDefaultValues(Dictionary<int, FieldWithValue> fields)
    {
        foreach (var field in fields.Values.Where(
            f => !f.HasEditableValue && f.Field.CanHaveValue &&
                 f.Field.IsAvailableForTarget(Character)))
        {
            var newValue = GenerateDefaultValue(field);

            var normalizedValue = NormalizeValueBeforeAssign(field, newValue);

            _ = AssignFieldValue(field, normalizedValue);
        }
    }

    public void AssignValues(IReadOnlyDictionary<int, string?> newFieldValue, Dictionary<int, FieldWithValue> fields)
    {
        foreach (var keyValuePair in newFieldValue)
        {
            var field = fields[keyValuePair.Key];

            EnsureEditAccess(field);

            var normalizedValue = NormalizeValueBeforeAssign(field, keyValuePair.Value);

            if (normalizedValue is null && FieldIsMandatory(field))
            {
                ThrowRequiredField(field);
                return;
            }

            _ = AssignFieldValue(field, normalizedValue);
        }
    }

    protected abstract bool FieldIsMandatory(FieldWithValue field);

    [DoesNotReturn]
    protected abstract void ThrowRequiredField(FieldWithValue field);

    public IReadOnlyCollection<FieldWithPreviousAndNewValue> PerformSave(IReadOnlyDictionary<int, string?> newFieldValue)
    {
        var fields = GetFields().ToDictionary(f => f.Field.Id.ProjectFieldId);

        AssignValues(newFieldValue, fields);

        GenerateDefaultValues(fields);

        Save(fields);
        return UpdatedFields;
    }
}
