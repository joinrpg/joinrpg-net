using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JoinRpg.DataModel;
using Microsoft.AspNetCore.Identity;

namespace Joinrpg.Web.Identity
{
    public partial class MyUserStore : IUserRoleStore<JoinIdentityUser>
    {
        Task IUserRoleStore<JoinIdentityUser>.AddToRoleAsync(JoinIdentityUser user, string roleName, CancellationToken cancellationToken) => throw new NotSupportedException();

        async Task<IList<string>> IUserRoleStore<JoinIdentityUser>.GetRolesAsync(JoinIdentityUser user, CancellationToken cancellationToken)
        {
            var dbUser = await LoadUser(user, cancellationToken);
            List<string> list;
            if (dbUser.Auth.IsAdmin)
            {
                list = new List<string>() { Security.AdminRoleName };
            }
            else
            {
                list = new List<string>();
            }
            return list;
        }

        Task<IList<JoinIdentityUser>> IUserRoleStore<JoinIdentityUser>.GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken) => throw new NotSupportedException();

        async Task<bool> IUserRoleStore<JoinIdentityUser>.IsInRoleAsync(JoinIdentityUser user, string roleName, CancellationToken cancellationToken)
        {
            IUserRoleStore<JoinIdentityUser> self = this;
            var roles = await self.GetRolesAsync(user, cancellationToken);

            return roles.Contains(roleName);
        }

        Task IUserRoleStore<JoinIdentityUser>.RemoveFromRoleAsync(JoinIdentityUser user, string roleName, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
