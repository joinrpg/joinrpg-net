using System;
using System.Security.Claims;
using System.Threading.Tasks;
using JoinRpg.Dal.Impl;
using JoinRpg.DataModel;
using JoinRpg.Web.Helpers;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;

namespace JoinRpg.Web
{
  // Configure the application user manager used in this application. UserManager is defined in ASP.NET Identity and is used by the application.
    public class ApplicationUserManager : UserManager<User, int>
    {
      internal ApplicationUserManager(IUserStore<User, int> store)
            : base(store)
        {
        }

        public static ApplicationUserManager Create(IdentityFactoryOptions<ApplicationUserManager> options, IOwinContext context) 
        {
            //TODO[DI]: Fix this to use MyUserStore from DI container
            var manager = new ApplicationUserManager(new MyUserStore(context.Get<MyDbContext>()));
            // Configure validation logic for usernames
            manager.UserValidator = new UserValidator<User, int>(manager)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true
            };

            // Configure validation logic for passwords
            manager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 6,
                RequireNonLetterOrDigit = false,
                RequireDigit = false,
                RequireLowercase = false,
                RequireUppercase = false,
            };

            // Configure user lockout defaults
            manager.UserLockoutEnabledByDefault = false;
            manager.DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes(5);
            manager.MaxFailedAccessAttemptsBeforeLockout = 5;

            manager.EmailService = new EmailService(new ApiSecretsStorage());



            //manager.SmsService = new SmsService();
            var dataProtectionProvider = options.DataProtectionProvider;
            
                manager.UserTokenProvider = 
                    new DataProtectorTokenProvider<User,int>(dataProtectionProvider.Create("ASP.NET Identity"));
            
            return manager;
        }
    }

    // Configure the application sign-in manager which is used in this application.
    public class ApplicationSignInManager : SignInManager<User, int>
    {
        public ApplicationSignInManager(ApplicationUserManager userManager, IAuthenticationManager authenticationManager)
            : base(userManager, authenticationManager)
        {
        }

      public override Task<ClaimsIdentity> CreateUserIdentityAsync(User user) => user
        .GenerateUserIdentityAsync(UserManager, AuthenticationType);

      public static ApplicationSignInManager Create(
        IdentityFactoryOptions<ApplicationSignInManager> options, IOwinContext context) => new
        ApplicationSignInManager(context.GetUserManager<ApplicationUserManager>(),
          context.Authentication);
    }
}
