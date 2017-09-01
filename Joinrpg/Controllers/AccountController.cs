using System;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;

namespace JoinRpg.Web.Controllers
{
  [Authorize]
  public class AccountController : Common.ControllerBase
  {
    private readonly IEmailService _emailService;

    public AccountController(ApplicationUserManager userManager,
      ApplicationSignInManager signInManager, IEmailService emailService) : base(userManager)
    {
      SignInManager = signInManager;
      _emailService = emailService;
    }

    private ApplicationSignInManager SignInManager { get; }

    //
    // GET: /Account/Login
    [AllowAnonymous]
    public ActionResult Login(string returnUrl)
    {
      ViewBag.ReturnUrl = returnUrl;
      return View();
    }

    //
    // POST: /Account/Login
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
    {
      if (!ModelState.IsValid)
      {
        return View(model);
      }

      // Require the user to have a confirmed email before they can log on.
      var user = await UserManager.FindByNameAsync(model.Email);

      if (user != null)
      {
        if (!await UserManager.IsEmailConfirmedAsync(user.UserId))
        {
          await SendConfirmationEmail(user);
          return View("EmailUnconfirmed");
        }
      }

      // This doesn't count login failures towards account lockout
      // To enable password failures to trigger account lockout, change to shouldLockout: true
      var result =
        await SignInManager.PasswordSignInAsync(model.Email, model.Password, isPersistent: true, shouldLockout: false);

      switch (result)
      {
        case SignInStatus.Success:
          return RedirectToLocal(returnUrl);
        case SignInStatus.LockedOut:
          return View("Lockout");
        default:
          ModelState.AddModelError("", "Не найден логин или пароль");
          return View(model);
      }
    }

    //
    // GET: /Account/Register
    [AllowAnonymous]
    public ActionResult Register()
    {
      return View();
    }

    //
    // POST: /Account/Register
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Register(RegisterViewModel model)
    {
      if (ModelState.IsValid)
      {
        var currentUser = await UserManager.FindByNameAsync(model.Email);

        if (currentUser != null)
        {
          ModelState.AddModelError("", "Вы уже зарегистрировались. Если пароль не подходит, нажмите «Забыли пароль?»");
          return View("Login", new LoginViewModel() {Email = model.Email});
        }

        var user = new User {UserName = model.Email, Email = model.Email};
        var result = await UserManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
          //We don't want to sign in user until he has email confirmed 
          //await SignInManager.SignInAsync(user, isPersistent:false, rememberBrowser:false);

          await SendConfirmationEmail(user);

          return View("RegisterSuccess");
        }
        AddErrors(result);
      }

      // If we got this far, something failed, redisplay form
      return View(model);
    }

    private async Task SendConfirmationEmail(User user)
    {
      string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.UserId);
      var callbackUrl = Url.Action("ConfirmEmail", "Account", new {userId = user.UserId, code},
        protocol: Request.Url.Scheme);

