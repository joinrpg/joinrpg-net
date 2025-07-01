using Microsoft.AspNetCore.Identity;

namespace JoinRpg.IdPortal.Components.Account;
internal sealed class IdentityUserAccessor(UserManager<JoinIdentityUser> userManager, IdentityRedirectManager redirectManager)
{
    public async Task<JoinIdentityUser> GetRequiredUserAsync(HttpContext context)
    {
        var user = await userManager.GetUserAsync(context.User);

        if (user is null)
        {
            redirectManager.RedirectToWithStatus("Account/InvalidUser", $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);
        }

        return user;
    }
}
