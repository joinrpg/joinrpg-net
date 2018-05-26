using System;
using System.Security.Claims;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Web.Helpers;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;

namespace JoinRpg.Web
{
  [UsedImplicitly]
  public class ApplicationUserManager : UserManager<User, int>
  {
    public ApplicationUserManager(IUserStore<User, int> store, IIdentityMessageService messageService)
      : base(store)
    {
      // Configure validation logic for usernames
      UserValidator = new UserValidator<User, int>(this)
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
        new DataProtectorTokenProvider<User, int>(
          Startup.DataProtectionProvider.Create("ASP.NET Identity"));

    }
  }

  [UsedImplicitly]
  public class ApplicationSignInManager : SignInManager<User, int>
  {
    public ApplicationSignInManager(ApplicationUserManager userManager,
      IAuthenticationManager authenticationManager)
      : base(userManager, authenticationManager)
    {
    }

    public override Task<ClaimsIdentity> CreateUserIdentityAsync(User user) => user
      .GenerateUserIdentityAsync(UserManager, AuthenticationType);
  }
}
