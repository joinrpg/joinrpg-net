using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search
{
  internal class UserSearchProvider : ISearchProvider
  {
    public IUnitOfWork UnitOfWork { private get; set; }

    public async Task<IReadOnlyCollection<ISearchResult>> SearchAsync(int? currentUserId, string searchString)
    {
      var results =
        await
          UnitOfWork.GetDbSet<User>()
            .Where(user =>
              //TODO There should be magic way to do this. Experiment with Expression.Voodoo
              user.Email.Contains(searchString)
              || user.FatherName.Contains(searchString)
              || user.BornName.Contains(searchString)
              || user.SurName.Contains(searchString)
              || user.PrefferedName.Contains(searchString)
              || (user.Extra != null && user.Extra.Nicknames != null && user.Extra.Nicknames.Contains(searchString))
            )
            .ToListAsync();

      return results.Select(user => new SearchResultImpl
      {
        LinkType = LinkType.ResultUser,
        Name = user.DisplayName,
        Description = "",
        Identification = user.UserId.ToString(),
        ProjectId = null, //Users not associated with any project
        IsPublic = true,
        IsActive = true
      }).ToList();
    }
  }
}