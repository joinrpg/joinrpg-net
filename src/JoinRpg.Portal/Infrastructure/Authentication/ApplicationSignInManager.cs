using Joinrpg.Web.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Identity;

public class ApplicationSignInManager : SignInManager<JoinIdentityUser>
{
    public ApplicationSignInManager(
        JoinUserManager userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<JoinIdentityUser> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<JoinIdentityUser>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<JoinIdentityUser> userConfirmation
    ) :
        base(userManager,
            contextAccessor,
            claimsFactory,
            optionsAccessor,
            logger,
            schemes,
            userConfirmation
            )
    {
    }
}
