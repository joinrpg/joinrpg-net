using Joinrpg.Web.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Identity
{
    public class ApplicationSignInManager : SignInManager<JoinIdentityUser>
    {
        public ApplicationSignInManager(
            UserManager<JoinIdentityUser> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<JoinIdentityUser> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<SignInManager<JoinIdentityUser>> logger,
            IAuthenticationSchemeProvider schemes
        ) :
            base(userManager,
                contextAccessor,
                claimsFactory,
                optionsAccessor,
                logger,
                schemes)
        {
        }
    }
}