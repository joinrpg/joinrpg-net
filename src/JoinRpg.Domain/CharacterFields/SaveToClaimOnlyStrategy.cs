using JoinRpg.DataModel;

namespace JoinRpg.Domain.CharacterFields;

internal class SaveToClaimOnlyStrategy : FieldSaveStrategyBase
{
    protected new Claim Claim => base.Claim!; //Claim should always exists

    public SaveToClaimOnlyStrategy(Claim claim,
        int currentUserId,
        IFieldDefaultValueGenerator generator,
        PrimitiveTypes.ProjectMetadata.ProjectInfo projectInfo) : base(claim,
        character: null,
        currentUserId,
        generator,
        projectInfo)
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

    public override IReadOnlyCollection<FieldWithValue> GetFields() => Claim.GetFields();
}
