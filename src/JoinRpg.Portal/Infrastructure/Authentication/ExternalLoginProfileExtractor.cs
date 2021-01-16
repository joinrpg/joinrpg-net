using System.Security.Claims;
using System.Threading.Tasks;
using Joinrpg.Web.Identity;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace JoinRpg.Portal.Infrastructure.Authentication
{
    /// <summary>
    /// Task of this class is to extract useful data from social logins
    /// </summary>
    public class ExternalLoginProfileExtractor
    {
        private readonly IUserService userService;

        public ExternalLoginProfileExtractor(IUserService userService)
        {
            this.userService = userService;
        }

        public async Task TryExtractProfile(JoinIdentityUser user, ExternalLoginInfo loginInfo)
        {
            UserFullName userFullName = TryGetUserName(loginInfo);
            await userService.SetNameIfNotSetWithoutAccessChecks(user.Id, userFullName);

            //var googleProfileLink = loginInfo.Principal.FindFirstValue("urn:google:profile");
            //var vkAvatar = loginInfo.Principal.FindFirstValue("urn:vkontakte:photo:link");
        }

        public static UserFullName TryGetUserName(ExternalLoginInfo loginInfo)
        {
            var bornName = BornName.FromOptional(loginInfo.Principal.FindFirstValue(ClaimTypes.GivenName));
            var surName = SurName.FromOptional(loginInfo.Principal.FindFirstValue(ClaimTypes.Surname));
            var prefferedName = new PrefferedName(loginInfo.Principal.FindFirstValue(ClaimTypes.Name));

            return new UserFullName(prefferedName, bornName, surName, FatherName: null);
        }
    }
}
