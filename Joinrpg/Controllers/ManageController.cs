using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;

namespace JoinRpg.Web.Controllers
{
  [Authorize]
  public class ManageController : Common.ControllerBase
  {
    private readonly ApplicationSignInManager _signInManager;
    private readonly IUserRepository _userRepository;
    private readonly IUserService _userService;

    public ManageController(ApplicationUserManager userManager, ApplicationSignInManager signInManager, IUserRepository userRepository, IUserService userService)
      : base(userManager)
    {
      _signInManager = signInManager;
      _userRepository = userRepository;
      _userService = userService;
    }

    private ApplicationSignInManager SignInManager
      => _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();


    [Authorize]
    //
    // GET: /Manage/Index
    public async Task<ActionResult> Index(ManageMessageId? message)
    {
      ViewBag.StatusMessage =
        message == ManageMessageId.ChangePasswordSuccess
          ? "Ваш пароль был изменен."
          : message == ManageMessageId.SetPasswordSuccess
            ? "Ваш пароль был установлен."
            : message == ManageMessageId.Error
              ? "An error has occurred."
              : "";

      var userId = CurrentUserId;
      var model = new IndexViewModel
      {
        HasPassword = HasPassword(),
        Email = await UserManager.GetEmailAsync(userId),
        Logins = await UserManager.GetLoginsAsync(userId),
      };
      return View(model);
    }

    //
    // POST: /Manage/RemoveLogin
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> RemoveLogin(string loginProvider, string providerKey)
    {
      ManageMessageId? message;
      var result = await UserManager.RemoveLoginAsync(CurrentUserId, new UserLoginInfo(loginProvider, providerKey));
      if (result.Succeeded)
      {
        var user = await UserManager.FindByIdAsync(CurrentUserId);
        if (user != null)
        {
          await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
        }
        message = ManageMessageId.RemoveLoginSuccess;
      }
      else
      {
        message = ManageMessageId.Error;
      }
      return RedirectToAction("ManageLogins", new {Message = message});
    }

    //
    // GET: /Manage/ChangePassword
    public ActionResult ChangePassword()
    {
      return View();
    }

    //
    // POST: /Manage/ChangePassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
    {
      if (!ModelState.IsValid)
      {
        return View(model);
      }
      var result = await UserManager.ChangePasswordAsync(CurrentUserId, model.OldPassword, model.NewPassword);
      if (result.Succeeded)
      {
        var user = await UserManager.FindByIdAsync(CurrentUserId);
        if (user != null)
        {
          await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
        }
        return RedirectToAction("Index", new {Message = ManageMessageId.ChangePasswordSuccess});
      }
      AddErrors(result);
      return View(model);
    }

    //
    // GET: /Manage/SetPassword
    public ActionResult SetPassword()
    {
      return View();
    }

    //
    // POST: /Manage/SetPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> SetPassword(SetPasswordViewModel model)
    {
      if (ModelState.IsValid)
      {
        var result = await UserManager.AddPasswordAsync(CurrentUserId, model.NewPassword);
        if (result.Succeeded)
        {
          var user = await UserManager.FindByIdAsync(CurrentUserId);
          if (user != null)
          {
            await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
          }
          return RedirectToAction("Index", new {Message = ManageMessageId.SetPasswordSuccess});
        }
        AddErrors(result);
      }

      // If we got this far, something failed, redisplay form
      return View(model);
    }

    //
    // GET: /Manage/ManageLogins
    public async Task<ActionResult> ManageLogins(ManageMessageId? message)
    {
      ViewBag.StatusMessage =
        message == ManageMessageId.RemoveLoginSuccess
          ? "The external login was removed."
          : message == ManageMessageId.Error
            ? "An error has occurred."
            : "";
      var user = await UserManager.FindByIdAsync(CurrentUserId);
      if (user == null)
      {
        return View("Error");
      }
      var userLogins = await UserManager.GetLoginsAsync(CurrentUserId);
      var otherLogins =
        AuthenticationManager.GetExternalAuthenticationTypes()
          .Where(auth => userLogins.All(ul => auth.AuthenticationType != ul.LoginProvider))
          .ToList();
      ViewBag.ShowRemoveButton = user.PasswordHash != null || userLogins.Count > 1;
      return View(new ManageLoginsViewModel
      {
        CurrentLogins = userLogins,
        OtherLogins = otherLogins
      });
    }

    //
    // POST: /Manage/LinkLogin
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult LinkLogin(string provider)
    {
      // Request a redirect to the external login provider to link a login for the current user
      return new AccountController.ChallengeResult(provider, Url.Action("LinkLoginCallback", "Manage"),
        User.Identity.GetUserId());
    }

    //
    // GET: /Manage/LinkLoginCallback
    public async Task<ActionResult> LinkLoginCallback()
    {
      var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(ApiSecretsStorage.XsrfKey, User.Identity.GetUserId());
      if (loginInfo == null)
      {
        return RedirectToAction("ManageLogins", new {Message = ManageMessageId.Error});
      }
      var result = await UserManager.AddLoginAsync(CurrentUserId, loginInfo.Login);
      return result.Succeeded
        ? RedirectToAction("ManageLogins")
        : RedirectToAction("ManageLogins", new {Message = ManageMessageId.Error});
    }

    [HttpGet]
    public async Task<ActionResult> SetupProfile(string greetingMessage = null)
    {
      if (!string.IsNullOrEmpty(greetingMessage))
      {
        ViewBag.GreetingMessage = greetingMessage;
      }

      var user = await _userRepository.WithProfile(CurrentUserId);
      return View(new EditUserProfileViewModel()
      {
        SurName = user.SurName,
        UserId = user.UserId,
        FatherName = user.FatherName,
        BornName = user.BornName,
        PrefferedName = user.DisplayName,
        //Gender = user.Extra.Gender,
        //BirthDate = user.Extra.BirthDate,
        PhoneNumber = user.Extra?.PhoneNumber,
        Nicknames = user.Extra?.Nicknames,
        GroupNames = user.Extra?.GroupNames,
        Vk = user.Extra?.Vk,
        Livejournal = user.Extra?.Livejournal,
        Skype = user.Extra?.Skype
      });
    }

    [HttpPost]
    public async Task<ActionResult> SetupProfile(EditUserProfileViewModel viewModel)
    {
      try
      {
        await
          _userService.UpdateProfile(CurrentUserId, viewModel.UserId, viewModel.SurName, viewModel.FatherName,
            viewModel.BornName, viewModel.PrefferedName, viewModel.Gender, viewModel.PhoneNumber, viewModel.Nicknames,
            viewModel.GroupNames, viewModel.Skype, viewModel.Vk, viewModel.Livejournal);
        return RedirectToAction("SetupProfile");
      }
      catch (Exception e)
      {
        ModelState.AddModelError("", $"Ошибка при сохранении: {e.Message}");
        return View(viewModel);
      }
    }

    #region Helpers

    // Used for XSRF protection when adding external logins
    private const string XsrfKey = "XsrfId";

    private IAuthenticationManager AuthenticationManager => HttpContext.GetOwinContext().Authentication;

    private void AddErrors(IdentityResult result)
    {
      foreach (var error in result.Errors)
      {
        ModelState.AddModelError("", error);
      }
    }

    private bool HasPassword()
    {
      var user = UserManager.FindById(CurrentUserId);
      return user?.PasswordHash != null;
    }

    public enum ManageMessageId
    {
      ChangePasswordSuccess,
      SetPasswordSuccess,
      RemoveLoginSuccess,
      Error
    }

    #endregion
  }

}