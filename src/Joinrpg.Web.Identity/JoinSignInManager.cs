using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Joinrpg.Web.Identity;

public class JoinSignInManager(
    JoinUserManager userManager,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<JoinIdentityUser> claimsFactory,
    IOptions<IdentityOptions> optionsAccessor,
    ILogger<JoinSignInManager> logger,
    IAuthenticationSchemeProvider schemes,
    IUserConfirmation<JoinIdentityUser> userConfirmation,
    IUserStore<JoinIdentityUser> store
    ) : SignInManager<JoinIdentityUser>(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, userConfirmation)
{
    private readonly MyUserStore userStore = (MyUserStore)store;

    public override async Task SignInWithClaimsAsync(JoinIdentityUser user, AuthenticationProperties? authenticationProperties, IEnumerable<Claim> additionalClaims)
    {
        await base.SignInWithClaimsAsync(user, authenticationProperties, additionalClaims);
        await userStore.UpdateLastLoginDateAsync(user.Id);
    }
}
