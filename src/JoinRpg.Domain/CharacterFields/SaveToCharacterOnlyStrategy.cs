using JoinRpg.PrimitiveTypes.Characters;

namespace JoinRpg.Domain.CharacterFields;

internal class SaveToCharacterOnlyStrategy(
    Character character,
    int currentUserId,
    IFieldDefaultValueGenerator generator,
    ProjectInfo projectInfo) : CharacterExistsStrategyBase(claim: null,
    character,
    currentUserId,
    generator,
    projectInfo)
{
    public override void Save(Dictionary<int, FieldWithValue> fields)
    {
        Character.JsonData = fields.Values
            .Where(v => v.Field.BoundTo == FieldBoundTo.Character).SerializeFields();
        SetCharacterDescription(fields);

        UpdateSpecialGroups(fields);
    }

    protected override IReadOnlyCollection<FieldWithValue> GetFields() => Character.GetFields(ProjectInfo);

    protected override void SetCharacterNameFromPlayer()
    {
        //TODO: we don't have player yet, but have to set player name from it.
        //M.b. Disallow create characters in this scenarios?
        Character.CharacterName = Character.CharacterName ?? "PLAYER_NAME";
    }

    protected override void SerializeFields(Dictionary<int, FieldWithValue> fields)
    {
        Character.JsonData = fields
            .Values
            .Where(v => v.Field.BoundTo == FieldBoundTo.Character)
            .SerializeFields();
    }

    protected override bool FieldIsMandatory(FieldWithValue field) =>
        field.Field.MandatoryStatus == MandatoryStatus.Required
        && field.Field.BoundTo == FieldBoundTo.Character // Игнорируем пустые поля заявок в данном случае
        && field.Field.IsAvailableForTarget(Character);
}
