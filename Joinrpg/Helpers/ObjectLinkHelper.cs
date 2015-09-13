using System;
using JetBrains.Annotations;
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
  }

  public static class ObjectLinkHelper
  {
    public static RouteTarget GetRouteTarget(this GameObjectLinkViewModel link)
    {
      switch (link.Type)
      {
        case GameObjectLinkType.User:
          return new RouteTarget("Details", "User", new {UserId = link.Identification});
        case GameObjectLinkType.CharacterGroup:
          return new RouteTarget("Edit", "GameGroups", new {CharacterGroupId = link.Identification, link.ProjectId});
        case GameObjectLinkType.Character:
          return new RouteTarget("Details", "Character", new {CharacterId = link.Identification, link.ProjectId });
        default:
          throw new ArgumentOutOfRangeException();
      }
    }
  }
}
