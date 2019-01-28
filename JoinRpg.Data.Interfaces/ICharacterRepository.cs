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
    public IReadOnlyCollection<GroupHeader> DirectGroups { get; set; }
      public IReadOnlyCollection<GroupHeader> AllGroups { get; set; }
        public string JsonData { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
  }

  public class GroupHeader : IEquatable<GroupHeader>
  {
    public bool IsActive { get; set; }
    public bool IsSpecial { get; set; }
    public int CharacterGroupId { get; set; }
    public string CharacterGroupName { get; set; }

      public IntList ParentGroupIds { get; set; }

      /// <inheritdoc />
      public bool Equals(GroupHeader other) => other != null && CharacterGroupId == other.CharacterGroupId;

      /// <inheritdoc />
      public override bool Equals(object obj) => Equals(obj as GroupHeader);

      /// <inheritdoc />
      public override int GetHashCode() => CharacterGroupId;
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

  public class ClaimWithPlayer
  {
    public int ClaimId { get; set; }
    public string CharacterName { get; set; }
    public User Player { get; set; }
    public UserExtra Extra { get; set; }
  }
}
