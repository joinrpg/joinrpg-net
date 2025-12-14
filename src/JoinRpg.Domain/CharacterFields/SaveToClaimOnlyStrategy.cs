using System.Diagnostics.CodeAnalysis;
using JoinRpg.Domain.Access;
using JoinRpg.PrimitiveTypes.Characters;

namespace JoinRpg.Domain.CharacterFields;

internal class SaveToClaimOnlyStrategy : FieldSaveStrategyBase
{
    protected new Claim Claim => base.Claim!; //Claim should always exists

    public SaveToClaimOnlyStrategy(Claim claim,
        int currentUserId,
        IFieldDefaultValueGenerator generator,
        ProjectInfo projectInfo) : base(claim,
        character: null,
        currentUserId,
        generator,
        projectInfo,
        AccessArgumentsFactory.Create(claim, currentUserId))
    {
    }

    protected override void SerializeFields(Dictionary<int, FieldWithValue> fields)
    {
        //TODO do not save fields that have values same as character's
        Claim.JsonData = fields.Values.SerializeFields();
    }

    protected override void SetCharacterNameFromPlayer()
    {
        //Do nothing player could not change character yet
    }

    protected override IReadOnlyCollection<FieldWithValue> GetFields() => Claim.GetFields(ProjectInfo);

    [DoesNotReturn]
    protected override void ThrowRequiredField(FieldWithValue field) => throw new CharacterFieldRequiredException(field.Field.Name, field.Field.Id, new(ProjectInfo.ProjectId, Claim.CharacterId));

    protected override bool FieldIsMandatory(FieldWithValue field) => field.Field.MandatoryStatus == MandatoryStatus.Required && field.Field.IsAvailableForTarget(Character);
}
