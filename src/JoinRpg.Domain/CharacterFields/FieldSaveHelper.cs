using JoinRpg.PrimitiveTypes.Characters;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Domain.CharacterFields;

/// <summary>
/// Saves fields either to character or to claim
/// </summary>
public class FieldSaveHelper(IFieldDefaultValueGenerator generator, ILogger<FieldSaveHelper> logger)
{

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
        Character character,
        Claim? claim,
        IReadOnlyDictionary<int, string?> newFieldValue,
        ProjectInfo projectInfo)
    {
        var strategy = CreateStrategy(currentUserId, character, claim, projectInfo);

        logger.LogDebug("Selected saving strategy as {strategyName}", strategy.GetType().Name);

        var updatedFields = strategy.PerformSave(newFieldValue);

        MarkAsUsed(updatedFields, character.Project);
        return updatedFields;
    }


    private static void MarkUsed(FieldWithValue field, Project project)
    {
        var entityField = project.ProjectFields.Single(f => f.ProjectFieldId == field.Field.Id.ProjectFieldId);
        entityField.WasEverUsed = true;

        if (field.Field.HasValueList)
        {
            foreach (var val in field.GetDropdownValues())
            {
                entityField.DropdownValues.Single(v => v.ProjectFieldDropdownValueId == val.Id.ProjectFieldVariantId).WasEverUsed = true;
            }
        }
    }

    protected virtual void MarkAsUsed(IReadOnlyCollection<FieldWithPreviousAndNewValue> updatedFields, Project project)
    {
        foreach (var field in updatedFields)
        {
            MarkUsed(field, project);
        }
    }

    private FieldSaveStrategyBase CreateStrategy(int currentUserId, Character character, Claim? claim, ProjectInfo projectInfo)
    {
        return claim switch
        {
            null => new SaveToCharacterOnlyStrategy(character, currentUserId, generator, projectInfo),
            { IsApproved: true } => new SaveToCharacterAndClaimStrategy(claim, character, currentUserId, generator, projectInfo),
            { IsApproved: false } => new SaveToClaimOnlyStrategy(claim, currentUserId, generator, projectInfo),
        };
    }
}
