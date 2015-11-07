using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.DataModel;
using JoinRpg.Helpers;
using JoinRpg.Web.Helpers;
using Microsoft.AspNet.Identity;

namespace JoinRpg.Web.Controllers.Common
{
  public class ControllerBase : Controller 
  {
    protected readonly ApplicationUserManager UserManager;

    protected ControllerBase(ApplicationUserManager userManager)
    {
      UserManager = userManager;
    }

    protected override void OnActionExecuting(ActionExecutingContext filterContext)
    {
      ViewBag.IsProduction = filterContext.HttpContext.Request.Url?.Host == "joinrpg.ru";
      base.OnActionExecuting(filterContext);
    }

    protected User GetCurrentUser()
    {
      return UserManager.FindById(CurrentUserId);
    }

    protected Task<User> GetCurrentUserAsync()
    {
      return UserManager.FindByIdAsync(CurrentUserId);
    }

    protected int CurrentUserId
    {
      get
      {
        var id = CurrentUserIdOrDefault;
        if (id == null)
          throw new Exception("Authorization required here");
        return id.Value;
      }
    }

    protected int? CurrentUserIdOrDefault
    {
      get
      {
        var userIdString = User.Identity.GetUserId();
        return userIdString == null ? (int?) null : int.Parse(userIdString);
      }
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        UserManager?.Dispose();
      }

      base.Dispose(disposing);
    }

    protected ActionResult RedirectToAction(RouteTarget routeTarget)
    {
      return RedirectToAction(routeTarget.Action, routeTarget.Controller, routeTarget.Params);
    }

    protected static IDictionary<int, string> GetDynamicValuesFromPost(Dictionary<string, string> post, string prefix)
    {
      return post.Keys.UnprefixNumbers(prefix)
        .ToDictionary(fieldClientId => fieldClientId, fieldClientId => post[prefix + fieldClientId]);
    }

    protected static ICollection<int> GetDynamicCheckBoxesFromPost(Dictionary<string, string> dict, string prefix)
    {
      return GetDynamicValuesFromPost(dict, prefix).Where(pair => pair.Value.Contains("true")).Select(pair => pair.Key).ToList();
    }
  }
}