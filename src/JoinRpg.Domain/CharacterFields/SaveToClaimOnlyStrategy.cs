using System.Diagnostics.CodeAnalysis;
using JoinRpg.Domain.Access;
using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.CharacterFields;

internal class SaveToClaimOnlyStrategy(Claim claim,
    UserIdentification currentUserId,
    IFieldDefaultValueGenerator generator,
    ProjectInfo projectInfo) : FieldSaveStrategyBase(claim,
    character: null,
    currentUserId,
    generator,
    projectInfo,
    new CharacterFieldLayers(
        ClaimLayer: FieldLayerContainer.DeserializeFieldLayer(projectInfo, claim.JsonData),
        CharacterLayer: FieldLayerContainer.DeserializeFieldLayer(projectInfo, claim.Character.JsonData).PublicOnly(),
        AccessArgumentsFactory.Create(claim, currentUserId, projectInfo)))
{
    protected new Claim Claim => base.Claim!; //Claim should always exists

    protected override void SerializeFields(Dictionary<int, FieldWithValue> fields)
    {
        //TODO do not save fields that have values same as character's
        Claim.JsonData = fields.Values.SerializeFields();
    }

    protected override void SetCharacterNameFromPlayer()
    {
        //Do nothing player could not change character yet
    }

    [DoesNotReturn]
    protected override void ThrowRequiredField(FieldWithValue field) => throw new CharacterFieldRequiredException(field.Field.Name, field.Field.Id, new(ProjectInfo.ProjectId, Claim.CharacterId));

    protected override bool FieldIsMandatory(FieldWithValue field) => field.Field.MandatoryStatus == MandatoryStatus.Required && field.Field.IsAvailableForTarget(Character, ProjectInfo);
}
