using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Dal.Impl;
using JoinRpg.DataModel;
using Microsoft.AspNet.Identity;
using DbUser = JoinRpg.DataModel.User;

namespace Joinrpg.Web.Identity
{
    public class MyUserStore :
        IUserPasswordStore<IdentityUser, int>,
        IUserLockoutStore<IdentityUser, int>,
        IUserTwoFactorStore<IdentityUser, int>,
        IUserEmailStore<IdentityUser, int>,
        IUserLoginStore<IdentityUser, int>,
        IUserRoleStore<IdentityUser, int>
    {
        private readonly MyDbContext _ctx;
        private readonly IDbSet<DbUser> UserSet;

        public MyUserStore(MyDbContext ctx)
        {
            _ctx = ctx;
            UserSet = _ctx.Set<User>();
        }

        public void Dispose() => _ctx?.Dispose();

        public async Task CreateAsync(IdentityUser user)
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

        public async Task UpdateAsync(IdentityUser user)
        {
            var dbUser = await LoadUser(user);
            dbUser.UserName = user.UserName;
            dbUser.Email = user.UserName;
            await _ctx.SaveChangesAsync();
        }

        public Task DeleteAsync(IdentityUser user) => throw new NotImplementedException();

        public async Task<IdentityUser> FindByIdAsync(int userId)
        {
            var dbUser = await LoadUser(userId);
            return dbUser.ToIdentityUser();
        }

        public async Task<IdentityUser> FindByNameAsync(string userName)
        {
            var dbUser = await LoadUser(userName);
            return dbUser.ToIdentityUser();
        }


        public async Task SetPasswordHashAsync(IdentityUser user, string passwordHash)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var dbUser = await LoadUser(user.UserName);

            dbUser.PasswordHash = passwordHash;

            await _ctx.SaveChangesAsync();
        }

        public async Task<string> GetPasswordHashAsync(IdentityUser user)
        {
            var dbUser = await LoadUser(user);
            return dbUser.PasswordHash;
        }

        public async Task<bool> HasPasswordAsync(IdentityUser user)
        {
            var dbUser = await LoadUser(user);
            return dbUser.PasswordHash != null;
        }

        public Task<DateTimeOffset> GetLockoutEndDateAsync(IdentityUser user) =>
            throw new NotImplementedException();

        public Task SetLockoutEndDateAsync(IdentityUser user, DateTimeOffset lockoutEnd) =>
            throw new NotImplementedException();

        public Task<int> IncrementAccessFailedCountAsync(IdentityUser user) =>
            throw new NotImplementedException();

        public Task ResetAccessFailedCountAsync(IdentityUser user) => Task.FromResult<object>(null);

        public Task<int> GetAccessFailedCountAsync(IdentityUser user) => Task.FromResult(0);

        public Task<bool> GetLockoutEnabledAsync(IdentityUser user) => Task.FromResult(false);

        public Task SetLockoutEnabledAsync(IdentityUser user, bool enabled) =>
            throw new NotImplementedException();

        public Task SetTwoFactorEnabledAsync(IdentityUser user, bool enabled) =>
            throw new NotImplementedException();

        public Task<bool> GetTwoFactorEnabledAsync(IdentityUser user) => Task.FromResult(false);

        public async Task SetEmailAsync(IdentityUser user, string email)
        {
            var dbUser = await LoadUser(user);
            dbUser.Email = email;
            dbUser.UserName = email;
            await _ctx.SaveChangesAsync();
        }

        public Task<string> GetEmailAsync(IdentityUser user)
        {
            return Task.FromResult(user.UserName);
        }

        public async Task<bool> GetEmailConfirmedAsync(IdentityUser user)
        {
            var dbUser = await LoadUser(user);
            return dbUser.Auth.EmailConfirmed;
        }

        public async Task SetEmailConfirmedAsync(IdentityUser user, bool confirmed)
        {
            var dbUser = await LoadUser(user);
            dbUser.Auth.EmailConfirmed = confirmed;
            await _ctx.SaveChangesAsync();
        }

        public async Task<IdentityUser> FindByEmailAsync(string email)
        {
            var user = await LoadUser(email);
            return user.ToIdentityUser();
        }

        public async Task AddLoginAsync(IdentityUser user, UserLoginInfo login)
        {
            var dbUser = await LoadUser(user);
            dbUser.ExternalLogins.Add(login.ToUserExternalLogin());
            await _ctx.SaveChangesAsync();
        }

        public async Task RemoveLoginAsync(IdentityUser user, UserLoginInfo login)
        {
            var dbUser = await LoadUser(user);
            var el =
                dbUser.ExternalLogins.First(
                    externalLogin => externalLogin.Key == login.ProviderKey &&
                                     externalLogin.Provider == login.LoginProvider);
            _ctx.Set<UserExternalLogin>().Remove(el);
            await _ctx.SaveChangesAsync();
        }

        public async Task<IList<UserLoginInfo>> GetLoginsAsync(IdentityUser user)
        {
            var dbUser = await LoadUser(user);
            return dbUser.ExternalLogins.Select(uel => uel.ToUserLoginInfo()).ToList();
        }

        public async Task<IdentityUser> FindAsync(UserLoginInfo login)
        {
            var uel =
                await _ctx.Set<UserExternalLogin>()
                    .SingleOrDefaultAsync(u =>
                        u.Key == login.ProviderKey && u.Provider == login.LoginProvider);

            return uel?.User.ToIdentityUser();
        }

        #region Implementation of IUserRoleStore<User,in int>

        public Task AddToRoleAsync(IdentityUser user, string roleName) =>
            throw new NotSupportedException();

        public Task RemoveFromRoleAsync(IdentityUser user, string roleName) =>
            throw new NotSupportedException();

        public async Task<IList<string>> GetRolesAsync(IdentityUser user)
        {
            var dbUser = await LoadUser(user);
            List<string> list;
            if (dbUser.Auth?.IsAdmin ?? false)
            {
                list = new List<string>() {Security.AdminRoleName};
            }
            else
            {
                list = new List<string>();
            }

            return list;
        }

        public async Task<bool> IsInRoleAsync(IdentityUser user, string roleName)
        {
            var roles = await GetRolesAsync(user);
            return roles.Contains(roleName);
        }

        #endregion

        private async Task<DbUser> LoadUser(string userName) =>
            await _ctx.UserSet.SingleOrDefaultAsync(user => user.Email == userName);

        private async Task<DbUser> LoadUser(int id) =>
            await _ctx.UserSet.SingleOrDefaultAsync(user => user.UserId == id);

        private async Task<DbUser> LoadUser(IdentityUser identityUser) =>
            await _ctx.UserSet.Include(u => u.ExternalLogins).SingleOrDefaultAsync(user => user.UserId == identityUser.Id);
    }
}
