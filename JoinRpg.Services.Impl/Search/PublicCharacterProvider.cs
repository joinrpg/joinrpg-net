using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Dal.Impl;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search
{
  internal class PublicCharacterProvider : ISearchProvider
  {
    public IUnitOfWork UnitOfWork
    { private get; set; }

    public async Task<IReadOnlyCollection<ISearchResult>> SearchAsync(string searchString)
    {
      var results =
        await
          UnitOfWork.GetDbSet<Character>()
            .Where(cg =>
              cg.CharacterName.Contains(searchString) && cg.IsPublic && cg.IsActive
            )
            .ToListAsync();

      return results.Select(@group => SearchResultImpl.FromWorldObject(@group, LinkType.ResultCharacter)).ToList();
    }
  }
}