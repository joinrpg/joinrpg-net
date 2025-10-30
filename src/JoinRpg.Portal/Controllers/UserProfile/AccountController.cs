using System.Security.Claims;
using Joinrpg.Web.Identity;
using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Portal.Identity;
using JoinRpg.Portal.Infrastructure.Authentication;
using JoinRpg.Services.Interfaces.Avatars;
using JoinRpg.Services.Interfaces.Notification;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Controllers;

[Authorize]
public class AccountController(
    JoinUserManager userManager,
    ApplicationSignInManager signInManager,
    IAccountEmailService<JoinIdentityUser> emailService,
    IOptions<RecaptchaOptions> recaptchaOptions,
    IRecaptchaVerificator recaptchaVerificator,
    ExternalLoginProfileExtractor externalLoginProfileExtractor,
    ILogger<AccountController> logger
    ) : Common.ControllerBase
{
    [AllowAnonymous]
    public async Task<ActionResult> Login(string returnUrl) => View(await CreateLoginPageViewModelAsync(returnUrl));

    private async Task<LoginPageViewModel> CreateLoginPageViewModelAsync(string returnUrl)
    {
        return new LoginPageViewModel()
        {
            External = new ExternalLoginListViewModel
            {
                ReturnUrl = returnUrl,
                ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync())
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
        var user = await userManager.FindByNameAsync(model.Email);

        if (user != null)
        {
            if (!await userManager.IsEmailConfirmedAsync(user))
            {
                await SendConfirmationEmail(user);
                return View("EmailUnconfirmed");
            }
        }

        // This doesn't count login failures towards account lockout
        // To enable password failures to trigger account lockout, change to shouldLockout: true
        var result =
            await signInManager.PasswordSignInAsync(model.Email,
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
    public async Task<ActionResult> Register(RegisterViewModel model, [FromForm(Name = "g-recaptcha-response")] string recaptchaToken, [FromServices] IAvatarService avatarService)
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
                ModelState.AddModelError("captcha", "Невозможно верифицировать ReCAPTCHA. Если эта ошибка повторяется, пожалуйста, обратитесь в техподдержку.");
                model.IsRecaptchaConfigured = isRecaptchaConfigured;
                model.RecaptchaPublicKey = recaptchaOptions.Value.PublicKey;
                return View(model);
            }
        }

        var currentUser = await userManager.FindByNameAsync(model.Email);

        if (currentUser != null)
        {
            ModelState.AddModelError("",
                "Вы уже зарегистрировались. Если пароль не подходит, нажмите «Забыли пароль?»");
            var loginModel = await CreateLoginPageViewModelAsync("");
            loginModel.Login.Email = model.Email;
            return View("Login", loginModel);
        }

        var user = new JoinIdentityUser { UserName = model.Email };
        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            ModelState.AddErrors(result);
            model.IsRecaptchaConfigured = isRecaptchaConfigured;
            model.RecaptchaPublicKey = recaptchaOptions.Value.PublicKey;
            return View(model);
        }

        await avatarService.EnsureAvatarPresent(user.Id);

        //We don't want to sign in user until he has email confirmed 
        //await SignInManager.SignInAsync(user, isPersistent:false, rememberBrowser:false);

        await SendConfirmationEmail(user);

        return View("RegisterSuccess");
    }

    private async Task SendConfirmationEmail(JoinIdentityUser user)
    {
        var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var callbackUrl = Url.Action("ConfirmEmail",
            "Account",
            new { userId = user.Id, code },
            protocol: Request.Scheme) ?? throw new InvalidOperationException();

        await emailService.ConfirmEmail(user, callbackUrl);
    }

    //
    // GET: /Account/ConfirmEmail
    [AllowAnonymous]
    public async Task<ActionResult> ConfirmEmail(int? userId, string code)
    {
        if (userId == null || code == null)
        {
            return NotFound();
        }
        var user = await userManager.FindByIdAsync(userId.Value.ToString()) ?? throw new InvalidOperationException();
        var result = await userManager.ConfirmEmailAsync(user, code);
        if (result.Succeeded)
        {
            return View("ConfirmEmail");
        }
        else
        {
            foreach (var err in result.Errors)
            {
                logger.LogWarning("Ошибка при подтверждении email (Code={accountErrorCode}: {description}", err.Code, err.Description);
            }
            throw new JoinRpgAccountOperationFailedException($"Не удалось подтвердить email ({string.Join(", ", result.Errors.Select(e => e.Code.ToString()))})");
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
            var user = await userManager.FindByNameAsync(model.Email);
            if (user == null /*|| !(await UserManager.IsEmailConfirmedAsync(user.Id))*/)
            {
                // Don't reveal that the user does not exist or is not confirmed
                return View("ForgotPasswordConfirmation");
            }

            // Send an email with this link
            var code = await userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action("ResetPassword",
                "Account",
                new { userId = user.Id, code },
                protocol: Request.Scheme) ?? throw new InvalidOperationException();


            await emailService.ResetPasswordEmail(user, callbackUrl);

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
            return NotFound();
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

        var user = await userManager.FindByNameAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError("", "Email не найден");
            return View();
        }

        var result =
            await userManager.ResetPasswordAsync(user, model.Code, model.Password);
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
        var redirectUrl = Url.Action(
            "ExternalLoginCallback",
            "Account",
            new { ReturnUrl = returnUrl },
            protocol: Request.Scheme //Ensure that it will request HTTPS if required
            );

        var authenticationProperties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(authenticationProperties, provider);
    }

    [AllowAnonymous]
    public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
    {
        var auth = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);

        if (auth.None)
        {
            logger.LogInformation("Неудачная попытка войти через ВК, дополнительная информация {authFailure}", auth.Failure);
            return RedirectToAction("Login");
        }

        var loginInfo = await signInManager.GetExternalLoginInfoAsync();
        if (loginInfo == null)
        {
            return RedirectToAction("Login");
        }

        // Sign in the user with this external login provider if the user already has a login
        var result = await signInManager.ExternalLoginSignInAsync(loginInfo.LoginProvider, loginInfo.ProviderKey, isPersistent: true);

        var email = loginInfo.Principal.FindFirstValue(ClaimTypes.Email);

        // If sign in failed, may be we have user with same email. Let's bind.
        if (!result.Succeeded && !string.IsNullOrWhiteSpace(email))
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user != null)
            {
                logger.LogInformation("Привязываем привязки {loginProvider} / {loginProviderKey} к аккаунту {userId}",
                    loginInfo.LoginProvider, loginInfo.LoginProvider, user.Id);

                var addLoginResult = await userManager.AddLoginAsync(user, loginInfo);
                if (!addLoginResult.Succeeded)
                {
                    logger.LogWarning("Неожиданная ошибка при привязке {loginProvider} / {loginProviderKey} к аккаунту {userId}: {loginResult}",
                        loginInfo.LoginProvider, loginInfo.ProviderKey, user.Id, addLoginResult);
                    return View("Lockout");

                }

                if (!user.EmaiLConfirmed)
                {
                    logger.LogInformation("Пользователь входит через {loginProvider} / {loginProviderKey}, но у него не подтвержден email {email}. Он соответствует тому, что отдает ВК, подтверждаем.",
                        loginInfo.LoginProvider, loginInfo.ProviderKey, email);
                    var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    _ = await userManager.ConfirmEmailAsync(user, token);
                }

                result = await signInManager.ExternalLoginSignInAsync(loginInfo.LoginProvider, loginInfo.ProviderKey, isPersistent: true);
                if (result.Succeeded)
                {
                    await externalLoginProfileExtractor.TryExtractProfile(user, loginInfo);
                }
                else
                {
                    logger.LogWarning("Неожиданная ошибка при повторном логине после привязки {loginProvider} / {loginProviderKey} к аккаунту {userId}: {loginResult}",
                        loginInfo.LoginProvider, loginInfo.ProviderKey, user.Id, result);
                    return View("Lockout");
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

    [AllowAnonymous]
    public ActionResult GoogleDeprecated() => View();

    //
    // POST: /Account/ExternalLoginConfirmation
    [HttpPost]
    [AllowAnonymous]

    public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, [FromServices] IAvatarService avatarService)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Manage");
        }

        if (ModelState.IsValid)
        {
            // Get the information about the user from the external login provider
            var loginInfo = await signInManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return View("ExternalLoginFailure");
            }

            var email = loginInfo.Principal.FindFirstValue(ClaimTypes.Email);

            if (email is null)
            {
                ModelState.AddModelError("", "Невозможно создать аккаунт, если в соцсети не указан ваш email. Создайте аккаунт обычным способом, а потом привяжите к нему соцсеть");
                return View(model);
            }

            var user = new JoinIdentityUser() { UserName = email };
            var result = await userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                result = await userManager.AddLoginAsync(user, loginInfo);
                if (result.Succeeded)
                {
                    var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    _ = await userManager.ConfirmEmailAsync(user, token);

                    await signInManager.SignInAsync(user, isPersistent: true);

                    await externalLoginProfileExtractor.TryExtractProfile(user, loginInfo);
                    await avatarService.EnsureAvatarPresent(user.Id);
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
        await signInManager.SignOutAsync();
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
    public async Task<ActionResult> AccessDenied(string returnUrl, int? projectId, [FromServices] IProjectMetadataRepository projectMetadataRepository)
    {
        if (projectId is int projectIdValue)
        {
            var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectIdValue));
            return View("ErrorNoAccessToProject", new ErrorNoAccessToProjectViewModel(projectInfo));
        }
        return View("AccessDenied", returnUrl);
    }
}
