using Microsoft.AspNetCore.Identity;

namespace Joinrpg.Web.Identity
{
    public partial class MyUserStore : IUserSecurityStampStore<JoinIdentityUser>
    {
        async Task<string> IUserSecurityStampStore<JoinIdentityUser>.GetSecurityStampAsync(JoinIdentityUser user, CancellationToken cancellationToken)
        {
            var dbUser = await LoadUser(user, cancellationToken);
            // if AspNetSecurityStamp setting random guid will make it refresh soonish
            return dbUser?.Auth?.AspNetSecurityStamp ?? new Guid().ToString();
        }

        async Task IUserSecurityStampStore<JoinIdentityUser>.SetSecurityStampAsync(JoinIdentityUser user, string stamp, CancellationToken cancellationToken)
        {
            var dbUser = await LoadUser(user, cancellationToken);
            if (dbUser == null)
            {
                return; // User not created yet, ignore
                //TODO if we still need it?
            }
            dbUser.Auth.AspNetSecurityStamp = stamp;
            _ = await _ctx.SaveChangesAsync(cancellationToken);
        }
    }
}
