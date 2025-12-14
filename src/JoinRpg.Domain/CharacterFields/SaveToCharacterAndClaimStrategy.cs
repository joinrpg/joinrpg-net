using JoinRpg.PrimitiveTypes.Characters;

namespace JoinRpg.Domain.CharacterFields;

internal class SaveToCharacterAndClaimStrategy(Claim claim,
    Character character,
    int currentUserId,
    IFieldDefaultValueGenerator generator,
    ProjectInfo projectInfo) : CharacterExistsStrategyBase(claim,
    character,
    currentUserId,
    generator,
    projectInfo)
{
    protected new Claim Claim => base.Claim!; //Claim should always exists

    protected override void SerializeFields(Dictionary<int, FieldWithValue> fields)
    {
        Character.JsonData = fields
            .Values
            .Where(v => v.Field.BoundTo == FieldBoundTo.Character).SerializeFields();

        Claim.JsonData = fields.Values
            .Where(v => v.Field.BoundTo == FieldBoundTo.Claim).SerializeFields();
    }

    protected override void SetCharacterNameFromPlayer() => Character.CharacterName = Claim.Player.GetDisplayName();
    protected override IReadOnlyCollection<FieldWithValue> GetFields() => Claim.GetFields(ProjectInfo);

    protected override bool FieldIsMandatory(FieldWithValue field) => field.Field.MandatoryStatus == MandatoryStatus.Required && field.Field.IsAvailableForTarget(Character);
}
