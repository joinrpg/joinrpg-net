namespace JoinRpg.Services.Interfaces.Characters;

public interface ICharacterService
{
    Task<CharacterIdentification> AddCharacter(AddCharacterRequest addCharacterRequest);

    Task DeleteCharacter(DeleteCharacterRequest deleteCharacterRequest);

    Task EditCharacter(EditCharacterRequest editCharacterRequest);

    Task MoveCharacter(int currentUserId,
        int projectId,
        int characterId,
        int parentCharacterGroupId,
        short direction);

    Task SetFields(CharacterIdentification characterId, Dictionary<int, string?> requestFieldValues);
}
