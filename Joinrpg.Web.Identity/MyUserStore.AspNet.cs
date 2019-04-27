using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.DataModel;
using Microsoft.AspNet.Identity;
using Claim = System.Security.Claims.Claim;

namespace Joinrpg.Web.Identity
{
    public partial class MyUserStore :
        IUserPasswordStore<JoinIdentityUser, int>,
        IUserLockoutStore<JoinIdentityUser, int>,
        IUserTwoFactorStore<JoinIdentityUser, int>,
        IUserEmailStore<JoinIdentityUser, int>,
        IUserLoginStore<JoinIdentityUser, int>,
        IUserRoleStore<JoinIdentityUser, int>,
        IUserClaimStore<JoinIdentityUser, int>,
        IUserSecurityStampStore<JoinIdentityUser, int>
    {
        async Task IUserStore<JoinIdentityUser, int>.CreateAsync(JoinIdentityUser user) => await CreateImpl(user);

        async Task IUserStore<JoinIdentityUser, int>.UpdateAsync(JoinIdentityUser user) => await UpdateImpl(user);

        Task IUserStore<JoinIdentityUser, int>.DeleteAsync(JoinIdentityUser user) => throw new NotImplementedException();

        async Task<JoinIdentityUser> IUserStore<JoinIdentityUser, int>.FindByIdAsync(int userId)
        {
            var dbUser = await LoadUser(userId);
            return dbUser?.ToIdentityUser();
        }

        async Task<JoinIdentityUser> IUserStore<JoinIdentityUser, int>.FindByNameAsync(string userName)
        {
            var dbUser = await LoadUser(userName);
            return dbUser?.ToIdentityUser();
        }

        async Task IUserPasswordStore<JoinIdentityUser, int>.SetPasswordHashAsync(JoinIdentityUser user, string passwordHash)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var dbUser = await LoadUser(user.UserName);

            dbUser.PasswordHash = passwordHash;

            await _ctx.SaveChangesAsync();
        }

        public async Task<string> GetPasswordHashAsync(JoinIdentityUser user)
        {
            var dbUser = await LoadUser(user);
            return dbUser.PasswordHash;
        }

        public async Task<bool> HasPasswordAsync(JoinIdentityUser user)
        {
            var dbUser = await LoadUser(user);
            return dbUser.PasswordHash != null;
        }

        public Task<DateTimeOffset> GetLockoutEndDateAsync(JoinIdentityUser user) =>
            throw new NotImplementedException();

        public Task SetLockoutEndDateAsync(JoinIdentityUser user, DateTimeOffset lockoutEnd) =>
            throw new NotImplementedException();

        public Task<int> IncrementAccessFailedCountAsync(JoinIdentityUser user) =>
            throw new NotImplementedException();

        public Task ResetAccessFailedCountAsync(JoinIdentityUser user) => Task.FromResult<object>(null);

        public Task<int> GetAccessFailedCountAsync(JoinIdentityUser user) => Task.FromResult(0);

        public Task<bool> GetLockoutEnabledAsync(JoinIdentityUser user) => Task.FromResult(false);

        public Task SetLockoutEnabledAsync(JoinIdentityUser user, bool enabled) =>
            throw new NotImplementedException();

        public Task SetTwoFactorEnabledAsync(JoinIdentityUser user, bool enabled) =>
            throw new NotImplementedException();

        public Task<bool> GetTwoFactorEnabledAsync(JoinIdentityUser user) => Task.FromResult(false);

        public async Task SetEmailAsync(JoinIdentityUser user, string email)
        {
            await SetUserNameImpl(user, email);
        }

        /// <inheritdoc />
        public Task<string> GetEmailAsync(JoinIdentityUser user)
        {
            return Task.FromResult(user.UserName);
        }

        /// <inheritdoc />
        public async Task<bool> GetEmailConfirmedAsync(JoinIdentityUser user)
        {
            var dbUser = await LoadUser(user);
            return dbUser.Auth.EmailConfirmed;
        }

