using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Dal.Impl;
using JoinRpg.DataModel;
using Microsoft.AspNet.Identity;
using DbUser = JoinRpg.DataModel.User;
using Claim = System.Security.Claims.Claim;

namespace Joinrpg.Web.Identity
{
    public class MyUserStore :
        IUserPasswordStore<JoinIdentityUser, int>,
        IUserLockoutStore<JoinIdentityUser, int>,
        IUserTwoFactorStore<JoinIdentityUser, int>,
        IUserEmailStore<JoinIdentityUser, int>,
        IUserLoginStore<JoinIdentityUser, int>,
        IUserRoleStore<JoinIdentityUser, int>,
        IUserClaimStore<JoinIdentityUser, int>,
        IUserSecurityStampStore<JoinIdentityUser, int>
    {
        private readonly MyDbContext _ctx;
        private readonly IDbSet<DbUser> UserSet;

        public MyUserStore(MyDbContext ctx)
        {
            _ctx = ctx;
            UserSet = _ctx.Set<DbUser>();
        }

        public void Dispose() => _ctx?.Dispose();

        public async Task CreateAsync(JoinIdentityUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var hasAnyUser = await UserSet.AnyAsync();

            var dbUser = new DbUser()
            {
                UserName = user.UserName,
                Email = user.UserName,
                Auth = new UserAuthDetails()
                {
                    RegisterDate = DateTime.UtcNow,
                },
            };

            if (!hasAnyUser)
            {
                dbUser.Auth.EmailConfirmed = true;
                dbUser.Auth.IsAdmin = true;
            }

            _ctx.UserSet.Add(dbUser);
            await _ctx.SaveChangesAsync();
            user.Id = dbUser.UserId;
        }

        /// <inheritdoc />
        public async Task UpdateAsync(JoinIdentityUser user)
        {
            var dbUser = await LoadUser(user);
            dbUser.UserName = user.UserName;
            dbUser.Email = user.UserName;
            await _ctx.SaveChangesAsync();
        }

        /// <inheritdoc />
        public Task DeleteAsync(JoinIdentityUser user) => throw new NotImplementedException();

        /// <inheritdoc />
        public async Task<JoinIdentityUser> FindByIdAsync(int userId)
        {
            var dbUser = await LoadUser(userId);
            return dbUser?.ToIdentityUser();
        }

        /// <inheritdoc />
        public async Task<JoinIdentityUser> FindByNameAsync(string userName)
        {
            var dbUser = await LoadUser(userName);
            return dbUser?.ToIdentityUser();
        }

        /// <inheritdoc />
        public async Task SetPasswordHashAsync(JoinIdentityUser user, string passwordHash)
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
            var dbUser = await LoadUser(user);
            dbUser.Email = email;
            dbUser.UserName = email;
            await _ctx.SaveChangesAsync();
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

        public Task AddToRoleAsync(JoinIdentityUser user, string roleName) =>
            throw new NotSupportedException();

        public Task RemoveFromRoleAsync(JoinIdentityUser user, string roleName) =>
            throw new NotSupportedException();

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

        [ItemCanBeNull]
        private async Task<DbUser> LoadUser(string userName) =>
            await _ctx.UserSet.SingleOrDefaultAsync(user => user.Email == userName);

        [ItemCanBeNull]
        private async Task<DbUser> LoadUser(int id) =>
            await _ctx.UserSet.SingleOrDefaultAsync(user => user.UserId == id);

        [ItemCanBeNull]
        private async Task<DbUser> LoadUser(JoinIdentityUser joinIdentityUser) =>
            await _ctx.UserSet.Include(u => u.ExternalLogins).SingleOrDefaultAsync(user => user.UserId == joinIdentityUser.Id);

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
