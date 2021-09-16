using System;
using System.Security.Claims;
using System.Threading.Tasks;
using AspNet.Security.OAuth.Vkontakte;
using Joinrpg.Web.Identity;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace JoinRpg.Portal.Infrastructure.Authentication
{

#nullable enable 
    /// <summary>
    /// Task of this class is to extract useful data from social logins
    /// </summary>
    public class ExternalLoginProfileExtractor
    {
        private readonly IUserService userService;

        public ExternalLoginProfileExtractor(IUserService userService) => this.userService = userService;

        public async Task TryExtractProfile(JoinIdentityUser user, ExternalLoginInfo loginInfo)
        {
            UserFullName userFullName = TryGetUserName(loginInfo);
            await userService.SetNameIfNotSetWithoutAccessChecks(user.Id, userFullName);

            if (TryGetVkId(loginInfo) is VkId vkId)
            {
                var vkAvatar = TryGetClaim(loginInfo, VkontakteAuthenticationConstants.Claims.PhotoUrl);

                if (vkAvatar is not null)
                {
                    await userService.SetVkIfNotSetWithoutAccessChecks(user.Id, vkId,
                      new AvatarInfo(new Uri(vkAvatar), 50, 50));
                }
            }

            if (TryGetClaim(loginInfo, "urn:google:photo") is string googleAvatar)
            {
                await userService.SetGoogleIfNotSetWithoutAccessChecks(user.Id,
                     new AvatarInfo(new Uri(googleAvatar), 96, 96));
            }
            //var googleProfileLink = loginInfo.Principal.FindFirstValue("urn:google:profile");
        }

        /// <summary>
        /// This method is required, because FindFirstValue is not properly null-annotated
        /// </summary>
        private static string? TryGetClaim(ExternalLoginInfo loginInfo, string claimType)
            => loginInfo.Principal.FindFirstValue(claimType);

        private static UserFullName TryGetUserName(ExternalLoginInfo loginInfo)
        {
            var bornName = BornName.FromOptional(loginInfo.Principal.FindFirstValue(ClaimTypes.GivenName));
            var surName = SurName.FromOptional(loginInfo.Principal.FindFirstValue(ClaimTypes.Surname));
            var prefferedName = new PrefferedName(loginInfo.Principal.FindFirstValue(ClaimTypes.Name));

            return new UserFullName(prefferedName, bornName, surName, FatherName: null);
        }

        private static VkId? TryGetVkId(ExternalLoginInfo loginInfo)
        {
            return loginInfo.LoginProvider == "Vkontakte"
                && TryGetClaim(loginInfo, ClaimTypes.NameIdentifier) is string id
                ? new VkId(id)
                : null;
        }

        internal async Task CleanAfterLogin(JoinIdentityUser user, string loginProvider)
        {
            if (loginProvider == "Vkontakte")
            {
                await userService.RemoveVkFromProfile(user.Id);
            }
        }
    }
}
