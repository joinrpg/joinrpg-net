using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Domain.CharacterFields;

internal class SaveToCharacterOnlyStrategy : CharacterExistsStrategyBase
{
    public SaveToCharacterOnlyStrategy(
        Character character,
        int currentUserId,
        IFieldDefaultValueGenerator generator)
        : base(claim: null,
        character,
        currentUserId,
        generator)
    {
    }

    public override void Save(Dictionary<int, FieldWithValue> fields)
    {
        Character.JsonData = fields.Values
            .Where(v => v.Field.FieldBoundTo == FieldBoundTo.Character).SerializeFields();
        SetCharacterDescription(fields);

        UpdateSpecialGroups(fields);
    }

    protected override void SetCharacterNameFromPlayer()
    {
        //TODO: we don't have player yet, but have to set player name from it.
        //M.b. Disallow create characters in this scenarios?
        Character.CharacterName = Character.CharacterName ?? "PLAYER_NAME";
    }
}
