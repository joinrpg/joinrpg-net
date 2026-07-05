namespace JoinRpg.Data.Interfaces;

public interface ICharacterGroupRepository
{
    Task<CharacterGroupFullInfo?> GetCharacterGroupFullInfo(CharacterGroupIdentification id);

    Task<IReadOnlyList<CharacterGroupFullInfo>> GetCharacterGroupsFullInfo(
        IReadOnlyCollection<CharacterGroupIdentification> groupIds);
}
