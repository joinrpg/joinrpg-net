using Joinrpg.Web.Identity;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Identity;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authentication;
using JoinRpg.Portal.Infrastructure.Authentication.Telegram;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Avatars;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.UserProfile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Controllers;

[Authorize]
public class ManageController : Common.ControllerBase
{
    private readonly IUserService _userService;
    private readonly ExternalLoginProfileExtractor externalLoginProfileExtractor;
    private readonly ILogger<ManageController> logger;
    private readonly IAvatarService avatarService;

    private ApplicationUserManager UserManager { get; }

    public ManageController(
        ApplicationUserManager userManager,
        ApplicationSignInManager signInManager,
        IUserRepository userRepository,
        IUserService userService,
        ICurrentUserAccessor currentUserAccessor,
        ConfigurationAdapter configurationAdapter,
        ExternalLoginProfileExtractor externalLoginProfileExtractor,
        ILogger<ManageController> logger,
        IAvatarService avatarService
        )
    {
        UserManager = userManager;
        SignInManager = signInManager;
        UserRepository = userRepository;
        _userService = userService;
        CurrentUserAccessor = currentUserAccessor;
        ConfigurationAdapter = configurationAdapter;
        this.externalLoginProfileExtractor = externalLoginProfileExtractor;
        this.logger = logger;
        this.avatarService = avatarService;
    }

