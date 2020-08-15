using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BitArmory.ReCaptcha;
using Joinrpg.Web.Identity;
using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Identity;
using JoinRpg.Portal.Infrastructure.Authentication;
using JoinRpg.Services.Interfaces.Notification;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Controllers
{
    [Authorize]
    public class AccountController : Common.ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly IOptions<RecaptchaOptions> recaptchaOptions;
        private readonly IRecaptchaVerificator recaptchaVerificator;

        private ApplicationUserManager UserManager { get; }
        private ApplicationSignInManager SignInManager { get; }
        private IUserRepository UserRepository { get; }


        public AccountController(
            ApplicationUserManager userManager,
            ApplicationSignInManager signInManager,
            IEmailService emailService,
            IUserRepository userRepository,
            IOptions<RecaptchaOptions> recaptchaOptions,
            IRecaptchaVerificator recaptchaVerificator
        )
        {
            UserManager = userManager;
            SignInManager = signInManager;
            _emailService = emailService;
            UserRepository = userRepository;
            this.recaptchaOptions = recaptchaOptions;
            this.recaptchaVerificator = recaptchaVerificator;
        }


        [AllowAnonymous]
        public async Task<ActionResult> Login(string returnUrl) => View(await CreateLoginPageViewModelAsync(returnUrl));

        private async Task<LoginPageViewModel> CreateLoginPageViewModelAsync(string returnUrl)
        {
            return new LoginPageViewModel()
            {
                External = new ExternalLoginListViewModel
                {
                    ReturnUrl = returnUrl,
                    ExternalLogins = (await SignInManager.GetExternalAuthenticationSchemesAsync())
                        .Select(exLogin => new AuthenticationDescriptionViewModel
                        {
                            AuthenticationType = exLogin.Name,
                            Caption = exLogin.DisplayName,
                        }).ToList(),
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
                var vm1 = await CreateLoginPageViewModelAsync(returnUrl);
                vm1.Login.Email = model.Email;
                vm1.Login.ReturnUrl = returnUrl;
                return View(vm1);
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
            var vm = await CreateLoginPageViewModelAsync(returnUrl);
            vm.Login.Email = model.Email;
            return View(vm);
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            var isRecaptchaConfigured = recaptchaVerificator.IsRecaptchaConfigured();
            return View
                (
                    new RegisterViewModel()
                    {
                        IsRecaptchaConfigured = isRecaptchaConfigured,
                        RecaptchaPublicKey = recaptchaOptions.Value.PublicKey,
                    }
                );
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> Register(RegisterViewModel model, [FromForm(Name = "g-recaptcha-response")] string recaptchaToken)
        {
            var isRecaptchaConfigured = recaptchaVerificator.IsRecaptchaConfigured();

            if (!ModelState.IsValid)
            {
                model.IsRecaptchaConfigured = isRecaptchaConfigured;
                model.RecaptchaPublicKey = recaptchaOptions.Value.PublicKey;
                return View(model);
            }

            //this can be null i.e. under proxy or from localhost.
            //TODO IISIntegration, etc
            var clientIp = HttpContext.Connection.RemoteIpAddress;

            if (isRecaptchaConfigured)
            {
                var isRecaptchaValid = await recaptchaVerificator.ValidateToken(recaptchaToken, clientIp);

                if (!isRecaptchaValid)
                {
                    ModelState.AddModelError("captcha", "Невозможно верифицировать ReCAPTCHA. Если эта ошибка повторяется, пожалуйста, обратитесь в техподдержку.");
                    model.IsRecaptchaConfigured = isRecaptchaConfigured;
                    model.RecaptchaPublicKey = recaptchaOptions.Value.PublicKey;
                    return View(model);
                }
            }

            var currentUser = await UserManager.FindByNameAsync(model.Email);

            if (currentUser != null)
            {
                ModelState.AddModelError("",
                    "Вы уже зарегистрировались. Если пароль не подходит, нажмите «Забыли пароль?»");
                var loginModel = await CreateLoginPageViewModelAsync("");
                loginModel.Login.Email = model.Email;
                return View("Login", loginModel);
            }

            var user = new JoinIdentityUser { UserName = model.Email };
            var result = await UserManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                ModelState.AddErrors(result);
                model.IsRecaptchaConfigured = isRecaptchaConfigured;
                model.RecaptchaPublicKey = recaptchaOptions.Value.PublicKey;
                return View(model);
            }

            //We don't want to sign in user until he has email confirmed 
            //await SignInManager.SignInAsync(user, isPersistent:false, rememberBrowser:false);

            await SendConfirmationEmail(user);

            return View("RegisterSuccess");
        }

        private async Task SendConfirmationEmail(JoinIdentityUser user)
        {
            var code = await UserManager.GenerateEmailConfirmationTokenAsync(user);
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
        public ActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

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
                var code = await UserManager.GeneratePasswordResetTokenAsync(user);
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
        public ActionResult ForgotPasswordConfirmation() => View();

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(int userId, string code)
        {
            if (userId == 0 || code is null)
            {
                return View("Error");
            }

            return View(new ResetPasswordViewModel() { Code = code, Email = "" });
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
            return View(model);
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation() => View();

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]

        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl });

            var authenticationProperties = SignInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(authenticationProperties, provider);
            // Request a redirect to the external login provider
            //return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var auth = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);

            var loginInfo = await SignInManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            // Sign in the user with this external login provider if the user already has a login
            var result = await SignInManager.ExternalLoginSignInAsync(loginInfo.LoginProvider, loginInfo.ProviderKey, isPersistent: true);

            var email = loginInfo.Principal.FindFirstValue(ClaimTypes.Email);



            // If sign in failed, may be we have user with same email. Let's bind.
            if (!result.Succeeded && !string.IsNullOrWhiteSpace(email))
            {
                var user = await UserManager.FindByEmailAsync(email);
                if (user != null)
                {
                    await UserManager.AddLoginAsync(user, loginInfo);
                    result = await SignInManager.ExternalLoginSignInAsync(loginInfo.LoginProvider, loginInfo.ProviderKey, isPersistent: true);
                }
            }

            if (result.Succeeded)
            {
                return RedirectToLocal(returnUrl);
            }

            if (result.IsLockedOut)
            {
                return View("Lockout");
            }

            if (result.RequiresTwoFactor)
            {
                throw new NotImplementedException();
            }

            // If the user does not have an account, then prompt the user to create an account
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.LoginProvider = loginInfo.LoginProvider;
            return View("ExternalLoginConfirmation",
                new ExternalLoginConfirmationViewModel { Email = email });

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
        public ActionResult ExternalLoginFailure() => View();

        #region Helpers
        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        #endregion

        [AllowAnonymous]
        public ActionResult AccessDenied(string returnUrl) => View("AccessDenied", returnUrl);
    }
}
