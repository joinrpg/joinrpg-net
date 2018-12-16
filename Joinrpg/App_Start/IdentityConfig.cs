using System;
using System.Security.Claims;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Joinrpg.Web.Identity;
using JoinRpg.Web.Helpers;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;

namespace JoinRpg.Web
{
    [UsedImplicitly]
  public class ApplicationUserManager : UserManager<JoinIdentityUser, int>
  {
    public ApplicationUserManager(IUserStore<JoinIdentityUser, int> store, IIdentityMessageService messageService)
      : base(store)
    {
      // Configure validation logic for usernames
      UserValidator = new UserValidator<JoinIdentityUser, int>(this)
      {
        AllowOnlyAlphanumericUserNames = false,
        RequireUniqueEmail = true,
      };

      // Configure validation logic for passwords
      PasswordValidator = new PasswordValidator
      {
        RequiredLength = 6,
        RequireNonLetterOrDigit = false,
        RequireDigit = false,
        RequireLowercase = false,
        RequireUppercase = false,
      };

      // Configure user lockout defaults
      UserLockoutEnabledByDefault = false;
      DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes(5);
      MaxFailedAccessAttemptsBeforeLockout = 5;

      EmailService = messageService;

      UserTokenProvider =
        new DataProtectorTokenProvider<JoinIdentityUser, int>(
          Startup.DataProtectionProvider.Create("ASP.NET Identity"));

    }
  }

    [UsedImplicitly]
    public class ApplicationSignInManager : SignInManager<JoinIdentityUser, int>
    {
        public ApplicationSignInManager(ApplicationUserManager userManager,
            IAuthenticationManager authenticationManager)
            : base(userManager, authenticationManager)
        {
        }

        public override Task<ClaimsIdentity> CreateUserIdentityAsync(JoinIdentityUser user) => user
            .GenerateUserIdentityAsync(UserManager, AuthenticationType);

        public async Task ReLoginUser(int userId)
        {
            var user = await UserManager.FindByIdAsync(userId);
            await SignInAsync(user, isPersistent: true, rememberBrowser: false);
        }
    }
}
