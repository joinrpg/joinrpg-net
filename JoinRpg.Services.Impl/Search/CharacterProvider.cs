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
      "персонаж"
    };

    public async Task<IReadOnlyCollection<ISearchResult>> SearchAsync(int? currentUserId, string searchString)
    {
      bool matchByIdIsPerfect;
      int? characterID = SearchKeywordsResolver.TryGetId(
        searchString,
        keysForPerfectMath,
        out matchByIdIsPerfect);

      var results =
        await
          UnitOfWork.GetDbSet<Character>()
            .Where(c =>
              (c.CharacterId == characterID
              || c.CharacterName.Contains(searchString)
              || (c.Description.Contents != null && c.Description.Contents.Contains(searchString)))
              && c.IsActive
            )
            .OrderByDescending(cg => cg.CharacterName.Contains(searchString))
            .ToListAsync();

      return GetWorldObjectsResult(
        currentUserId,
        results.Where(c => CheckMasterAccessIfMatchById(c, currentUserId, characterID)),
        LinkType.ResultCharacter,
        wo => wo.Id == characterID && matchByIdIsPerfect);
    }
  }
}