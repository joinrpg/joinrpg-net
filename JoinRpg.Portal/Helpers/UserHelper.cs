using System;
using System.Security.Claims;
using System.Threading.Tasks;
using JoinRpg.Portal.Identity;
using Joinrpg.Web.Identity;
using Microsoft.AspNet.Identity;
using IdentityResult = Microsoft.AspNet.Identity.IdentityResult;

namespace JoinRpg.Web.Helpers
{
    static class UserHelper
    {
        public static Task<ClaimsIdentity> GenerateUserIdentityAsync(this JoinIdentityUser user,
            UserManager<JoinIdentityUser, int> manager,
            string authenticationType)
        {
            return manager.ClaimsIdentityFactory.CreateAsync(manager, user, authenticationType);
        }

        public static async Task<IdentityResult> SetPasswordWithoutValidationAsync(
            this ApplicationUserManager applicationUserManager,
            int userId,
            string password)
        {
            throw new NotImplementedException();
            // var prevValidators = applicationUserManager.PasswordValidators.ToArray();
            // IdentityResult changePasswordResult;
            // try
            // {
            //     applicationUserManager.PasswordValidators.Clear();
            //     changePasswordResult =
            //         await applicationUserManager.AddPasswordAsync(userId, password);
            // }
            // finally
            // {
            //     foreach (var validator in prevValidators)
            //     {
            //         applicationUserManager.PasswordValidators.Add(validator);
            //     }
            //   
            // }
            //
            // return changePasswordResult;
        }
    }
}
