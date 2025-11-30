using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.Users;

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
    Task<IReadOnlyCollection<Character>> GetCharacters(int projectId, IReadOnlyCollection<int> characterIds);

    Task<IReadOnlyCollection<Character>> GetCharacters(IReadOnlyCollection<CharacterIdentification> characterIds);

    Task<Character> GetCharacterAsync(int projectId, int characterId);

    Task<Character> GetCharacterAsync(CharacterIdentification characterId);
    Task<Character> GetCharacterWithGroups(int projectId, int characterId);
    Task<Character> GetCharacterWithDetails(int projectId, int characterId);
    Task<CharacterView> GetCharacterViewAsync(int projectId, int characterId);
    Task<IEnumerable<Character>> GetAvailableCharacters(int projectId);
    Task<IEnumerable<Character>> GetAllCharacters(int projectId);
    Task<IEnumerable<Character>> GetActiveTemplateCharacters(int projectId);
    Task<IReadOnlyCollection<Character>> LoadCharactersWithGroups(IReadOnlyCollection<CharacterIdentification> characterIds);

    Task<IReadOnlyCollection<Character>> LoadCharactersWithGroups(ProjectIdentification projectId);
}

public class CharacterView : IFieldContainter
{
    public required int CharacterId { get; set; }
    public required DateTime UpdatedAt { get; set; }
    public required bool IsActive { get; set; }
    public required bool InGame { get; set; }
    public required CharacterTypeInfo CharacterTypeInfo { get; set; }
    public Claim? ApprovedClaim { get; set; }
    public required IReadOnlyCollection<ClaimHeader> Claims { get; set; }
    public required IReadOnlyCollection<GroupHeader> DirectGroups { get; set; }
    public required IReadOnlyCollection<GroupHeader> AllGroups { get; set; }
    public required string JsonData { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
}

public class GroupHeader : IEquatable<GroupHeader>
{
    public bool IsActive { get; set; }
    public bool IsSpecial { get; set; }
    public int CharacterGroupId { get; set; }
    public required string CharacterGroupName { get; set; }

    public required IntList ParentGroupIds { get; set; }

    /// <inheritdoc />
    public bool Equals(GroupHeader? other) => other != null && CharacterGroupId == other.CharacterGroupId;

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as GroupHeader);

    /// <inheritdoc />
    public override int GetHashCode() => CharacterGroupId;
}

public class ClaimView : IFieldContainter
{
    public int PlayerUserId { get; set; }
    public required string JsonData { get; set; }

    public bool PaidInFull { get; set; }
}

public class ClaimHeader
{
    public bool IsActive { get; set; }
}

public class ClaimWithPlayer
{
    public required ClaimIdentification ClaimId { get; set; }
    public required string CharacterName { get; set; }
    public required UserInfoHeader Player { get; set; }
    public required string? ExtraNicknames { get; set; }
    public required UserIdentification ResponsibleMasterUserId { get; set; }
}
