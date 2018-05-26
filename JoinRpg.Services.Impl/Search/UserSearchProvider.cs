using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Services.Impl.Search
{
  internal class UserSearchProvider : ISearchProvider
  {
    //keep longer strings first to please Regexp
    private static readonly string[] keysForPerfectMath =
    {
      "%контакты",
      "контакты",
      "%игрок",
      "игрок",
    };

    public IUnitOfWork UnitOfWork { private get; set; }

    public async Task<IReadOnlyCollection<ISearchResult>> SearchAsync(int? currentUserId, string searchString)
    {
      bool matchByIdIsPerfect;
      int? idToFind = SearchKeywordsResolver.TryGetId(
        searchString,
        keysForPerfectMath,
        out matchByIdIsPerfect);

      var results =
        await
          UnitOfWork.GetDbSet<User>()
            .Where(user =>
              //TODO There should be magic way to do this. Experiment with Expression.Voodoo
              user.UserId == idToFind
              || user.Email.Contains(searchString)
              || user.FatherName.Contains(searchString)
              || user.BornName.Contains(searchString)
              || user.SurName.Contains(searchString)
              || user.PrefferedName.Contains(searchString)
              || (user.Extra != null && user.Extra.Nicknames != null && user.Extra.Nicknames.Contains(searchString))
            )
            .ToListAsync();

      return results.Select(user =>
      {
        bool wasfoundById = user.UserId == idToFind;
        var description = new MarkdownString(wasfoundById
          ? WorldObjectProviderBase.GetFoundByIdDescription(user.UserId)
          : "");

        return new SearchResultImpl
                {
                  LinkType = LinkType.ResultUser,
                  Name = user.GetDisplayName(),
                  Description = description,
                  Identification = user.UserId.ToString(),
                  ProjectId = null, //Users not associated with any project
                  IsPublic = true,
                  IsActive = true,
                  IsPerfectMatch = wasfoundById && matchByIdIsPerfect,
                };
      }).ToList();
    }
  }
}
