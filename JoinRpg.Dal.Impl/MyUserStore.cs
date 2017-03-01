using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.DataModel;
using Microsoft.AspNet.Identity;

namespace JoinRpg.Dal.Impl
{
  public class MyUserStore : 
    IUserPasswordStore<User, int>, 
    IUserLockoutStore<User, int>,
    IUserTwoFactorStore<User, int>, 
    IUserEmailStore<User, int>, 
    IUserLoginStore<User, int>, 
    IUserRoleStore<User, int>
  {
    private readonly MyDbContext _ctx;

    public MyUserStore(MyDbContext ctx)
    {
      _ctx = ctx;
    }

    public void Dispose()
    {
      _ctx?.Dispose();
    }

    public Task CreateAsync(User user)
    {
      if (user == null)
      {
        throw new ArgumentNullException(nameof(user));
      }
      user.Auth = user.Auth ?? new UserAuthDetails() {RegisterDate = DateTime.Now};

      _ctx.UserSet.Add(user);
      return _ctx.SaveChangesAsync();
    }

    public Task UpdateAsync(User user)
    {
      if (user == null)
      {
        throw new ArgumentNullException(nameof(user));
      }
      _ctx.UserSet.Attach(user);
      return _ctx.SaveChangesAsync();
    }

    public Task DeleteAsync(User user)
    {
      if (user == null)
      {
        throw new ArgumentNullException(nameof(user));
      }
      _ctx.UserSet.Remove(user);
      return _ctx.SaveChangesAsync();
    }

    public async Task<User> FindByIdAsync(int userId)
    {
      return await _ctx.UserSet.FindAsync(userId);
    }

    public async Task<User> FindByNameAsync(string userName)
    {
      return await _ctx.UserSet.SingleOrDefaultAsync(user => user.Email == userName);
    }

    public Task SetPasswordHashAsync(User user, string passwordHash)
    {
      if (user == null)
      {
        throw new ArgumentNullException(nameof(user));
      }
      user.PasswordHash = passwordHash;
      if (user.Allrpg != null)
      {
        //First time we change password, we should never ask allrpg.info for password
        user.Allrpg.PreventAllrpgPassword = true;
      }

      return SaveUserIfRequired(user);
    }

    private Task SaveUserIfRequired(User user)
    {
      var entry = _ctx.Entry(user);
      if (entry.State != EntityState.Detached)
      {
        return _ctx.SaveChangesAsync();
      }
      else
      {
        return Task.FromResult(0); //Will save anything on CreateUser()
      }
    }

    public Task<string> GetPasswordHashAsync(User user)
    {
      return Task.FromResult(user.PasswordHash);
    }

    public Task<bool> HasPasswordAsync(User user)
    {
      return Task.FromResult(user.PasswordHash != null);
    }

    public Task<DateTimeOffset> GetLockoutEndDateAsync(User user)
    {
      throw new NotImplementedException();
    }

    public Task SetLockoutEndDateAsync(User user, DateTimeOffset lockoutEnd)
    {
      throw new NotImplementedException();
    }

    public Task<int> IncrementAccessFailedCountAsync(User user)
    {
      throw new NotImplementedException();
    }

    public Task ResetAccessFailedCountAsync(User user)
    {
      return Task.FromResult<object>(null);
    }

    public Task<int> GetAccessFailedCountAsync(User user)
    {
      return Task.FromResult(0);
    }

    public Task<bool> GetLockoutEnabledAsync(User user)
    {
      return Task.FromResult(false);
    }

    public Task SetLockoutEnabledAsync(User user, bool enabled)
    {
      throw new NotImplementedException();
    }

    public Task SetTwoFactorEnabledAsync(User user, bool enabled)
    {
      throw new NotImplementedException();
    }

    public Task<bool> GetTwoFactorEnabledAsync(User user)
    {
      return Task.FromResult(false);
    }

    public Task SetEmailAsync(User user, string email)
    {
      user.Email = email;
      return Task.FromResult<object>(null);
    }

    public Task<string> GetEmailAsync(User user)
    {
      return Task.FromResult(user.Email);
    }

    public Task<bool> GetEmailConfirmedAsync(User user)
    {
      return Task.FromResult(user.Auth?.EmailConfirmed ?? false);
    }

    public Task SetEmailConfirmedAsync(User user, bool confirmed)
    {
      user.Auth = user.Auth ?? new UserAuthDetails() { RegisterDate = DateTime.Now };
      user.Auth.EmailConfirmed = confirmed;
      return Task.FromResult(0);
    }

    public Task<User> FindByEmailAsync(string email)
    {
      return _ctx.UserSet.SingleOrDefaultAsync(user => user.Email == email);
    }

    public Task AddLoginAsync(User user, UserLoginInfo login)
    {
      user.ExternalLogins.Add(new UserExternalLogin() {Key = login.ProviderKey, Provider = login.LoginProvider});
      return _ctx.SaveChangesAsync();
    }

    public Task RemoveLoginAsync(User user, UserLoginInfo login)
    {
      var el =
        user.ExternalLogins.First(
          externalLogin => externalLogin.Key == login.ProviderKey && externalLogin.Provider == login.LoginProvider);
      _ctx.Set<UserExternalLogin>().Remove(el);
      return Task.FromResult(0);
    }

    public async Task<IList<UserLoginInfo>> GetLoginsAsync(User user)
    {
      var result =
        await
          _ctx.Set<UserExternalLogin>()
            .Where(uel => uel.UserId == user.UserId)
            .ToListAsync();
      return result.Select(uel => new UserLoginInfo(uel.Provider, uel.Key)).ToList();
    }

    public async Task<User> FindAsync(UserLoginInfo login)
    {
      var uel =
        await _ctx.Set<UserExternalLogin>()
          .SingleOrDefaultAsync(u => u.Key == login.ProviderKey && u.Provider == login.LoginProvider);

      return uel?.User;
    }

    #region Implementation of IUserRoleStore<User,in int>

    public Task AddToRoleAsync(User user, string roleName)
    {
      //This is not managed via UserManager
      throw new NotSupportedException();
    }

    public Task RemoveFromRoleAsync(User user, string roleName)
    {
      //This is not managed via UserManager
      throw new NotSupportedException();
    }

    public Task<IList<string>> GetRolesAsync(User user)
    {
      List<string> list;
      if (user.Auth?.IsAdmin ?? false)
      {
        list = new List<string>() { Security.AdminRoleName };
      }
      else
      {
       list = new List<string>();
      }
      return Task.FromResult((IList<string>) list);
    }

    public async Task<bool> IsInRoleAsync(User user, string roleName)
    {
      var roles = await GetRolesAsync(user);
      return roles.Contains(roleName);
    }

    #endregion
  }
}