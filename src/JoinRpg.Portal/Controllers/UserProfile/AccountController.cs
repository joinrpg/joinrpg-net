using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Joinrpg.Web.Identity;
using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Identity;
using JoinRpg.Portal.Infrastructure.Authentication;
using JoinRpg.Portal.Resources;
using JoinRpg.Services.Interfaces.Notification;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Controllers
{
    [Authorize]
    public class AccountController : Common.ControllerBase
    {
        private readonly IEmailService emailService;
        private readonly IOptions<RecaptchaOptions> recaptchaOptions;
        private readonly IRecaptchaVerificator recaptchaVerificator;
        private readonly IStringLocalizer<LocalizationSharedResource> localizer;
        private readonly Lazy<IProjectRepository> projectRepository;
        private readonly ExternalLoginProfileExtractor externalLoginProfileExtractor;

        private static readonly string ImpossibleToVerifyRecaptcha = typeof(AccountController).FullName + ".Register.ImpossibleToVerifyCaptcha";
        private static readonly string LoginOrPasswordNotFound = typeof(AccountController).FullName + ".Login.LoginOrPasswordNotFound";

        private ApplicationUserManager UserManager { get; }
        private ApplicationSignInManager SignInManager { get; }
        private IUserRepository UserRepository { get; }

        public AccountController(
            ApplicationUserManager userManager,
            ApplicationSignInManager signInManager,
            IEmailService emailService,
            IUserRepository userRepository,
            IOptions<RecaptchaOptions> recaptchaOptions,
            IRecaptchaVerificator recaptchaVerificator,
            ExternalLoginProfileExtractor externalLoginProfileExtractor,
            Lazy<IProjectRepository> projectRepository,
            IStringLocalizer<LocalizationSharedResource> localizer
        )
        {
            this.emailService = emailService;
            this.recaptchaVerificator = recaptchaVerificator;
            this.localizer = localizer;

            UserManager = userManager;
            SignInManager = signInManager;
            UserRepository = userRepository;
            this.recaptchaOptions = recaptchaOptions;
            this.recaptchaVerificator = recaptchaVerificator;
            this.externalLoginProfileExtractor = externalLoginProfileExtractor;
            this.projectRepository = projectRepository;
        }


        [AllowAnonymous]
        [HttpGet]
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
        [AllowAnonymous]
        [HttpPost]
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

            ModelState.AddModelError("", localizer[LoginOrPasswordNotFound]);
            var vm = await CreateLoginPageViewModelAsync(returnUrl);
            vm.Login.Email = model.Email;
            return View(vm);
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        [HttpGet]
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
        [AllowAnonymous]
        [HttpPost]
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
                var isRecaptchaValid =
                    !string.IsNullOrWhiteSpace(recaptchaToken) &&
                    await recaptchaVerificator.ValidateToken(recaptchaToken, clientIp);

                if (!isRecaptchaValid)
                {
                    ModelState.AddModelError("captcha", localizer[ImpossibleToVerifyRecaptcha]);
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

            await emailService.Email(new ConfirmEmail()
            { CallbackUrl = callbackUrl, Recipient = dbUser });
        }

        //
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        [HttpGet]
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
        [HttpGet]
        public ActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

        //
        // POST: /Account/ForgotPassword
        [AllowAnonymous]
        [HttpPost]
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


                await emailService.Email(new RemindPasswordEmail()
                { CallbackUrl = callbackUrl, Recipient = dbUser });

                return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        [HttpGet]
        public ActionResult ForgotPasswordConfirmation() => View();

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        [HttpGet]
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
        [AllowAnonymous]
        [HttpPost]

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
        [HttpGet]
        public ActionResult ResetPasswordConfirmation() => View();

        //
        // POST: /Account/ExternalLogin
        [AllowAnonymous]
        [HttpPost]

        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            var redirectUrl = Url.Action(
                "ExternalLoginCallback",
                "Account",
                new { ReturnUrl = returnUrl },
                protocol: Request.Scheme //Ensure that it will request HTTPS if required
                );

            var authenticationProperties = SignInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(authenticationProperties, provider);
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        [HttpGet]
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
                    _ = await UserManager.AddLoginAsync(user, loginInfo);
                    result = await SignInManager.ExternalLoginSignInAsync(loginInfo.LoginProvider, loginInfo.ProviderKey, isPersistent: true);
                    if (result.Succeeded)
                    {
                        await externalLoginProfileExtractor.TryExtractProfile(user, loginInfo);
                    }
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
            return View("ExternalLoginConfirmation",
                new ExternalLoginConfirmationViewModel()
                {
                    ReturnUrl = returnUrl,
                    LoginProviderName = loginInfo.LoginProvider,
                    RulesApproved = false,
                }
                );

        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [AllowAnonymous]
        [HttpPost]

        public async Task<ActionResult> ExternalLoginConfirmation(
            ExternalLoginConfirmationViewModel model)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var loginInfo = await SignInManager.GetExternalLoginInfoAsync();
                if (loginInfo == null)
                {
                    return View("ExternalLoginFailure");
                }

                var email = loginInfo.Principal.FindFirstValue(ClaimTypes.Email);

                var user = new JoinIdentityUser() { UserName = email };
                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user, loginInfo);
                    if (result.Succeeded)
                    {
                        var token = await UserManager.GenerateEmailConfirmationTokenAsync(user);
                        _ = await UserManager.ConfirmEmailAsync(user, token);

                        await SignInManager.SignInAsync(user, isPersistent: true);

                        await externalLoginProfileExtractor.TryExtractProfile(user, loginInfo);
                        return RedirectToLocal(model.ReturnUrl);
                    }
                }

                ModelState.AddErrors(result);
            }

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
        [HttpGet]
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
        public async Task<ActionResult> AccessDenied(string returnUrl, int? projectId)
        {
            if (projectId is not null)
            {
                var project = await projectRepository.Value.GetProjectAsync(projectId.Value);
                return View("ErrorNoAccessToProject", new ErrorNoAccessToProjectViewModel(project));
            }
            return View("AccessDenied", returnUrl);
        }
    }
}
