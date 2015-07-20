using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Web.Models;
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
  }
}