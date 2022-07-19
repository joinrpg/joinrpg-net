using JoinRpg.PrimitiveTypes;

namespace JoinRpg.DataModel.Extensions;
public static class CharacterExtensions
{
    public static CharacterTypeInfo ToCharacterTypeInfo(this Character character)
    {
        var slotName
            = character.CharacterType == CharacterType.Slot ? character.CharacterName : null;
        return new(character.CharacterType, character.IsHot, character.CharacterSlotLimit, slotName);
    }
}
