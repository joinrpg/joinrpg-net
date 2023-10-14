using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Domain.CharacterFields;

/// <summary>
/// Saves fields either to character or to claim
/// </summary>
public class FieldSaveHelper
{
    private readonly IFieldDefaultValueGenerator generator;
    private readonly ILogger<FieldSaveHelper> logger;

    public FieldSaveHelper(IFieldDefaultValueGenerator generator, ILogger<FieldSaveHelper> logger)
    {
        this.generator = generator;
        this.logger = logger;
    }

    /// <summary>
    /// Saves character fields
    /// </summary>
    /// <returns>Fields that have changed.</returns>
    public IReadOnlyCollection<FieldWithPreviousAndNewValue> SaveCharacterFields(
        int currentUserId,
        Claim claim,
        IReadOnlyDictionary<int, string?> newFieldValue,
        ProjectInfo projectInfo)
    {
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentNullException.ThrowIfNull(newFieldValue);
        ArgumentNullException.ThrowIfNull(projectInfo);

        return SaveCharacterFieldsImpl(currentUserId,
            claim.Character,
            claim,
            newFieldValue,
            projectInfo);
    }

    /// <summary>
    /// Saves fields of a character
    /// </summary>
    /// <returns>The list of updated fields</returns>
    public IReadOnlyCollection<FieldWithPreviousAndNewValue> SaveCharacterFields(
        int currentUserId,
        Character character,
        IReadOnlyDictionary<int, string?> newFieldValue,
        ProjectInfo projectInfo)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(newFieldValue);
        ArgumentNullException.ThrowIfNull(projectInfo);

        return SaveCharacterFieldsImpl(currentUserId,
            character,
            character.ApprovedClaim,
            newFieldValue,
            projectInfo);
    }

    private IReadOnlyCollection<FieldWithPreviousAndNewValue> SaveCharacterFieldsImpl(
        int currentUserId,
        Character? character,
        Claim? claim,
        IReadOnlyDictionary<int, string?> newFieldValue,
        ProjectInfo projectInfo)
    {
        var strategy = CreateStrategy(currentUserId, character, claim, projectInfo);

        logger.LogDebug("Selected saving strategy as {strategyName}", strategy.GetType().Name);

        var fields = strategy.GetFields().ToDictionary(f => f.Field.ProjectFieldId);

        AssignValues(newFieldValue, fields, strategy);

        GenerateDefaultValues(character, fields, strategy);

        strategy.Save(fields);
        return strategy.GetUpdatedFields();
    }

    private FieldSaveStrategyBase CreateStrategy(int currentUserId, Character? character,
        Claim? claim, ProjectInfo projectInfo)
    {
        return (claim, character) switch
        {
            (null, Character ch) => new SaveToCharacterOnlyStrategy(ch, currentUserId, generator, projectInfo),
            ({ IsApproved: true }, Character ch) => new SaveToCharacterAndClaimStrategy(claim!, ch, currentUserId, generator, projectInfo),
            ({ IsApproved: false }, _) => new SaveToClaimOnlyStrategy(claim!, currentUserId, generator, projectInfo),
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
