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

    public override void Save(Dictionary<int, FieldWithValue> fields)
    {
        Character.JsonData = fields.Values
            .Where(v => v.Field.FieldBoundTo == FieldBoundTo.Character).SerializeFields();

        Claim.JsonData = fields.Values
            .Where(v => v.Field.FieldBoundTo == FieldBoundTo.Claim).SerializeFields();

        SetCharacterDescription(fields);

        UpdateSpecialGroups(fields);
    }

    protected override void SetCharacterNameFromPlayer() => Character.CharacterName = Claim.Player.GetDisplayName();
}
