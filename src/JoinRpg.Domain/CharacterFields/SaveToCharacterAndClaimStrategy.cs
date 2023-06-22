using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Domain.CharacterFields;

internal class SaveToCharacterAndClaimStrategy : CharacterExistsStrategyBase
{
    protected new Claim Claim => base.Claim!; //Claim should always exists

    public SaveToCharacterAndClaimStrategy(Claim claim,
        Character character,
        int currentUserId,
        IFieldDefaultValueGenerator generator) : base(claim,
        character,
        currentUserId,
        generator)
    {
    }

    protected override void SerializeFields(Dictionary<int, FieldWithValue> fields)
    {
        Character.JsonData = fields
            .Values
            .Where(v => v.Field.FieldBoundTo == FieldBoundTo.Character).SerializeFields();

        Claim.JsonData = fields.Values
            .Where(v => v.Field.FieldBoundTo == FieldBoundTo.Claim).SerializeFields();
    }

    protected override void SetCharacterNameFromPlayer() => Character.CharacterName = Claim.Player.GetDisplayName();
    public override IReadOnlyCollection<FieldWithValue> GetFields() => Claim.GetFields();
}
