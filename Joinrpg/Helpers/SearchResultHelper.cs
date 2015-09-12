using System;
using JetBrains.Annotations;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.Web.Helpers
{
  public class RouteTarget
  {
    public RouteTarget([AspMvcAction] string action, [AspMvcController] string controller, object @params)
    {
      Action = action;
      Controller = controller;
      Params = @params;
    }

    public string Action { get; }
    public string Controller { get; }
    public object Params { get; }
  }
  public static class SearchResultHelper
  {
    public static RouteTarget GetRouteTarget(this ISearchResult result)
    {
      switch (result.Type)
      {
        case SearchResultType.ResultUser:
          return new RouteTarget("Details", "User", new { UserId = result.Identification });
        default:
          throw new ArgumentOutOfRangeException();
      }
    }
  }
}
