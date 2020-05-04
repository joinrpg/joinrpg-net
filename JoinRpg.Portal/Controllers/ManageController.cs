using System;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Identity;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers
{
    [Authorize]
    public class ManageController : Common.ControllerBase
    {
        private readonly IUserService _userService;
        private ApplicationUserManager UserManager { get; }

        public ManageController(
            ApplicationUserManager userManager,
            ApplicationSignInManager signInManager,
            IUserRepository userRepository,
            IUserService userService,
            ICurrentUserAccessor currentUserAccessor,
            ConfigurationAdapter configurationAdapter)
        {
            UserManager = userManager;
            SignInManager = signInManager;
            UserRepository = userRepository;
            _userService = userService;
            CurrentUserAccessor = currentUserAccessor;
            ConfigurationAdapter = configurationAdapter;
        }

        private ApplicationSignInManager SignInManager { get; }
        public IUserRepository UserRepository { get; }
        private ICurrentUserAccessor CurrentUserAccessor { get; }
        private ConfigurationAdapter ConfigurationAdapter { get; }

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

            var userId = CurrentUserAccessor.UserId;
            var user = await UserManager.FindByIdAsync(userId.ToString());
            var model = new IndexViewModel
            {
                HasPassword = user.HasPassword,
                Email = CurrentUserAccessor.Email,
                LoginsCount = (await UserManager.GetLoginsAsync(user)).Count,
            };
            return View(model);
        }

        //
        // POST: /Manage/RemoveLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveLogin(string loginProvider, string providerKey)
        {
            var userId = CurrentUserAccessor.UserId;
            var user = await UserManager.FindByIdAsync(userId.ToString());
            ManageMessageId? message;
            var result = await UserManager.RemoveLoginAsync(user, loginProvider, providerKey);
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
            return RedirectToAction("ManageLogins", new { Message = message });
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
            var userId = CurrentUserAccessor.UserId;
            var user = await UserManager.FindByIdAsync(userId.ToString());
            var result = await UserManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await SignInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", new { Message = ManageMessageId.ChangePasswordSuccess });
            }
            ModelState.AddErrors(result);
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
                var userId = CurrentUserAccessor.UserId;
                var user = await UserManager.FindByIdAsync(userId.ToString());
                var result = await UserManager.AddPasswordAsync(user, model.NewPassword);
                if (result.Succeeded)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", new { Message = ManageMessageId.SetPasswordSuccess });
                }
                ModelState.AddErrors(result);
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
            var userId = CurrentUserAccessor.UserId;
            var user = await UserManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return View("Error");
            }
            var userLogins = await UserManager.GetLoginsAsync(user);
            var otherLogins =
              (await SignInManager.GetExternalAuthenticationSchemesAsync())
                .Where(auth => userLogins.All(ul => auth.Name != ul.LoginProvider))
                .ToList();
            ViewBag.ShowRemoveButton = user.HasPassword || userLogins.Count > 1;
            return View(new ManageLoginsViewModel
            {
                CurrentLogins = userLogins.Select(ul => new UserLoginInfoViewModel()
                {
                    LoginProvider = ul.LoginProvider,
                    ProviderKey = ul.ProviderKey
                }).ToList(),
                OtherLogins = otherLogins.Select(ol => ol.ToViewModel()).ToList(),
            });
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
                return RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
            }

            var userId = CurrentUserAccessor.UserId;
            var user = await UserManager.FindByIdAsync(userId.ToString());

            var result = await UserManager.AddLoginAsync(user, loginInfo);
            return result.Succeeded
              ? RedirectToAction("ManageLogins")
              : RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
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
            return View(new EditUserProfileViewModel()
            {
                SurName = user.SurName,
                UserId = user.UserId,
                FatherName = user.FatherName,
                BornName = user.BornName,
                PrefferedName = user.GetDisplayName(),
                //Gender = user.Extra.Gender,
                //BirthDate = user.Extra.BirthDate,
                PhoneNumber = user.Extra?.PhoneNumber,
                Nicknames = user.Extra?.Nicknames,
                GroupNames = user.Extra?.GroupNames,
                Vk = user.Extra?.Vk,
                Livejournal = user.Extra?.Livejournal,
                Telegram = user.Extra?.Telegram,
                Skype = user.Extra?.Skype,
                LastClaimId = lastClaim?.ClaimId,
                LastClaimProjectId = lastClaim?.ProjectId,
                IsVerifiedFlag = user.VerifiedProfileFlag,
            });
        }

        [HttpPost]
        public async Task<ActionResult> SetupProfile(EditUserProfileViewModel viewModel)
        {
            try
            {
                await
                  _userService.UpdateProfile(viewModel.UserId, viewModel.SurName, viewModel.FatherName,
                    viewModel.BornName, viewModel.PrefferedName, viewModel.Gender, viewModel.PhoneNumber, viewModel.Nicknames,
                    viewModel.GroupNames, viewModel.Skype, viewModel.Vk, viewModel.Livejournal, viewModel.Telegram);
                var userId = CurrentUserAccessor.UserId;
                var user = await UserManager.FindByIdAsync(userId.ToString());
                await UserManager.UpdateSecurityStampAsync(user);
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
            catch (Exception e)
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
