using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search
{
  internal class CharacterGroupsProvider : WorldObjectProviderBase, ISearchProvider
  { 
    public async Task<IReadOnlyCollection<ISearchResult>> SearchAsync(int? currentUserId, string searchString)
    {
      var results =
        await
          UnitOfWork.GetDbSet<CharacterGroup>()
            .Where(cg =>
              (cg.CharacterGroupName.Contains(searchString) 
                || (cg.Description.Contents != null && cg.Description.Contents.Contains(searchString)))
              && cg.IsActive && !cg.IsRoot
            )
            .OrderByDescending(cg => cg.CharacterGroupName.Contains(searchString))
            .ToListAsync();

      return GetWorldObjectsResult(currentUserId, results, LinkType.ResultCharacterGroup, wo => false);
    }
  }
}