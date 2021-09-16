using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Joinrpg.Web.Identity
{
    public partial class MyUserStore : IUserEmailStore<JoinIdentityUser>
    {
        async Task<JoinIdentityUser?> IUserEmailStore<JoinIdentityUser>.FindByEmailAsync(string normalizedEmail, CancellationToken ct)
        {
            var dbUser = await LoadUser(normalizedEmail, ct);
            return dbUser?.ToIdentityUser();
        }

        Task<string> IUserEmailStore<JoinIdentityUser>.GetEmailAsync(JoinIdentityUser user, CancellationToken ct) => Task.FromResult(user.UserName);

        async Task<bool> IUserEmailStore<JoinIdentityUser>.GetEmailConfirmedAsync(JoinIdentityUser user, CancellationToken ct)
        {
            var dbUser = await LoadUser(user, ct);
            return dbUser.Auth.EmailConfirmed;
        }

        Task<string> IUserEmailStore<JoinIdentityUser>.GetNormalizedEmailAsync(JoinIdentityUser user, CancellationToken ct) => Task.FromResult(user.UserName.ToUpperInvariant());

        Task IUserEmailStore<JoinIdentityUser>.SetEmailAsync(JoinIdentityUser user, string email, CancellationToken ct)
        {
            //it will be saved by calling Update later
            user.UserName = email;
            return Task.CompletedTask;
        }

        Task IUserEmailStore<JoinIdentityUser>.SetEmailConfirmedAsync(JoinIdentityUser user, bool confirmed, CancellationToken ct)
        {
            //it will be saved by calling Update later
            user.EmaiLConfirmed = confirmed;
            return Task.CompletedTask;
        }

        Task IUserEmailStore<JoinIdentityUser>.SetNormalizedEmailAsync(JoinIdentityUser user, string normalizedEmail, CancellationToken ct)
        {
            //We don't persist it for now
            return Task.CompletedTask;
        }
    }
}
