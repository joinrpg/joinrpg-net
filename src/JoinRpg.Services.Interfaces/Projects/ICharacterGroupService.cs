namespace JoinRpg.Services.Interfaces.Projects;

public interface ICharacterGroupService
{
    Task<CharacterGroupIdentification> AddCharacterGroup(ProjectIdentification projectId,
        string name,
        bool isPublic,
        IReadOnlyCollection<CharacterGroupIdentification> parentCharacterGroupIds,
        string description);

    Task EditCharacterGroup(CharacterGroupIdentification characterGroupId,
        string name,
        bool isPublic,
        IReadOnlyCollection<CharacterGroupIdentification> parentCharacterGroupIds,
        string description);

    Task DeleteCharacterGroup(CharacterGroupIdentification characterGroupId);

    Task MoveCharacterGroup(CharacterGroupIdentification characterGroupId,
        CharacterGroupIdentification parentCharacterGroupId,
        short direction);
}
