using System;
using System.Data.Entity;
using System.Threading.Tasks;
using JoinRpg.DataModel;
using Microsoft.AspNet.Identity;

namespace JoinRpg.Dal.Impl
{
  public class MyUserStore : IUserStore<User, int>, IUserPasswordStore<User, int>, IUserLockoutStore<User,int>, IUserTwoFactorStore<User,int>, IUserEmailStore<User, int>
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
        throw  new ArgumentNullException(nameof(user));
      }
      _ctx.UserSet.Add(user);
      _ctx.SaveChanges();
      return Task.FromResult<object>(null);
    }

    public Task UpdateAsync(User user)
    {
      if (user == null)
      {
        throw new ArgumentNullException(nameof(user));
      }
      _ctx.UserSet.Attach(user);
      _ctx.SaveChanges();
      return Task.FromResult<object>(null);
    }

    public Task DeleteAsync(User user)
    {
      if (user == null)
      {
        throw new ArgumentNullException(nameof(user));
      }
      _ctx.UserSet.Remove(user);
      return Task.FromResult<object>(null);
    }

    public async Task<User> FindByIdAsync(int userId)
    {
      return await _ctx.UserSet.FindAsync(userId);
    }

    public async Task<User> FindByNameAsync(string userName)
    {
      return await _ctx.UserSet.SingleOrDefaultAsync(user => user.UserName == userName);
    }

    public Task SetPasswordHashAsync(User user, string passwordHash)
    {
      if (user == null)
      {
        throw new ArgumentNullException(nameof(user));
      }
      user.PasswordHash = passwordHash;
      return Task.FromResult<object>(null);
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
      return Task.FromResult(false);//We don't support confirming yet
    }

    public Task SetEmailConfirmedAsync(User user, bool confirmed)
    {
      throw new NotImplementedException();
    }

    public Task<User> FindByEmailAsync(string email)
    {
      return _ctx.UserSet.SingleOrDefaultAsync(user => user.Email == email);
    }
  }
}
