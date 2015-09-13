using System;
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

    public static GameObjectLinkViewModel AsObjectLink(this IWorldObject c)
    {
      //TODO ugly hack
      return new GameObjectLinkViewModel()
      {
        DisplayName = c.Name,
        Identification = c.Id.ToString(),
        ProjectId = c.ProjectId,
        Type = c.GetType().IsSubclassOf(typeof(CharacterGroup)) ? GameObjectLinkType.CharacterGroup : GameObjectLinkType.Character
      };
    }

    public static GameObjectLinkViewModel AsObjectLink(this ISearchResult result)
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
        case SearchResultType.ResultCharacterGroup:
          return GameObjectLinkType.CharacterGroup;
        case SearchResultType.ResultCharacter:
          return GameObjectLinkType.Character;
        default:
          throw new ArgumentOutOfRangeException(nameof(type), type, null);
      }
    }
  }
}
