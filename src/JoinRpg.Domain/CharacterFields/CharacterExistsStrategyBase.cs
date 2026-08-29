using System.Diagnostics.CodeAnalysis;
using JoinRpg.Domain.Access;
using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.CharacterFields;

internal abstract class CharacterExistsStrategyBase(Claim? claim, Character character, UserIdentification currentUserId, IFieldDefaultValueGenerator generator, ProjectInfo projectInfo)
    : FieldSaveStrategyBase(claim, character, currentUserId, generator, projectInfo,
        new CharacterFieldLayers(
            ClaimLayer: FieldLayerContainer.DeserializeFieldLayer(projectInfo, claim?.JsonData),
            CharacterLayer: FieldLayerContainer.DeserializeFieldLayer(projectInfo, character.JsonData),
            AccessArgumentsFactory.Create(character, currentUserId, projectInfo)))
{
    protected new Character Character => base.Character!; //Character should always exists

    /// <summary>
    /// Спецгруппы проставляются по значениям полей, обычные — остаются как были.
    /// </summary>
    private IReadOnlyCollection<CharacterGroupIdentification> ComputeParentGroupIds(Dictionary<int, FieldWithValue> fields)
    {
        var specialGroupIds = fields.Values.SelectMany(v => v.GetSpecialGroupsToApply());
        var regularGroupIds = Character.GetDirectGroups(ProjectInfo).Where(g => !g.IsSpecial).Select(g => g.Id);

        return [.. regularGroupIds.Union(specialGroupIds)];
    }

    /// <summary>
    /// Имя и описание персонажа берутся из «специальных» полей проекта, если те настроены.
    /// </summary>
    private (string Name, MarkdownDbValue? Description) ComputeNameAndDescription(Dictionary<int, FieldWithValue> fields)
    {
        var description = ProjectInfo.CharacterDescriptionField is ProjectFieldInfo descField
            ? new MarkdownDbValue(GetFieldValue(descField))
            : null;

        string name;
        if (ProjectInfo.CharacterNameField is not ProjectFieldInfo nameField)
        {
            name = CharacterNameFromPlayer();
        }
        else
        {
            var fromField = GetFieldValue(nameField);

            name = string.IsNullOrWhiteSpace(fromField)
                ? "CHAR" + Character.CharacterId
                : fromField;
        }

        return (name, description);

        string? GetFieldValue(ProjectFieldInfo field) => fields[field.Id.ProjectFieldId].Value;
    }

    /// <summary>Имя персонажа, когда в проекте не настроено поле-имя.</summary>
    protected abstract string CharacterNameFromPlayer();

    protected override (CharacterUpdate? Character, FieldLayerContainer? ClaimFields) BuildResult(
        Dictionary<int, FieldWithValue> fields)
    {
        var (name, description) = ComputeNameAndDescription(fields);

        var characterUpdate = new CharacterUpdate(
            Layer(fields.Values.Where(v => v.Field.BoundTo == FieldBoundTo.Character)),
            name,
            description,
            ComputeParentGroupIds(fields));

        return (characterUpdate, BuildClaimFields(fields));
    }

    /// <summary>Слой полей заявки; <c>null</c>, если заявки нет.</summary>
    protected abstract FieldLayerContainer? BuildClaimFields(Dictionary<int, FieldWithValue> fields);

    [DoesNotReturn]
    protected override void ThrowRequiredField(FieldWithValue field) => throw new CharacterFieldRequiredException(field.Field.Name, field.Field.Id, Character.GetId());
}
