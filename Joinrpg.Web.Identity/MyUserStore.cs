using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Dal.Impl;
using JoinRpg.DataModel;
using Microsoft.AspNet.Identity;
using Claim = System.Security.Claims.Claim;
using DbUser = JoinRpg.DataModel.User;

namespace Joinrpg.Web.Identity
{
    [UsedImplicitly]
    public class MyUserStore :
        IUserPasswordStore<JoinIdentityUser, int>,
        IUserLockoutStore<JoinIdentityUser, int>,
        IUserTwoFactorStore<JoinIdentityUser, int>,
        IUserEmailStore<JoinIdentityUser, int>,
        IUserLoginStore<JoinIdentityUser, int>,
        IUserRoleStore<JoinIdentityUser, int>,
        IUserClaimStore<JoinIdentityUser, int>
    {
        private readonly MyDbContext _ctx;
        private readonly IDbSet<DbUser> _userSet;

        public MyUserStore(MyDbContext ctx)
        {
            _ctx = ctx;
            _userSet = _ctx.Set<User>();
        }

        public void Dispose() => _ctx?.Dispose();

        public async Task CreateAsync(JoinIdentityUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var hasAnyUser = await _userSet.AnyAsync();

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

        public async Task UpdateAsync(JoinIdentityUser user)
        {
            var dbUser = await LoadUser(user);
            dbUser.UserName = user.UserName;
            dbUser.Email = user.UserName;
            await _ctx.SaveChangesAsync();
        }

        public Task DeleteAsync(JoinIdentityUser user) => throw new NotImplementedException();

        public async Task<JoinIdentityUser> FindByIdAsync(int userId)
        {
            var dbUser = await LoadUser(userId);
            return dbUser.ToIdentityUser();
        }

        public async Task<JoinIdentityUser> FindByNameAsync(string userName)
        {
            var dbUser = await LoadUser(userName);
            return dbUser?.ToIdentityUser();
        }


        public async Task SetPasswordHashAsync(JoinIdentityUser user, string passwordHash)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var dbUser = await LoadUser(user.UserName);

            if (dbUser == null)
            {
                throw new InvalidOperationException();
            }

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

        public Task<string> GetEmailAsync(JoinIdentityUser user)
        {
            return Task.FromResult(user.UserName);
        }

        public async Task<bool> GetEmailConfirmedAsync(JoinIdentityUser user)
        {
            var dbUser = await LoadUser(user);
            return dbUser.Auth.EmailConfirmed;
        }

        public async Task SetEmailConfirmedAsync(JoinIdentityUser user, bool confirmed)
        {
            var dbUser = await LoadUser(user);
            dbUser.Auth.EmailConfirmed = confirmed;
            await _ctx.SaveChangesAsync();
        }

        public async Task<JoinIdentityUser> FindByEmailAsync(string email)
        {
            var user = await LoadUser(email);
            return user?.ToIdentityUser();
        }

        public async Task AddLoginAsync(JoinIdentityUser user, UserLoginInfo login)
        {
            var dbUser = await LoadUser(user);
            dbUser.ExternalLogins.Add(login.ToUserExternalLogin());
            await _ctx.SaveChangesAsync();
        }

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

        public async Task<IList<UserLoginInfo>> GetLoginsAsync(JoinIdentityUser user)
        {
            var dbUser = await LoadUser(user);
            return dbUser.ExternalLogins.Select(uel => uel.ToUserLoginInfo()).ToList();
        }

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
            var list = dbUser.Auth.IsAdmin ? new List<string>() {Security.AdminRoleName} : new List<string>();

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

        [ItemNotNull]
        private async Task<DbUser> LoadUser(int id) =>
            await _ctx.UserSet.SingleAsync(user => user.UserId == id);

        [ItemNotNull]
        private async Task<DbUser> LoadUser(JoinIdentityUser joinIdentityUser) =>
            await _ctx.UserSet.Include(u => u.ExternalLogins).SingleAsync(user => user.UserId == joinIdentityUser.Id);

        public async Task<IList<Claim>> GetClaimsAsync(JoinIdentityUser user)
        {
            var dbUser = await LoadUser(user);
            return dbUser.ToClaimsList();
        }

        public Task AddClaimAsync(JoinIdentityUser user, Claim claim) => throw new NotImplementedException();

        public Task RemoveClaimAsync(JoinIdentityUser user, Claim claim) => throw new NotImplementedException();
    }
}
