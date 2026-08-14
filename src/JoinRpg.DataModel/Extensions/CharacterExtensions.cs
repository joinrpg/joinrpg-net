using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.DataModel.Extensions;

public static class CharacterExtensions
{
    public static CharacterTypeInfo ToCharacterTypeInfo(this Character character)
    {
        try
        {
            return CharacterTypeInfo.Create(
                character.CharacterType,
                character.IsHot,
                character.CharacterSlotLimit,
                character.CharacterName,
                character.IsPublic,
                character.HidePlayerForCharacter);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"{ex.Message}, CharacterId={character.CharacterId}", ex.ParamName, ex);
        }
    }
}
