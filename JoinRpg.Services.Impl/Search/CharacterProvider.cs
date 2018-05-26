using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search
{
  internal class CharacterProvider : WorldObjectProviderBase, ISearchProvider
  {
    //keep longer strings first to please Regexp
    private static readonly string[] keysForPerfectMath =
    {
      "%персонаж",
      "персонаж",
    };

    public async Task<IReadOnlyCollection<ISearchResult>> SearchAsync(int? currentUserId, string searchString)
    {
      bool matchByIdIsPerfect;
      int? characterIdToFind = SearchKeywordsResolver.TryGetId(
        searchString,
        keysForPerfectMath,
        out matchByIdIsPerfect);

      var results =
        await
          UnitOfWork.GetDbSet<Character>()
            .Where(c =>
              (c.CharacterId == characterIdToFind
              || c.CharacterName.Contains(searchString)
              || (c.Description.Contents != null && c.Description.Contents.Contains(searchString)))
              && c.IsActive
            )
            .OrderByDescending(cg => cg.CharacterName.Contains(searchString))
            .ToListAsync();
      
      //search by ID is only for masters of the character's project
      var characters = results.Where(c => 
        CheckMasterAccessIfMatchById(c, currentUserId, characterIdToFind));

      return GetWorldObjectsResult(
        currentUserId,
        characters,
        LinkType.ResultCharacter,
        wasFoundByIdPredicate: c => c.Id == characterIdToFind,
        perfectMatchPredicte: c => c.Id == characterIdToFind && matchByIdIsPerfect);
    }
  }
}