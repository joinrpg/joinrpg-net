using System;
using JoinRpg.Services.Interfaces.Search;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Helpers
{
  public static class SearchResultHelper
  {
    public static GameObjectLinkViewModel ToLinkViewModel(this ISearchResult result)
    {
      return new GameObjectLinkViewModel
      {
        Type = ConvertToLinkType(result.Type),
        Identification = result.Identification,
        DisplayName = result.Name,
        ProjectId = result.ProjectId
      };
    }

    private static GameObjectLinkType ConvertToLinkType(SearchResultType type)
    {
      switch (type)
      {
        case SearchResultType.ResultUser:
          return GameObjectLinkType.User;
        default:
          throw new ArgumentOutOfRangeException(nameof(type), type, null);
      }
    }
  }
}
