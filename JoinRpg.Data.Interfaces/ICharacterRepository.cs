using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces
{
  public interface ICharacterRepository : IDisposable
  {
    [ItemNotNull]
    Task<IReadOnlyCollection<int>> GetCharacterIds(int projectId, DateTime? modifiedSince);
    [ItemNotNull]
    Task<IReadOnlyCollection<Character>> GetCharacters(int projectId, [NotNull] IReadOnlyCollection<int> characterIds);

    Task<Character> GetCharacterAsync(int projectId, int characterId);
    Task<Character> GetCharacterWithGroups(int projectId, int characterId);
    Task<Character> GetCharacterWithDetails(int projectId, int characterId);
  }
}
