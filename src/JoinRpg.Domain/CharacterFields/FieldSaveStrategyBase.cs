using System.Diagnostics.CodeAnalysis;
using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.CharacterFields;

internal abstract class FieldSaveStrategyBase(Claim? claim,
    Character? character,
    UserIdentification currentUserId,
    IFieldDefaultValueGenerator generator,
    ProjectInfo projectInfo,
    CharacterFieldLayers characterFieldLayers)
{
    protected Claim? Claim { get; } = claim;
    protected Character? Character { get; } = character;

    protected ProjectInfo ProjectInfo { get; } = projectInfo;

    protected CharacterFieldLayers CharacterFieldLayers { get; } = characterFieldLayers;

    protected AccessArguments AccessArguments { get; } = characterFieldLayers.AccessArguments;

    private Dictionary<ProjectFieldIdentification, FieldWithPreviousAndNewValue> UpdatedFields { get; } = [];

    protected virtual void Save(Dictionary<int, FieldWithValue> fields) => SerializeFields(fields);

    protected abstract void SerializeFields(Dictionary<int, FieldWithValue> fields);

    private void EnsureEditAccess(FieldWithValue field)
    {
        var editAccess = field.Field.HasEditAccess(AccessArguments);
        if (!editAccess)
        {
            throw new NoAccessToProjectException(ProjectInfo, currentUserId);
        }
    }

    /// <summary>
    /// Returns true is the value has changed
    /// </summary>
    private bool AssignFieldValue(FieldWithValue field, string? newValue)
    {
        if (field.Value == newValue)
        {
            return false;
        }

        var updated = new FieldWithPreviousAndNewValue(field, newValue);

        UpdatedFields[field.Field.Id] = updated;

        field.Value = newValue;

        return true;
    }

    private string? GenerateDefaultValue(FieldWithValue field)
    {
        return field.Field.BoundTo switch
        {
            FieldBoundTo.Character => generator.CreateDefaultValue(Character, field),
            FieldBoundTo.Claim => generator.CreateDefaultValue(Claim, field),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    protected abstract void SetCharacterNameFromPlayer();

    private void GenerateDefaultValues(Dictionary<int, FieldWithValue> fields)
    {
        foreach (var field in fields.Values.Where(
            f => !f.HasEditableValue && f.Field.CanHaveValue &&
                 f.Field.IsAvailableForTarget(Character, ProjectInfo)))
        {
            var newValue = GenerateDefaultValue(field);

            var normalizedValue = field.NormalizeValueBeforeAssign(newValue);

            _ = AssignFieldValue(field, normalizedValue);
        }
    }

    private void AssignValues(FieldLayerContainer? fieldsToSet, Dictionary<int, FieldWithValue> fields)
    {
        if (fieldsToSet is null)
        {
            return;
        }

        foreach (var (fieldId, valueToSet) in fieldsToSet.LayerData)
        {
            var field = fields[fieldId.ProjectFieldId];

            if (!field.Field.CanHaveValue)
            {
                throw new FieldCannotHaveValueException(field.Field.Name);
            }

            EnsureEditAccess(field);

            var normalizedValue = field.NormalizeValueBeforeAssign(valueToSet.Value);

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

    /// <param name="fieldsToSet">
    /// Поля, которые надо изменить. Это дельта, а не полный слой: поля, которых в нём нет,
    /// сохраняют текущее значение. <c>null</c> — не менять ничего (тогда сохранение сводится
    /// к генерации значений по умолчанию и пересчёту спецгрупп).
    /// </param>
    public IReadOnlyCollection<FieldWithPreviousAndNewValue> PerformSave(FieldLayerContainer? fieldsToSet)
    {
        var fields = CharacterFieldLayers.GetAllFieldsForEdit().ToDictionary(f => f.Field.Id.ProjectFieldId);

        AssignValues(fieldsToSet, fields);

        GenerateDefaultValues(fields);

        Save(fields);
        return UpdatedFields.Values;
    }
}
