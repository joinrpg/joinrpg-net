using System.Security.Claims;
using System.Threading.Tasks;
using JoinRpg.DataModel;
using Microsoft.AspNet.Identity;

namespace JoinRpg.Web.Helpers
{
  static class UserHelper
  {
    public static Task<ClaimsIdentity> GenerateUserIdentityAsync(this User user, UserManager<User, int> manager, string authenticationType)
    {
      return manager.ClaimsIdentityFactory.CreateAsync(manager, user, authenticationType);
    }

    public static async Task<IdentityResult> SetPasswordWithoutValidationAsync(this ApplicationUserManager applicationUserManager, int userId, string password)
    {
      var prevValidator = applicationUserManager.PasswordValidator;
      IdentityResult changePasswordResult;
      try
      {
        applicationUserManager.PasswordValidator = new PasswordValidator();
        changePasswordResult = await applicationUserManager.AddPasswordAsync(userId, password);
      }
      finally
      {
        applicationUserManager.PasswordValidator = prevValidator;
      }
      return changePasswordResult;
    }
  }
}
