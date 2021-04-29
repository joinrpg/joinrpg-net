using System;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Identity;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authentication;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers
{
    [Authorize]
    public class ManageController : Common.ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ExternalLoginProfileExtractor externalLoginProfileExtractor;

        private ApplicationUserManager UserManager { get; }

        public ManageController(
            ApplicationUserManager userManager,
            ApplicationSignInManager signInManager,
            IUserRepository userRepository,
            IUserService userService,
            ICurrentUserAccessor currentUserAccessor,
            ConfigurationAdapter configurationAdapter,
            ExternalLoginProfileExtractor externalLoginProfileExtractor)
        {
            UserManager = userManager;
            SignInManager = signInManager;
            UserRepository = userRepository;
            _userService = userService;
            CurrentUserAccessor = currentUserAccessor;
            ConfigurationAdapter = configurationAdapter;
            this.externalLoginProfileExtractor = externalLoginProfileExtractor;
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
                return RedirectToAction("SetupProfile", new { Message = ManageMessageId.Error });
            }

            await externalLoginProfileExtractor.TryExtractProfile(user, loginInfo);
            return RedirectToAction("SetupProfile");
        }

        [HttpGet]
        public async Task<ActionResult> SetupProfile(bool checkContactsMessage = false)
        {
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
                Vk = user.Extra?.Vk,
                Livejournal = user.Extra?.Livejournal ?? "",
                Telegram = user.Extra?.Telegram ?? "",
                Skype = user.Extra?.Skype ?? "",
                LastClaimId = lastClaim?.ClaimId,
                LastClaimProjectId = lastClaim?.ProjectId,
                IsVerifiedFlag = user.VerifiedProfileFlag,
                IsVkVerifiedFlag = user.Extra?.VkVerified ?? false,
                SocialNetworkAccess = (ContactsAccessTypeView)user.GetSocialNetworkAccess(),
                SocialLoginStatus = user.GetSocialLogins().ToList(),
                Email = user.Email,
                HasPassword = user.PasswordHash != null,
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
                    viewModel.Telegram,
                    (ContactsAccessType)viewModel.SocialNetworkAccess
                  );
                var userId = CurrentUserAccessor.UserId;
                var user = await UserManager.FindByIdAsync(userId.ToString());
                _ = await UserManager.UpdateSecurityStampAsync(user);
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

        #region Helpers

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            Error,
        }

        #endregion
    }

}