        /// <inheritdoc />
        public async Task SetEmailConfirmedAsync(JoinIdentityUser user, bool confirmed)
        {
            var dbUser = await LoadUser(user);
            dbUser.Auth.EmailConfirmed = confirmed;
            await _ctx.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task<JoinIdentityUser> FindByEmailAsync(string email)
        {
            var user = await LoadUser(email);
            return user?.ToIdentityUser();
        }

        /// <inheritdoc />
        public async Task AddLoginAsync(JoinIdentityUser user, UserLoginInfo login)
        {
            var dbUser = await LoadUser(user);
            dbUser.ExternalLogins.Add(login.ToUserExternalLogin());
            await _ctx.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task RemoveLoginAsync(JoinIdentityUser user, UserLoginInfo login)
        {
            var dbUser = await LoadUser(user);
            var el =
                dbUser.ExternalLogins.First(
                    externalLogin => externalLogin.Key == login.ProviderKey &&
                                     externalLogin.Provider == login.LoginProvider);
            _ctx.Set<UserExternalLogin>().Remove(el);
            await _ctx.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task<IList<UserLoginInfo>> GetLoginsAsync(JoinIdentityUser user)
        {
            var dbUser = await LoadUser(user);
            return dbUser.ExternalLogins.Select(uel => uel.ToUserLoginInfo()).ToList();
        }

        /// <inheritdoc />
        public async Task<JoinIdentityUser> FindAsync(UserLoginInfo login)
        {
            var uel =
                await _ctx.Set<UserExternalLogin>()
                    .SingleOrDefaultAsync(u =>
                        u.Key == login.ProviderKey && u.Provider == login.LoginProvider);

            return uel?.User.ToIdentityUser();
        }

        #region Implementation of IUserRoleStore<User,in int>

        Task IUserRoleStore<JoinIdentityUser, int>.AddToRoleAsync(JoinIdentityUser user, string roleName)
            => throw new NotSupportedException();

        Task IUserRoleStore<JoinIdentityUser, int>.RemoveFromRoleAsync(JoinIdentityUser user, string roleName)
            => throw new NotSupportedException();

        public async Task<IList<string>> GetRolesAsync(JoinIdentityUser user)
        {
            var dbUser = await LoadUser(user);
            List<string> list;
            if (dbUser.Auth.IsAdmin)
            {
                list = new List<string>() {Security.AdminRoleName};
            }
            else
            {
                list = new List<string>();
            }

            return list;
        }

        public async Task<bool> IsInRoleAsync(JoinIdentityUser user, string roleName)
        {
            var roles = await GetRolesAsync(user);
            return roles.Contains(roleName);
        }

        #endregion




       

        async Task<IList<Claim>> IUserClaimStore<JoinIdentityUser, int>.GetClaimsAsync(JoinIdentityUser user)
        {
            var dbUser = await LoadUser(user);
            return dbUser.ToClaimsList();
        }

        Task IUserClaimStore<JoinIdentityUser, int>.AddClaimAsync(JoinIdentityUser user, Claim claim) =>
            throw new NotImplementedException();

        Task IUserClaimStore<JoinIdentityUser, int>.RemoveClaimAsync(JoinIdentityUser user, Claim claim) =>
            throw new NotImplementedException();

        async Task IUserSecurityStampStore<JoinIdentityUser, int>.SetSecurityStampAsync(JoinIdentityUser user, string stamp)
        {
            var dbUser = await LoadUser(user);
            if (dbUser == null)
            {
                return; // User not created yet, ignore
            }
            dbUser.Auth.AspNetSecurityStamp = stamp;
            await _ctx.SaveChangesAsync();
        }

        async Task<string> IUserSecurityStampStore<JoinIdentityUser, int>.GetSecurityStampAsync(JoinIdentityUser user)
        {
            var dbUser = await LoadUser(user);
            // if AspNetSecurityStamp setting random guid will make it refresh soonish
            return dbUser.Auth.AspNetSecurityStamp ?? new Guid().ToString();
            
        }
    }
}
