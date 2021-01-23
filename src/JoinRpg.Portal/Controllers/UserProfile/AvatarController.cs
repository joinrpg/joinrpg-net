using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.UserProfile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.UserProfile
{
    [Authorize]
    public class AvatarController : Controller
    {
        private readonly IUserRepository userRepository;
        private readonly ICurrentUserAccessor currentUserAccessor;
        private readonly IUserService userService;

        public AvatarController(
            IUserRepository userRepository,
            ICurrentUserAccessor currentUserAccessor,
            IUserService userService)
        {
            this.userRepository = userRepository;
            this.currentUserAccessor = currentUserAccessor;
            this.userService = userService;
        }

        [HttpGet("manage/avatars")]
        public async Task<IActionResult> Home()
        {
            await userService.AddGrAvatarIfRequired(currentUserAccessor.UserId);

            var user = await userRepository.WithProfile(currentUserAccessor.UserId);
            return View(new UserAvatarListViewModel(user));
        }

        [HttpPost("manage/avatars/choose")]
        public async Task<IActionResult> ChooseAvatar(int userAvatarId)
        {
            await userService.SelectAvatar(
                currentUserAccessor.UserId,
                new AvatarIdentification(userAvatarId)
                );
            return RedirectToAction("Home");
        }

        [HttpPost("manage/avatars/delete")]
        public async Task<IActionResult> DeleteAvatar(int userAvatarId)
        {
            await userService.DeleteAvatar(
                currentUserAccessor.UserId,
                new AvatarIdentification(userAvatarId)
                );
            return RedirectToAction("Home");
        }
    }
}
