using JoinRpg.DomainTypes;
using JoinRpg.Services.Interfaces.Notification;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Joinrpg.Web.Identity;

public class JoinUserManager(IUserStore<JoinIdentityUser> store, IOptions<IdentityOptions> optionsAccessor, IPasswordHasher<JoinIdentityUser> passwordHasher, IEnumerable<IUserValidator<JoinIdentityUser>> userValidators, IEnumerable<IPasswordValidator<JoinIdentityUser>> passwordValidators, ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors, IServiceProvider services, ILogger<JoinUserManager> logger, IAccountEmailService<JoinIdentityUser> accountEmailService) : UserManager<JoinIdentityUser>(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
{
    // Письмо о смене пароля шлём здесь, а не в вызывающих: пароль меняют и Portal, и IdPortal,
    // через смену, установку первого пароля и сброс по ссылке — так ни один путь не забудет (#1092).
    // Регистрация (CreateAsync) и перехеширование при входе сюда не попадают — там пароль не меняется.

    public override async Task<IdentityResult> ChangePasswordAsync(JoinIdentityUser user, string currentPassword, string newPassword)
        => await NotifyIfSucceeded(user, await base.ChangePasswordAsync(user, currentPassword, newPassword));

    public override async Task<IdentityResult> AddPasswordAsync(JoinIdentityUser user, string password)
        => await NotifyIfSucceeded(user, await base.AddPasswordAsync(user, password));

    public override async Task<IdentityResult> ResetPasswordAsync(JoinIdentityUser user, string token, string newPassword)
        => await NotifyIfSucceeded(user, await base.ResetPasswordAsync(user, token, newPassword));

    private async Task<IdentityResult> NotifyIfSucceeded(JoinIdentityUser user, IdentityResult result)
    {
        if (result.Succeeded)
        {
            await accountEmailService.PasswordChangedEmail(user);
        }
        return result;
    }

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
