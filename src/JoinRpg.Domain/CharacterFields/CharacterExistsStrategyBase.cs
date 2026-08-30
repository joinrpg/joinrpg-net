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
    private IReadOnlyCollection<CharacterGroupIdentification> ComputeParentGroupIds(FieldLayerContainer working)
    {
        var specialGroupIds = working.LayerData.Values.SelectMany(v => v.GetSpecialGroupsToApply());
        var regularGroupIds = Character.GetDirectGroups(ProjectInfo).Where(g => !g.IsSpecial).Select(g => g.Id);

        return [.. regularGroupIds.Union(specialGroupIds)];
    }

    /// <summary>
    /// Имя и описание персонажа берутся из «специальных» полей проекта, если те настроены.
    /// </summary>
    private (string Name, MarkdownString? Description) ComputeNameAndDescription(FieldLayerContainer working)
    {
        // Пустое описание — это MarkdownString(""), а не null: null здесь означает
        // «поле описания в проекте не настроено, не трогать».
        var description = ProjectInfo.CharacterDescriptionField is ProjectFieldInfo descField
            ? new MarkdownString(working.GetValue(descField) ?? "")
            : null;

        string name;
        if (ProjectInfo.CharacterNameField is not ProjectFieldInfo nameField)
        {
            name = CharacterNameFromPlayer();
        }
        else
        {
            var fromField = working.GetValue(nameField);

            name = string.IsNullOrWhiteSpace(fromField)
                ? "CHAR" + Character.CharacterId
                : fromField;
        }

        return (name, description);
    }

    /// <summary>Имя персонажа, когда в проекте не настроено поле-имя.</summary>
    protected abstract string CharacterNameFromPlayer();

    protected override (CharacterUpdate? Character, FieldLayerContainer? ClaimFields) BuildResult(
        FieldLayerContainer working)
    {
        var (name, description) = ComputeNameAndDescription(working);

        var characterUpdate = new CharacterUpdate(
            Layer(working.LayerData.Values.Where(v => v.Field.BoundTo == FieldBoundTo.Character)),
            name,
            description,
            ComputeParentGroupIds(working));

        return (characterUpdate, BuildClaimFields(working));
    }

    /// <summary>Слой полей заявки; <c>null</c>, если заявки нет.</summary>
    protected abstract FieldLayerContainer? BuildClaimFields(FieldLayerContainer working);

    [DoesNotReturn]
    protected override void ThrowRequiredField(FieldWithValue field) => throw new CharacterFieldRequiredException(field.Field.Name, field.Field.Id, Character.GetId());
}