    private ApplicationSignInManager SignInManager { get; }
    public IUserRepository UserRepository { get; }
    private ICurrentUserAccessor CurrentUserAccessor { get; }
    private ConfigurationAdapter ConfigurationAdapter { get; }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> RemoveLogin(string loginProvider, string providerKey)
    {
        var userId = CurrentUserAccessor.UserId;
        var user = await UserManager.FindByIdAsync(userId.ToString());
        ManageMessageId? message;
        var result = await UserManager.RemoveLoginAsync(user, loginProvider, providerKey);
        await externalLoginProfileExtractor.CleanAfterLogin(user, loginProvider);
        if (result.Succeeded)
        {
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: true);
            }
            message = ManageMessageId.RemoveLoginSuccess;
        }
        else
        {
            message = ManageMessageId.Error;
        }
        return RedirectToAction("SetupProfile", new { Message = message });
    }

    //
    // GET: /Manage/ChangePassword
    public ActionResult ChangePassword() => View();

    [HttpGet("/.well-known/change-password")]
    public ActionResult ChangePasswordWellKnown() => RedirectToAction("ChangePassword");

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
        var userId = CurrentUserAccessor.UserId;
        var user = await UserManager.FindByIdAsync(userId.ToString());
        var result = await UserManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
        if (result.Succeeded)
        {
            await SignInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("SetupProfile", new { Message = ManageMessageId.ChangePasswordSuccess });
        }
        ModelState.AddErrors(result);
        return View(model);
    }

    //
    // GET: /Manage/SetPassword
    public ActionResult SetPassword() => View();

    //
    // POST: /Manage/SetPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> SetPassword(SetPasswordViewModel model)
    {
        if (ModelState.IsValid)
        {
            var userId = CurrentUserAccessor.UserId;
            var user = await UserManager.FindByIdAsync(userId.ToString());
            var result = await UserManager.AddPasswordAsync(user, model.NewPassword);
            if (result.Succeeded)
            {
                await SignInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("SetupProfile", new { Message = ManageMessageId.SetPasswordSuccess });
            }
            ModelState.AddErrors(result);
        }

        // If we got this far, something failed, redisplay form
        return View(model);
    }

    //
    // POST: /Manage/LinkLogin
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult LinkLogin(string provider)
    {
        var redirectUrl = Url.Action("LinkLoginCallback", "Manage");

        var authenticationProperties =
            SignInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, CurrentUserAccessor.UserId.ToString());
        return Challenge(authenticationProperties, provider);
    }

    //
    // GET: /Manage/LinkLoginCallback
    public async Task<ActionResult> LinkLoginCallback()
    {
        var loginInfo = await SignInManager.GetExternalLoginInfoAsync(ConfigurationAdapter.XsrfKey);
        if (loginInfo == null)
        {
            return RedirectToAction("SetupProfile", new { Message = ManageMessageId.Error });
        }

        var userId = CurrentUserAccessor.UserId;
        var user = await UserManager.FindByIdAsync(userId.ToString());

        var result = await UserManager.AddLoginAsync(user, loginInfo);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(i => i.Code == "LoginAlreadyAssociated"))
            {
                return RedirectToAction("SetupProfile", new { Message = ManageMessageId.SocialLoginAlreadyLinked });
            }
            logger.LogError("Unexpected error during linking user to another account: {loginError}", result.Errors.First().Code);
            return RedirectToAction("SetupProfile", new { Message = ManageMessageId.Error });
        }

        await externalLoginProfileExtractor.TryExtractProfile(user, loginInfo);
        return RedirectToAction("SetupProfile");
    }

    public async Task<ActionResult> LinkTelegramLoginCallback([FromServices] ICustomLoginStore loginStore, [FromServices] TelegramLoginValidator loginValidator)
    {

        var dictionary = Request.Query.Select(x => x).ToDictionary(x => x.Key, x => x.Value.First() ?? "");
        var value = loginValidator.CheckAuthorization(dictionary);

        var principal = new System.Security.Claims.ClaimsPrincipal();


        var userId = CurrentUserAccessor.UserId;
        var user = (await UserManager.FindByIdAsync(userId.ToString()))!;


        var telegramUserId = dictionary["id"];

        var u = await loginStore.FindByLoginAsync("telegram", telegramUserId, CancellationToken.None);

        if (u is not null)
        {
            return RedirectToAction("SetupProfile", new { Message = ManageMessageId.SocialLoginAlreadyLinked });
        }
        await loginStore.AddCustomLoginAsync(user, telegramUserId, "telegram", CancellationToken.None);

        await externalLoginProfileExtractor.TryExtractTelegramProfile(user, dictionary);
        return RedirectToAction("SetupProfile");
    }

    [HttpGet]
    public async Task<ActionResult> SetupProfile([FromServices] IOptions<TelegramLoginOptions> options, bool checkContactsMessage = false, ManageMessageId? message = null)
    {
        await avatarService.AddGrAvatarIfRequired(CurrentUserAccessor.UserId);

        var user = await UserRepository.WithProfile(CurrentUserAccessor.UserId);
        var lastClaim = checkContactsMessage ? user.Claims.OrderByDescending(c => c.CreateDate).FirstOrDefault() : null;
        var claimBeforeThat = checkContactsMessage ? user.Claims.OrderByDescending(c => c.CreateDate).Skip(1).FirstOrDefault() : null;
        if (claimBeforeThat != null && claimBeforeThat.CreateDate.AddMonths(3) > DateTime.Now && lastClaim != null)
        {
            return RedirectToAction("Edit", "Claim",
                new { lastClaim.ClaimId, lastClaim.ProjectId });
        }

        var model = new EditUserProfileViewModel()
        {
            SurName = user.SurName ?? "",
            UserId = user.UserId,
            FatherName = user.FatherName ?? "",
            BornName = user.BornName ?? "",
            PrefferedName = user.GetDisplayName(),
            //Gender = user.Extra.Gender,
            //BirthDate = user.Extra.BirthDate,
            PhoneNumber = user.Extra?.PhoneNumber ?? "",
            Nicknames = user.Extra?.Nicknames ?? "",
            GroupNames = user.Extra?.GroupNames ?? "",
            Livejournal = user.Extra?.Livejournal ?? "",
            Skype = user.Extra?.Skype ?? "",
            LastClaimId = lastClaim?.ClaimId,
            LastClaimProjectId = lastClaim?.ProjectId,
            IsVerifiedFlag = user.VerifiedProfileFlag,
            SocialNetworkAccess = (ContactsAccessTypeView)user.GetSocialNetworkAccess(),
            SocialLoginStatus = user.GetSocialLogins().ToList(),
            Email = user.Email,
            HasPassword = user.PasswordHash != null,
            Avatars = new UserAvatarListViewModel(user),
            Message = message,
            TelegramBotName = options.Value.BotName,
        };

        return base.View(model);
    }

    [HttpPost]
    public async Task<ActionResult> SetupProfile(EditUserProfileViewModel viewModel)
    {
        try
        {
            await
              _userService.UpdateProfile(viewModel.UserId,
                new UserFullName(
                    new PrefferedName(viewModel.PrefferedName),
                    new BornName(viewModel.BornName),
                    new SurName(viewModel.SurName),
                    new FatherName(viewModel.FatherName)),
                viewModel.Gender, viewModel.PhoneNumber, viewModel.Nicknames,
                viewModel.GroupNames, viewModel.Skype, viewModel.Livejournal,
                (ContactsAccessType)viewModel.SocialNetworkAccess
              );
            var userId = CurrentUserAccessor.UserId;
            var user = await UserManager.FindByIdAsync(userId.ToString());
            await SignInManager.RefreshSignInAsync(user);
            if (viewModel.LastClaimId == null || viewModel.LastClaimProjectId == null)
            {
                return RedirectToAction("SetupProfile");
            }
            else
            {
                return RedirectToAction("Edit", "Claim",
                  new { ClaimId = viewModel.LastClaimId, ProjectId = viewModel.LastClaimProjectId });
            }
        }
        catch
        {
            //ModelState.AddException(e);
            return View(viewModel);
        }
    }
}

