using JoinRpg.Data.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Joinrpg.Web.Identity;

public class JoinUserManager(IUserStore<JoinIdentityUser> store, IOptions<IdentityOptions> optionsAccessor, IPasswordHasher<JoinIdentityUser> passwordHasher, IEnumerable<IUserValidator<JoinIdentityUser>> userValidators, IEnumerable<IPasswordValidator<JoinIdentityUser>> passwordValidators, ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors, IServiceProvider services, ILogger<JoinUserManager> logger) : UserManager<JoinIdentityUser>(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
{
    /// <summary>
    /// Находит пользователя по типизированному id и падает, если его нет.
    /// Базовый <see cref="UserManager{TUser}.FindByIdAsync(string)" /> возвращает null,
    /// а все вызывающие рассчитывают на существующего пользователя: id приходит либо из
    /// текущей сессии, либо из админки по уже существующей записи.
    /// </summary>
    public async Task<JoinIdentityUser> FindRequiredByIdAsync(UserIdentification userId)
        => await FindByIdAsync(userId.Value.ToString())
            ?? throw new JoinRpgEntityNotFoundException(userId.Value, nameof(JoinIdentityUser));
}
