using JoinRpg.DataModel;

namespace Joinrpg.Web.Identity;

internal partial class MyUserStore : IUserRoleStore<JoinIdentityUser>
{
    Task IUserRoleStore<JoinIdentityUser>.AddToRoleAsync(JoinIdentityUser user, string roleName, CancellationToken cancellationToken) => throw new NotSupportedException();

    async Task<IList<string>> IUserRoleStore<JoinIdentityUser>.GetRolesAsync(JoinIdentityUser user, CancellationToken cancellationToken)
    {
        var dbUser = await LoadUser(user, cancellationToken);
        if (dbUser.Auth.IsAdmin)
        {
            return [Security.AdminRoleName];
        }
        else
        {
            return [];
        }
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
