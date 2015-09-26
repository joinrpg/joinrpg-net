using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Threading.Tasks;
using JoinRpg.DataModel;
using Microsoft.AspNet.Identity;

namespace JoinRpg.Dal.Impl
{
  public class MyUserStore : 
    IUserStore<User, int>, IUserPasswordStore<User, int>, IUserLockoutStore<User, int>,IUserTwoFactorStore<User, int>, IUserEmailStore<User, int>, IUserLoginStore<User, int>
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
      var entry = _ctx.Entry(user);
      //if (entry.State == )
      //_ctx.UserSet.Attach(user);
      user.PasswordHash = passwordHash;
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

    //External logins are not implemented yet

    public Task AddLoginAsync(User user, UserLoginInfo login)
    {
      throw new NotImplementedException();
    }

    public Task RemoveLoginAsync(User user, UserLoginInfo login)
    {
      throw new NotImplementedException();
    }

    public Task<IList<UserLoginInfo>> GetLoginsAsync(User user)
    {
      return Task.FromResult<IList<UserLoginInfo>>(new UserLoginInfo[] {});
    }

    public Task<User> FindAsync(UserLoginInfo login)
    {
      throw new NotImplementedException();
    }
  }
}