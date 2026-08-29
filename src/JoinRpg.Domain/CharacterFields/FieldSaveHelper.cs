using JoinRpg.DomainTypes.Characters;
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
    /// <param name="fieldsToSet">
    /// Поля, которые надо изменить, — дельта, а не полный слой. Пустой слой — не менять ничего.
    /// </param>
    public IReadOnlyCollection<FieldWithPreviousAndNewValue> SaveCharacterFields(
        int currentUserId,
        Claim claim,
        FieldLayerContainer fieldsToSet,
        ProjectInfo projectInfo)
    {
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentNullException.ThrowIfNull(fieldsToSet);
        ArgumentNullException.ThrowIfNull(projectInfo);

        return SaveCharacterFieldsImpl(new UserIdentification(currentUserId),
            claim.Character,
            claim,
            fieldsToSet,
            projectInfo);
    }

    /// <summary>
    /// Saves fields of a character
    /// </summary>
    /// <returns>The list of updated fields</returns>
    /// <param name="fieldsToSet">
    /// Поля, которые надо изменить, — дельта, а не полный слой. Пустой слой — не менять ничего.
    /// </param>
    public IReadOnlyCollection<FieldWithPreviousAndNewValue> SaveCharacterFields(
        int currentUserId,
        Character character,
        FieldLayerContainer fieldsToSet,
        ProjectInfo projectInfo)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(fieldsToSet);
        ArgumentNullException.ThrowIfNull(projectInfo);

        return SaveCharacterFieldsImpl(new UserIdentification(currentUserId),
            character,
            character.ApprovedClaim,
            fieldsToSet,
            projectInfo);
    }

    private IReadOnlyCollection<FieldWithPreviousAndNewValue> SaveCharacterFieldsImpl(
        UserIdentification currentUserId,
        Character character,
        Claim? claim,
        FieldLayerContainer fieldsToSet,
        ProjectInfo projectInfo)
    {
        var strategy = CreateStrategy(currentUserId, character, claim, projectInfo);

        logger.LogDebug("Selected saving strategy as {strategyName}", strategy.GetType().Name);

        var result = strategy.PerformSave(fieldsToSet);

        Apply(result, character, claim);

        MarkAsUsed(result.UpdatedFields, character.Project);
        return result.UpdatedFields;
    }

    /// <summary>
    /// Переносит результат сохранения в EF-сущности. Единственное место, где слои полей
    /// превращаются в JSON.
    /// </summary>
    private static void Apply(FieldSaveResult result, Character character, Claim? claim)
    {
        if (result.Character is CharacterUpdate update)
        {
            character.JsonData = Serialize(update.Fields);

            if (update.Description is MarkdownDbValue description)
            {
                character.Description = description;
            }

            character.CharacterName = update.CharacterName;

            character.ParentCharacterGroupIds = [.. update.ParentGroupIds.Select(x => x.Id)];
        }

        if (result.ClaimFields is FieldLayerContainer claimFields)
        {
            // ClaimFields не бывает без заявки: его отдают только стратегии, которые в неё пишут.
            claim!.JsonData = Serialize(claimFields);
        }
    }

    private static string Serialize(FieldLayerContainer layer) => layer.LayerData.Values.SerializeFields();


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
            MarkUsed(field.New, project);
        }
    }

    private FieldSaveStrategyBase CreateStrategy(UserIdentification currentUserId, Character character, Claim? claim, ProjectInfo projectInfo)
    {
        return claim switch
        {
            null => new SaveToCharacterOnlyStrategy(character, currentUserId, generator, projectInfo),
            { IsApproved: true } => new SaveToCharacterAndClaimStrategy(claim, character, currentUserId, generator, projectInfo),
            { IsApproved: false } => new SaveToClaimOnlyStrategy(claim, currentUserId, generator, projectInfo),
        };
    }
}
