using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Joinrpg.Web.Identity.AspNetCore;
using JoinRpg.DataModel;
using Microsoft.AspNetCore.Identity;

namespace Joinrpg.Web.Identity
{
    public partial class MyUserStore : IUserLoginStore<JoinIdentityUser>
    {
        async Task IUserLoginStore<JoinIdentityUser>.AddLoginAsync(JoinIdentityUser user, UserLoginInfo login, CancellationToken cancellationToken)
        {
            var dbUser = await LoadUser(user);
            dbUser.ExternalLogins.Add(login.ToUserExternalLogin());
            await _ctx.SaveChangesAsync();
        }

        async Task<JoinIdentityUser?> IUserLoginStore<JoinIdentityUser>.FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            var uel = await _ctx.Set<UserExternalLogin>().SingleOrDefaultAsync(u => u.Key == providerKey && u.Provider == loginProvider);

            return uel?.User.ToIdentityUser();
        }

        async Task<IList<UserLoginInfo>> IUserLoginStore<JoinIdentityUser>.GetLoginsAsync(JoinIdentityUser user, CancellationToken cancellationToken)
        {
            var dbUser = await LoadUser(user);
            return dbUser.ExternalLogins.Select(uel => uel.ToUserLoginInfoCore()).ToList();
        }

        async Task IUserLoginStore<JoinIdentityUser>.RemoveLoginAsync(JoinIdentityUser user, string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            var dbUser = await LoadUser(user);
            var el =
                dbUser.ExternalLogins.First(
                    externalLogin => externalLogin.Key == providerKey &&
                                     externalLogin.Provider == loginProvider);
            _ctx.Set<UserExternalLogin>().Remove(el);
            await _ctx.SaveChangesAsync();
        }
    }
}
