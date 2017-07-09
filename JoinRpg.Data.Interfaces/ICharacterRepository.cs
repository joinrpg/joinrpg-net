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
  }
}
