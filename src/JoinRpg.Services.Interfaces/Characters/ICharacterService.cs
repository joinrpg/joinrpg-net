namespace JoinRpg.Services.Interfaces.Characters;

public interface ICharacterService
{
    Task AddCharacter(AddCharacterRequest addCharacterRequest);

    Task DeleteCharacter(DeleteCharacterRequest deleteCharacterRequest);

    Task EditCharacter(EditCharacterRequest editCharacterRequest);

    Task MoveCharacter(int currentUserId,
        int projectId,
        int characterId,
        int parentCharacterGroupId,
        short direction);

    Task SetFields(int projectId, int characterId, Dictionary<int, string?> requestFieldValues);
    Task<int> CreateSlotFromGroup(int projectId, int characterGroupId, string slotName);
}
