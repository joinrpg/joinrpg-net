using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search
{
  internal class PublicCharacterGroupsProvider : ISearchProvider
  {
    public IUnitOfWork UnitOfWork { private get; set; }

    public async Task<IReadOnlyCollection<ISearchResult>> SearchAsync(string searchString)
    {
      var results =
        await
          UnitOfWork.GetDbSet<CharacterGroup>()
            .Where(cg =>
              cg.CharacterGroupName.Contains(searchString) && cg.IsPublic && cg.IsActive && !cg.IsRoot
            )
            .ToListAsync();

      return results.Select(@group => SearchResultImpl.FromWorldObject(@group, LinkType.ResultCharacterGroup)).ToList();
    }
  }
}