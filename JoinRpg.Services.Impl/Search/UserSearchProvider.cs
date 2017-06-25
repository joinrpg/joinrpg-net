using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces.Search;
using Microsoft.Practices.ObjectBuilder2;

namespace JoinRpg.Services.Impl.Search
{
  internal class UserSearchProvider : ISearchProvider
  {
    //keep longer strings first to please Regexp below
    private static readonly string[] keysForPerfectMath =
    {
      "%контакты",
      "контакты",
      "%игрок",
      "игрок"
    };

    private int? TryGetUserId(
      string searchString,
      out bool whenFoundItIsPerfectMatch)
    {
      int userId;
      // bare number in search string requires, among other, search by id. No perfect match.
      if (int.TryParse(searchString.Trim(), out userId))
      {
        whenFoundItIsPerfectMatch = false;
        return userId;
      }

      //"%контакты4196", "контакты4196", "%игрок4196", "игрок4196" provide a perfect match
      if (keysForPerfectMath.Any(k => searchString.StartsWith(k, StringComparison.CurrentCultureIgnoreCase)))
      {
        keysForPerfectMath.ForEach(k =>
          searchString = Regex.Replace(searchString, Regex.Escape(k), "", RegexOptions.IgnoreCase));

        //"%контакты 65" is not accepted. Space between keyword and number is prohibited
        if (!searchString.StartsWith(" ") && int.TryParse(searchString, out userId))
        {
          whenFoundItIsPerfectMatch = true;
          return userId;
        }
      }

      // in other cases search by Id is not needed
      whenFoundItIsPerfectMatch = false;
      return null;
    }

    public IUnitOfWork UnitOfWork { private get; set; }

    public async Task<IReadOnlyCollection<ISearchResult>> SearchAsync(int? currentUserId, string searchString)
    {
      bool matchByIdIsPerfect;
      int? idToFind = TryGetUserId(searchString, out matchByIdIsPerfect);

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

      return results.Select(user => new SearchResultImpl
      {
        LinkType = LinkType.ResultUser,
        Name = user.DisplayName,
        Description = new MarkdownString(""),
        Identification = user.UserId.ToString(),
        ProjectId = null, //Users not associated with any project
        IsPublic = true,
        IsActive = true,
        IsPerfectMatch = user.UserId == idToFind && matchByIdIsPerfect //Only some mathes by Id can be "perfect"
      }).ToList();
    }
  }
}