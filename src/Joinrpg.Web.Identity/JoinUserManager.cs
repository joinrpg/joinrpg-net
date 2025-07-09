using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Joinrpg.Web.Identity;

public class JoinUserManager(IUserStore<JoinIdentityUser> store, IOptions<IdentityOptions> optionsAccessor, IPasswordHasher<JoinIdentityUser> passwordHasher, IEnumerable<IUserValidator<JoinIdentityUser>> userValidators, IEnumerable<IPasswordValidator<JoinIdentityUser>> passwordValidators, ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors, IServiceProvider services, ILogger<JoinUserManager> logger) : UserManager<JoinIdentityUser>(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
{
}