      await _emailService.Email(new ConfirmEmail() {CallbackUrl = callbackUrl, Recipient = user});
    }

    //
    // GET: /Account/ConfirmEmail
    [AllowAnonymous]
    public async Task<ActionResult> ConfirmEmail(int? userId, string code)
    {
      if (userId == null || code == null)
      {
        return View("Error");
      }
      var result = await UserManager.ConfirmEmailAsync((int) userId, code);
      if (result.Succeeded)
      {
        return View("ConfirmEmail");
      }
      else
      {
        return View("Error");
      }
    }

    //
    // GET: /Account/ForgotPassword
    [AllowAnonymous]
    public ActionResult ForgotPassword()
    {
      return View();
    }

    //
    // POST: /Account/ForgotPassword
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
      if (ModelState.IsValid)
      {
        var user = await UserManager.FindByNameAsync(model.Email);
        if (user == null /*|| !(await UserManager.IsEmailConfirmedAsync(user.Id))*/)
        {
          // Don't reveal that the user does not exist or is not confirmed
          return View("ForgotPasswordConfirmation");
        }

        // Send an email with this link
        string code = await UserManager.GeneratePasswordResetTokenAsync(user.UserId);
        var callbackUrl = Url.Action("ResetPassword", "Account", new {userId = user.UserId, code = code},
          protocol: Request.Url.Scheme);

        await _emailService.Email(new RemindPasswordEmail() {CallbackUrl = callbackUrl, Recipient = user});

        return RedirectToAction("ForgotPasswordConfirmation", "Account");
      }

      // If we got this far, something failed, redisplay form
      return View(model);
    }

    //
    // GET: /Account/ForgotPasswordConfirmation
    [AllowAnonymous]
    public ActionResult ForgotPasswordConfirmation()
    {
      return View();
    }

    //
    // GET: /Account/ResetPassword
    [AllowAnonymous]
    public ActionResult ResetPassword(string code)
    {
      return code == null ? View("Error") : View();
    }

    //
    // POST: /Account/ResetPassword
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
    {
      if (!ModelState.IsValid)
      {
        return View(model);
      }
      var user = await UserManager.FindByNameAsync(model.Email);
      if (user == null)
      {
        ModelState.AddModelError("", "Email не найден");
        return View();
      }
      var result = await UserManager.ResetPasswordAsync(user.UserId, model.Code, model.Password);
      if (result.Succeeded)
      {
        return RedirectToAction("ResetPasswordConfirmation", "Account");
      }
      ModelState.AddModelError("", "Что-то пошло не так, скорее всего ссылка истекла. Попробуйте запросить ее снова, если не удастся — напишите в техподдержку");
      AddErrors(result);
      return View();
    }

    //
    // GET: /Account/ResetPasswordConfirmation
    [AllowAnonymous]
    public ActionResult ResetPasswordConfirmation()
    {
      return View();
    }

    //
    // POST: /Account/ExternalLogin
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public ActionResult ExternalLogin(string provider, string returnUrl)
    {
      // Request a redirect to the external login provider
      return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new {ReturnUrl = returnUrl}));
    }

    //
    // GET: /Account/ExternalLoginCallback
    [AllowAnonymous]
    public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
    {
      var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
      if (loginInfo == null)
      {
        return RedirectToAction("Login");
      }

      // Sign in the user with this external login provider if the user already has a login
      var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: true);

      // If sign in failed, may be we have user with same email. Let's bind.
      if (result == SignInStatus.Failure && !string.IsNullOrWhiteSpace(loginInfo.Email))
      {
        var user = await UserManager.FindByEmailAsync(loginInfo.Email);
        if (user != null)
        {
          await UserManager.AddLoginAsync(user.UserId, loginInfo.Login);
          result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: true);
        }
      }

      switch (result)
      {
        case SignInStatus.Success:
          return RedirectToLocal(returnUrl);
        case SignInStatus.LockedOut:
          return View("Lockout");
        case SignInStatus.RequiresVerification:
          //return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
          throw new NotImplementedException();
        case SignInStatus.Failure:
        default:
          // If the user does not have an account, then prompt the user to create an account
          ViewBag.ReturnUrl = returnUrl;
          ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
          return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel {Email = loginInfo.Email});
      }
    }

    //
    // POST: /Account/ExternalLoginConfirmation
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
    {
      if (User.Identity.IsAuthenticated)
      {
        return RedirectToAction("Index", "Manage");
      }

      if (ModelState.IsValid)
      {
        // Get the information about the user from the external login provider
        var info = await AuthenticationManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
          return View("ExternalLoginFailure");
        }
        var user = new User {Email = model.Email, UserName = model.Email};
        var result = await UserManager.CreateAsync(user);
        if (result.Succeeded)
        {
          result = await UserManager.AddLoginAsync(user.UserId, info.Login);
          if (result.Succeeded)
          {
            await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            return RedirectToLocal(returnUrl);
          }
        }
        AddErrors(result);
      }

      ViewBag.ReturnUrl = returnUrl;
      return View(model);
    }

    //
    // POST: /Account/LogOff
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult LogOff()
    {
      AuthenticationManager.SignOut();
      return RedirectToAction("Index", "Home");
    }

    //
    // GET: /Account/ExternalLoginFailure
    [AllowAnonymous]
    public ActionResult ExternalLoginFailure()
    {
      return View();
    }

    #region Helpers
    private IAuthenticationManager AuthenticationManager => HttpContext.GetOwinContext().Authentication;

    private void AddErrors(IdentityResult result)
    {
      foreach (var error in result.Errors)
      {
        ModelState.AddModelError("", error);
      }
    }

    private ActionResult RedirectToLocal(string returnUrl)
    {
      if (Url.IsLocalUrl(returnUrl))
      {
        return Redirect(returnUrl);
      }
      return RedirectToAction("Index", "Home");
    }

    internal class ChallengeResult : HttpUnauthorizedResult
    {
      public ChallengeResult(string provider, string redirectUri, string userId = null)
      {
        LoginProvider = provider;
        RedirectUri = redirectUri;
        UserId = userId;
      }

      public string LoginProvider { get; set; }
      public string RedirectUri { get; set; }
      public string UserId { get; set; }

      public override void ExecuteResult(ControllerContext context)
      {
        var properties = new AuthenticationProperties {RedirectUri = RedirectUri};
        if (UserId != null)
        {
          properties.Dictionary[ApiSecretsStorage.XsrfKey] = UserId;
        }
        context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
      }
    }

    #endregion
  }
}