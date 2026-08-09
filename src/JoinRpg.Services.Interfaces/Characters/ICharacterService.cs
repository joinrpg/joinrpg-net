namespace JoinRpg.Services.Interfaces.Characters;

public interface ICharacterService
{
    Task<CharacterIdentification> AddCharacter(AddCharacterRequest addCharacterRequest);

    Task DeleteCharacter(DeleteCharacterRequest deleteCharacterRequest);

    Task EditCharacter(EditCharacterRequest editCharacterRequest);

    Task SetFields(CharacterIdentification characterId, Dictionary<int, string?> requestFieldValues);
}
