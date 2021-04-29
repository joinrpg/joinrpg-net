using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Joinrpg.Web.Identity
{
    public partial class MyUserStore :
        IRoleStore<string>
    {
        Task<IdentityResult> IRoleStore<string>.CreateAsync(string role, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        Task<IdentityResult> IRoleStore<string>.DeleteAsync(string role, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        Task<string?> IRoleStore<string>.FindByIdAsync(string roleId, CancellationToken cancellationToken)
            => CheckRole(roleId);

        Task<string?> IRoleStore<string>.FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
            => CheckRole(normalizedRoleName);

        Task<string?> IRoleStore<string>.GetNormalizedRoleNameAsync(string role, CancellationToken cancellationToken)
            => CheckRole(role);

        Task<string?> IRoleStore<string>.GetRoleIdAsync(string role, CancellationToken cancellationToken)
            => CheckRole(role);

        Task<string?> IRoleStore<string>.GetRoleNameAsync(string role, CancellationToken cancellationToken)
            => CheckRole(role);

        Task IRoleStore<string>.SetNormalizedRoleNameAsync(string role, string normalizedName, CancellationToken cancellationToken)
            => Task.CompletedTask;

        Task IRoleStore<string>.SetRoleNameAsync(string role, string roleName, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        Task<IdentityResult> IRoleStore<string>.UpdateAsync(string role, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        // We actually support only one role, everything else is not bind to roles
        private static Task<string?> CheckRole(string roleId) => Task.FromResult(roleId == "Admin" ? "Admin" : null);
    }
}
