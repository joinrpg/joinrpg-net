using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers;
using JoinRpg.Web.Helpers;
using Microsoft.AspNet.Identity;

namespace JoinRpg.Web.Controllers.Common
{
  [ValidateInput(false)]
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

    protected ActionResult RedirectToAction(RouteTarget routeTarget)
    {
      return Redirect(routeTarget.GetUri(Url).ToString());
    }

    protected IDictionary<int, string> GetDynamicValuesFromPost(string prefix)
    {
      //Some other fields can be [AllowHtml] so we need to use Request.Unvalidated.Form, or validator will fail.
      var post = Request.Unvalidated.Form.ToDictionary();
      return post.Keys.UnprefixNumbers(prefix)
        .ToDictionary(fieldClientId => fieldClientId, fieldClientId => post[prefix + fieldClientId]);
    }

    private bool IsClientCached(DateTime contentModified)
    {
      string header = Request.Headers["If-Modified-Since"];

      if (header == null) return false;

      DateTime isModifiedSince;
      return DateTime.TryParse(header, out isModifiedSince) && isModifiedSince.ToUniversalTime() > contentModified;
    }
    
    protected bool CheckCache(DateTime characterTreeModifiedAt)
    {
      if (IsClientCached(characterTreeModifiedAt)) return true;
      Response.AddHeader("Last-Modified", characterTreeModifiedAt.ToString("R"));
      return false;
    }

    protected static HttpStatusCodeResult NotModified()
    {
      return new HttpStatusCodeResult(304, "Page has not been modified");
    }

    protected string GetFullyQualifiedUri([AspMvcAction] string actionName, [AspMvcController] string controllerName, object routeValues)
    {
      if (Request.Url == null)
      {
        throw new InvalidOperationException("Request.Url is unexpectedly null");
      }
      return Request.Url.Scheme + "://" + Request.Url.Host +
             (Request.Url.IsDefaultPort ? "" : $":{Request.Url.Port}") +
             Url.Action(actionName, controllerName, routeValues);
    }

    protected bool IsCurrentUserAdmin() => User.IsInRole(Security.AdminRoleName);
  }
}