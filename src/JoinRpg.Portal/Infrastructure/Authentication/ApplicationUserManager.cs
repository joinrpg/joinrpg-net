using Joinrpg.Web.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Identity;

public class ApplicationUserManager : UserManager<JoinIdentityUser>
{
    public ApplicationUserManager(IUserStore<JoinIdentityUser> store, IOptions<IdentityOptions> optionsAccessor, IPasswordHasher<JoinIdentityUser> passwordHasher, IEnumerable<IUserValidator<JoinIdentityUser>> userValidators, IEnumerable<IPasswordValidator<JoinIdentityUser>> passwordValidators, ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors, IServiceProvider services, ILogger<UserManager<JoinIdentityUser>> logger) : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
    {
    }
}
