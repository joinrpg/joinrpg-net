using JoinRpg.PrimitiveTypes;

namespace JoinRpg.DataModel.Extensions;
public static class CharacterExtensions
{
    public static CharacterTypeInfo ToCharacterTypeInfo(this Character character)
    {
        var slotName
            = character.CharacterType == CharacterType.Slot ? character.CharacterName : null;

        var charVisibility =
            (character.IsPublic, character.HidePlayerForCharacter) switch
            {
                (false, _) => CharacterVisibility.Private,
                (true, false) => CharacterVisibility.Public,
                (true, true) => CharacterVisibility.PlayerHidden,
            };
        return new(
            character.CharacterType,
            character.IsHot,
            character.CharacterSlotLimit,
            slotName,
            charVisibility);
    }
}
