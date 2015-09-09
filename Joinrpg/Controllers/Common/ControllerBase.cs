using System;
using System.Collections.Generic;
using System.Web.Mvc;
using JoinRpg.DataModel;
using Microsoft.AspNet.Identity;

namespace JoinRpg.Web.Controllers
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

    protected int CurrentUserId
    {
      get
      {
        var userIdString = User.Identity.GetUserId();
        if (userIdString == null)
          throw new Exception("Authorization required here");
        return int.Parse(userIdString);
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

    protected IEnumerable<T> LoadForCurrentUser<T>(Func<int, IEnumerable<T>> loadFunc)
    {
      return User.Identity.IsAuthenticated
        ? loadFunc(CurrentUserId)
        : new T[] {};
    }
  }
}