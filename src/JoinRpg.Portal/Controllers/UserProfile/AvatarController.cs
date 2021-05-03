using System.Threading.Tasks;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.UserProfile
{
    [Authorize]
    public class AvatarController : Controller
    {
        private readonly ICurrentUserAccessor currentUserAccessor;
        private readonly IUserService userService;

        public AvatarController(
            ICurrentUserAccessor currentUserAccessor,
            IUserService userService)
        {
            this.currentUserAccessor = currentUserAccessor;
            this.userService = userService;
        }

        [HttpPost("manage/avatars/choose")]
        public async Task<IActionResult> ChooseAvatar(int userAvatarId)
        {
            await userService.SelectAvatar(
                currentUserAccessor.UserId,
                new AvatarIdentification(userAvatarId)
                );
            return RedirectToAction("SetupProfile", "Manage");
        }

        [HttpPost("manage/avatars/delete")]
        public async Task<IActionResult> DeleteAvatar(int userAvatarId)
        {
            await userService.DeleteAvatar(
                currentUserAccessor.UserId,
                new AvatarIdentification(userAvatarId)
                );
            return RedirectToAction("SetupProfile", "Manage");
        }
    }
}
