using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Joinrpg.Web.Identity;
using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Identity;
using JoinRpg.Services.Interfaces.Notification;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers
{
    [Authorize]
    public class AccountController : Common.ControllerBase
    {
        private readonly IEmailService _emailService;
        private ApplicationUserManager UserManager { get; }
        private ApplicationSignInManager SignInManager { get; }
        private IUserRepository UserRepository { get; }


        public AccountController(
            ApplicationUserManager userManager,
            ApplicationSignInManager signInManager,
            IEmailService emailService,
            IUserRepository userRepository
        )
        {
            UserManager = userManager;
            SignInManager = signInManager;
            _emailService = emailService;
            UserRepository = userRepository;
        }

        
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            return View(CreateLoginPageViewModel(returnUrl));
        }

        private LoginPageViewModel CreateLoginPageViewModel(string returnUrl)
        {
            return new LoginPageViewModel()
            {
                External = new ExternalLoginListViewModel
                {
                    ReturnUrl = returnUrl,
                    ExternalLogins = new List<AuthenticationDescriptionViewModel> { } // AuthenticationManager.GetExternalAuthenticationTypes().Select(ol => ol.ToViewModel()).ToList(),
                },
                Login = new LoginViewModel
                {
                    ReturnUrl = returnUrl,
                }
            };
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        
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
                if (!await UserManager.IsEmailConfirmedAsync(user))
                {
                    await SendConfirmationEmail(user);
                    return View("EmailUnconfirmed");
                }
            }

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, change to shouldLockout: true
            var result =
                await SignInManager.PasswordSignInAsync(model.Email,
                    model.Password,
                    isPersistent: true,
                    lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return RedirectToLocal(returnUrl);
            }

            if (result.IsLockedOut)
            {
                return View("Lockout");
            }

            ModelState.AddModelError("", "Не найден логин или пароль");
            var vm = CreateLoginPageViewModel(returnUrl);
            vm.Login.Email = model.Email;
            return View(vm);
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
        
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var currentUser = await UserManager.FindByNameAsync(model.Email);

            if (currentUser != null)
            {
                ModelState.AddModelError("",
                    "Вы уже зарегистрировались. Если пароль не подходит, нажмите «Забыли пароль?»");
                return View("Login", new LoginViewModel() { Email = model.Email });
            }

            var user = new JoinIdentityUser { UserName = model.Email };
            var result = await UserManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                ModelState.AddErrors(result);
                return View(model);
            }

            //We don't want to sign in user until he has email confirmed 
            //await SignInManager.SignInAsync(user, isPersistent:false, rememberBrowser:false);

            await SendConfirmationEmail(user);

            return View("RegisterSuccess");
        }

        private async Task SendConfirmationEmail(JoinIdentityUser user)
        {
            string code = await UserManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = Url.Action("ConfirmEmail",
                "Account",
                new { userId = user.UserId, code },
                protocol: Request.Scheme);

            //TODO we need to reconsider interface for email service to unbound EmailService from User objects. 
            var dbUser = await UserRepository.GetById(user.UserId);

            await _emailService.Email(new ConfirmEmail()
            { CallbackUrl = callbackUrl, Recipient = dbUser });
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
            var user = await UserManager.FindByIdAsync(userId.Value.ToString());
            var result = await UserManager.ConfirmEmailAsync(user, code);
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
                string code = await UserManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.Action("ResetPassword",
                    "Account",
                    new { userId = user.UserId, code },
                    protocol: Request.Scheme);

                //TODO we need to reconsider interface for email service to unbound EmailService from User objects. 
                var dbUser = await UserRepository.GetById(user.UserId);


                await _emailService.Email(new RemindPasswordEmail()
                { CallbackUrl = callbackUrl, Recipient = dbUser });

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

            var result =
                await UserManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }

            ModelState.AddModelError("",
                "Что-то пошло не так, скорее всего ссылка истекла. Попробуйте запросить ее снова, если не удастся — напишите в техподдержку");
            ModelState.AddErrors(result);
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
        
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult(provider,
                Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            //var loginInfo = await SignInManager.GetExternalLoginInfoAsync();
            //if (loginInfo == null)
            //{
            //    return RedirectToAction("Login");
            //}

            //// Sign in the user with this external login provider if the user already has a login
            //var result = await SignInManager.ExternalLoginSignInAsync(loginInfo, isPersistent: true);

            throw new NotImplementedException();

            //// If sign in failed, may be we have user with same email. Let's bind.
            //if (result.Success == SignInStatus.Failure && !string.IsNullOrWhiteSpace(loginInfo.Email))
            //{
            //    var user = await UserManager.FindByEmailAsync(loginInfo.Email);
            //    if (user != null)
            //    {
            //        await UserManager.AddLoginAsync(user.UserId, loginInfo.Login);
            //        result = await SignInManager.ExternalLoginSignInAsync(loginInfo, isPersistent: true);
            //    }
            //}

            //switch (result)
            //{
            //    case SignInStatus.Success:
            //        return RedirectToLocal(returnUrl);
            //    case SignInStatus.LockedOut:
            //        return View("Lockout");
            //    case SignInStatus.RequiresVerification:
            //        //return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
            //        throw new NotImplementedException();
            //    case SignInStatus.Failure:
            //    default:
            //        // If the user does not have an account, then prompt the user to create an account
            //        ViewBag.ReturnUrl = returnUrl;
            //        ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
            //        return View("ExternalLoginConfirmation",
            //            new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
            //}
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        
        public async Task<ActionResult> ExternalLoginConfirmation(
            ExternalLoginConfirmationViewModel model,
            string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await SignInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }

                var user = new JoinIdentityUser() { UserName = model.Email };
                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        await SignInManager.SignInAsync(user,
                            isPersistent: true);
                        return RedirectToLocal(returnUrl);
                    }
                }

                ModelState.AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        [HttpPost]
        
        public async Task<ActionResult> LogOff(string returnUrl)
        {
            await SignInManager.SignOutAsync();
            return RedirectToLocal(returnUrl);
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        #region Helpers
        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : UnauthorizedResult
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

            public override void ExecuteResult(ActionContext context)
            {
                throw new NotImplementedException();
                //var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                //if (UserId != null)
                //{
                //    properties.Dictionary[ApiSecretsStorage.XsrfKey] = UserId;
                //}

                //context.HttpContext.GetOwinContext().Authentication
                //    .Challenge(properties, LoginProvider);
            }
        }

        #endregion
    }
}
