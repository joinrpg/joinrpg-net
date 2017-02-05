using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces.Search;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Helpers
{
  public class RouteTarget
  {
    public RouteTarget(
      [AspMvcAction] [NotNull] string action, 
      [AspMvcController] [NotNull] string controller,
      [NotNull] object @params,
      [NotNull] string anchor = "")
    {
      if (action == null) throw new ArgumentNullException(nameof(action));
      if (controller == null) throw new ArgumentNullException(nameof(controller));
      if (@params == null) throw new ArgumentNullException(nameof(@params));
      if (anchor == null) throw new ArgumentNullException(nameof(anchor));
      Action = action;
      Controller = controller;
      Params = @params;
      Anchor = anchor;
    }

    private string Action { get; }
    private string Controller { get; }
    private object Params { get; }
    private string Anchor { get; }

    public string GetUri(UrlHelper urlHelper)
    {
      //TODO[https]
      var uri = urlHelper.Action(Action, Controller, Params, "http");

      if (Anchor != "")
      {
        uri += "#" + Anchor;
      }
      return uri;
    }
  }

  public static class ObjectLinkHelper
  {
    public static RouteTarget GetRouteTarget([NotNull] this ILinkable link)
    {
      if (link == null) throw new ArgumentNullException(nameof(link));
      switch (link.LinkType)
      {
        case LinkType.ResultUser:
          return new RouteTarget("Details", "User", new {UserId = link.Identification});
        case LinkType.ResultCharacterGroup:
          return new RouteTarget("Index", "GameGroups", new {CharacterGroupId = link.Identification, link.ProjectId});
        case LinkType.ResultCharacter:
          return new RouteTarget("Details", "Character", new {CharacterId = link.Identification, link.ProjectId});
        case LinkType.Claim:
          return new RouteTarget("Edit", "Claim", new {link.ProjectId, ClaimId = link.Identification});
        case LinkType.Plot:
          return new RouteTarget("Edit", "Plot", new {PlotFolderId = link.Identification, link.ProjectId});
        case LinkType.Comment:
          return new RouteTarget("Edit", "Claim",
            new {link.ProjectId, ClaimId = link.Identification.BeforeSeparator('.')},
            anchor: $"comment{link.Identification.AfterSeparator('.')}");
        case LinkType.Project:
            return new RouteTarget("Details", "Game", new { link.ProjectId });
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    public static GameObjectLinkViewModel AsObjectLink([NotNull] this IWorldObject obj)
    {
      if (obj == null) throw new ArgumentNullException(nameof(obj));
      //TODO ugly hack
      return new GameObjectLinkViewModel()
      {
        DisplayName = obj.Name,
        Identification = obj.Id.ToString(),
        ProjectId = obj.ProjectId,
        LinkType = obj.GetType().IsSubclassOf(typeof(CharacterGroup)) ? LinkType.ResultCharacterGroup : LinkType.ResultCharacter,
        IsActive = obj.IsActive
      };
    }

    public static GameObjectLinkViewModel AsObjectLink(this ISearchResult result)
    {
      return new GameObjectLinkViewModel
      {
        LinkType = result.LinkType,
        Identification = result.Identification,
        DisplayName = result.Name,
        ProjectId = result.ProjectId,
        IsActive = result.IsActive
      };
    }

    public static IEnumerable<GameObjectLinkViewModel> AsObjectLinks([NotNull, ItemNotNull] this IEnumerable<IWorldObject> objects)
    {
      if (objects == null) throw new ArgumentNullException(nameof(objects));
      return objects.Select(t => t.AsObjectLink());
    }
  }
}
