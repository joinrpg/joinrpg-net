using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces;

public class CharacterHeader
{
    public int CharacterId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}
public interface ICharacterRepository : IDisposable
{
    Task<IReadOnlyCollection<CharacterHeader>> GetCharacterHeaders(int projectId, DateTime? modifiedSince);

    Task<IReadOnlyCollection<Character>> GetCharacters(IReadOnlyCollection<CharacterIdentification> characterIds);

    [Obsolete]
    Task<Character> GetCharacterAsync(int projectId, int characterId);

    Task<Character> GetCharacterAsync(CharacterIdentification characterId);
    Task<Character> GetCharacterWithGroups(int projectId, int characterId);
    Task<Character> GetCharacterWithDetails(int projectId, int characterId);
    Task<IEnumerable<Character>> GetAllCharacters(int projectId);
    Task<IReadOnlyCollection<Character>> LoadCharactersWithGroups(IReadOnlyCollection<CharacterIdentification> characterIds);

    Task<IReadOnlyCollection<Character>> LoadCharactersWithGroups(ProjectIdentification projectId);
}

public class ClaimView : IFieldContainter
{
    public int PlayerUserId { get; set; }
    public required string JsonData { get; set; }

    public bool PaidInFull { get; set; }
}

public class ClaimWithPlayer
{
    public required ClaimIdentification ClaimId { get; set; }
    public required string CharacterName { get; set; }
    public required UserInfoHeader Player { get; set; }
    public required string? ExtraNicknames { get; set; }
    public required UserIdentification ResponsibleMasterUserId { get; set; }
    public required CharacterIdentification CharacterId { get; set; }
}
