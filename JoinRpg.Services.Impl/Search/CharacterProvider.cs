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
    public async Task<IReadOnlyCollection<ISearchResult>> SearchAsync(int? currentUserId, string searchString)
    {
      var results =
        await
          UnitOfWork.GetDbSet<Character>()
            .Where(cg =>
              (cg.CharacterName.Contains(searchString)
               || (cg.Description.Contents != null && cg.Description.Contents.Contains(searchString)))
              && cg.IsActive
            )
            .ToListAsync();

      return GetWorldObjectsResult(currentUserId, results, LinkType.ResultCharacter);
    }
  }
}