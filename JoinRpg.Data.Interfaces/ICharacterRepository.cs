using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces
{
  public class CharacterHeader
  {
    public int CharacterId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
  }
  public interface ICharacterRepository : IDisposable
  {
    [ItemNotNull]
    Task<IReadOnlyCollection<CharacterHeader>> GetCharacterHeaders(int projectId, DateTime? modifiedSince);
    [ItemNotNull]
    Task<IReadOnlyCollection<Character>> GetCharacters(int projectId, [NotNull] IReadOnlyCollection<int> characterIds);

    Task<Character> GetCharacterAsync(int projectId, int characterId);
    Task<Character> GetCharacterWithGroups(int projectId, int characterId);
    Task<Character> GetCharacterWithDetails(int projectId, int characterId);
    Task<CharacterView> GetCharacterViewAsync(int projectId, int characterId);
    Task<IEnumerable<Character>> GetAvailableCharacters(int projectId);
  }

  public class CharacterView : IFieldContainter
  {
    public int CharacterId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public bool InGame { get; set; }
    public bool IsAcceptingClaims { get; set; }
    public ClaimView ApprovedClaim { get; set; }
    public IReadOnlyCollection<ClaimHeader> Claims { get; set; }
    public IReadOnlyCollection<GroupHeader> Groups { get; set; }
    public string JsonData { get; set; }
  }

  public class GroupHeader
  {
    public bool IsActive { get; set; }
    public bool IsSpecial { get; set; }
    public int CharacterGroupId { get; set; }
    public string CharacterGroupName { get; set; }
  }

  public class ClaimView : IFieldContainter
  {
    public int PlayerUserId { get; set; }
    public string JsonData { get; set; }
  }

  public class ClaimHeader
  {
    public bool IsActive { get; set; }
  }
}
