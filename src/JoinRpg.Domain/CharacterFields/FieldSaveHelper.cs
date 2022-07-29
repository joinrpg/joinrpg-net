using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Domain.CharacterFields;

/// <summary>
/// Saves fields either to character or to claim
/// </summary>
public class FieldSaveHelper
{
    private readonly IFieldDefaultValueGenerator generator;

    public FieldSaveHelper(IFieldDefaultValueGenerator generator)
    {
        this.generator = generator;
    }

    /// <summary>
    /// Saves character fields
    /// </summary>
    /// <returns>Fields that have changed.</returns>
    public IReadOnlyCollection<FieldWithPreviousAndNewValue> SaveCharacterFields(
        int currentUserId,
        [NotNull]
        Claim claim,
        [NotNull]
        IReadOnlyDictionary<int, string?> newFieldValue
        )
    {
        if (claim == null)
        {
            throw new ArgumentNullException(nameof(claim));
        }

        return SaveCharacterFieldsImpl(currentUserId,
            claim.Character,
            claim,
            newFieldValue);
    }

    /// <summary>
    /// Saves fields of a character
    /// </summary>
    /// <returns>The list of updated fields</returns>
    public IReadOnlyCollection<FieldWithPreviousAndNewValue> SaveCharacterFields(
        int currentUserId,
        Character character,
        IReadOnlyDictionary<int, string?> newFieldValue)
    {
        if (character == null)
        {
            throw new ArgumentNullException(nameof(character));
        }

        return SaveCharacterFieldsImpl(currentUserId,
            character,
            character.ApprovedClaim,
            newFieldValue);
    }

    private IReadOnlyCollection<FieldWithPreviousAndNewValue> SaveCharacterFieldsImpl(
        int currentUserId,
        Character? character,
        Claim? claim,
        IReadOnlyDictionary<int, string?> newFieldValue)
    {
        if (newFieldValue == null)
        {
            throw new ArgumentNullException(nameof(newFieldValue));
        }

        var strategy = CreateStrategy(currentUserId, character, claim);

        var fields = strategy.LoadFields();

        AssignValues(newFieldValue, fields, strategy);

        GenerateDefaultValues(character, fields, strategy);

        strategy.Save(fields);
        return strategy.GetUpdatedFields();
    }

    private FieldSaveStrategyBase CreateStrategy(int currentUserId, Character? character,
        Claim? claim)
    {
        return (claim, character) switch
        {
            (null, Character ch) => new SaveToCharacterOnlyStrategy(ch, currentUserId, generator),
            ({ IsApproved: true }, Character ch) => new SaveToCharacterAndClaimStrategy(claim!, ch, currentUserId, generator),
            ({ IsApproved: false }, _) => new SaveToClaimOnlyStrategy(claim!, currentUserId, generator),
            _ => throw new InvalidOperationException("Either claim or character should be correct"),
        };
    }

    private static void AssignValues(IReadOnlyDictionary<int, string?> newFieldValue, Dictionary<int, FieldWithValue> fields,
        FieldSaveStrategyBase strategy)
    {
        foreach (var keyValuePair in newFieldValue)
        {
            var field = fields[keyValuePair.Key];

            strategy.EnsureEditAccess(field);

            var normalizedValue = NormalizeValueBeforeAssign(field, keyValuePair.Value);

            if (normalizedValue is null && field.Field.MandatoryStatus == MandatoryStatus.Required)
            {
                throw new FieldRequiredException(field.Field.FieldName);
            }

            _ = strategy.AssignFieldValue(field, normalizedValue);
        }
    }

    private static void GenerateDefaultValues(Character? character, Dictionary<int, FieldWithValue> fields,
        FieldSaveStrategyBase strategy)
    {
        foreach (var field in fields.Values.Where(
            f => !f.HasEditableValue && f.Field.CanHaveValue() &&
                 f.Field.IsAvailableForTarget(character)))
        {
            var newValue = strategy.GenerateDefaultValue(field);

            var normalizedValue = NormalizeValueBeforeAssign(field, newValue);

            _ = strategy.AssignFieldValue(field, normalizedValue);
        }
    }

    private static string? NormalizeValueBeforeAssign(FieldWithValue field, string? toAssign)
    {
        switch (field.Field.FieldType)
        {
            case ProjectFieldType.Checkbox:
                return toAssign?.StartsWith(FieldWithValue.CheckboxValueOn) == true
                    ? FieldWithValue.CheckboxValueOn
                    : "";
            default:
                return string.IsNullOrEmpty(toAssign) ? null : toAssign;
        }
    }
}
