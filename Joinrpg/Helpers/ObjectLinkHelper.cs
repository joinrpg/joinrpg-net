using System;
using System.Web;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces.Search;
using JoinRpg.Web.Models;

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

    public string GetUri(HttpContext context)
    {
      return new UrlHelper(context.Request.RequestContext).Action(Action, Controller, Params, context.Request.Url.Scheme);
    }
  }

  public static class ObjectLinkHelper
  {
    public static RouteTarget GetRouteTarget(this ILinkable link)
    {
      switch (link.LinkType)
      {
        case LinkType.ResultUser:
          return new RouteTarget("Details", "User", new {UserId = link.Identification});
        case LinkType.ResultCharacterGroup:
          return new RouteTarget("Index", "GameGroups", new {CharacterGroupId = link.Identification, link.ProjectId});
        case LinkType.ResultCharacter:
          return new RouteTarget("Details", "Character", new {CharacterId = link.Identification, link.ProjectId });
        case LinkType.Claim:
          return new RouteTarget("Edit", "Claim", new {ClaimId = link.Identification, link.ProjectId});
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    public static GameObjectLinkViewModel AsObjectLink(this IWorldObject c)
    {
      //TODO ugly hack
      return new GameObjectLinkViewModel()
      {
        DisplayName = c.Name,
        Identification = c.Id.ToString(),
        ProjectId = c.ProjectId,
        LinkType = c.GetType().IsSubclassOf(typeof(CharacterGroup)) ? LinkType.ResultCharacterGroup : LinkType.ResultCharacter
      };
    }

    public static GameObjectLinkViewModel AsObjectLink(this ISearchResult result)
    {
      return new GameObjectLinkViewModel
      {
        LinkType = result.LinkType,
        Identification = result.Identification,
        DisplayName = result.Name,
        ProjectId = result.ProjectId
      };
    }
  }
}
