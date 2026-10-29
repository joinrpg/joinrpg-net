using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.AdminTools;

namespace Joinrpg.Web.Identity;

public class UserAdminViewService(
    JoinUserManager userManager,
    IUserService userService,
    IUserRepository userRepository,
    ExternalLoginProfileExtractor externalLoginProfileExtractor) : IUserAdminClient
{
    public async Task<UserAdminPanelViewModel> GetAdminPanel(UserIdentification userId)
    {
        var userInfo = await userRepository.GetRequiredUserInfo(userId);
        var hasVkLink = userInfo.Social.Vk is not null;
        var isVkOnlyLoginMethod = IsVkOnlyLoginMethod(userInfo);

        return new UserAdminPanelViewModel(userInfo.IsAdmin, userInfo.VerifiedProfileFlag, hasVkLink, isVkOnlyLoginMethod, userInfo.BirthDate);
    }

    public async Task RemoveVkLink(UserIdentification userId)
    {
        var userInfo = await userRepository.GetRequiredUserInfo(userId);
        if (IsVkOnlyLoginMethod(userInfo))
        {
            throw new InvalidOperationException("Невозможно удалить единственный способ входа");
        }

        var user = await userManager.FindRequiredByIdAsync(userId);
        // ВК мог быть привязан без записи в AspNetUserLogins (легаси-данные) —
        // RemoveLogin сам разберётся и в этом случае просто почистит профиль.
        _ = await externalLoginProfileExtractor.RemoveLogin(user, UserExternalLogin.VkProvider);
    }

    private static bool IsVkOnlyLoginMethod(UserInfo userInfo)
        => userInfo.Social.Vk?.CanLogin == true && userInfo.HasSingleLoginMethod;

    public async Task SetAdminFlag(UserIdentification userId, bool value)
    {
        await userService.SetAdminFlag(userId.Value, value);
        var user = await userManager.FindRequiredByIdAsync(userId);
        _ = await userManager.UpdateSecurityStampAsync(user);
    }

    public Task SetVerificationFlag(UserIdentification userId, bool value)
        => userService.SetVerificationFlag(userId.Value, value);

    public async Task ChangeEmail(UserIdentification userId, string newEmail)
    {
        // Email — это и имя пользователя в Identity, пробел в нём не пропустит валидатор (#5891).
        // Форма тоже обрезает, но на клиента не полагаемся. При регистрации адрес обрезается так же.
        newEmail = newEmail.Trim();
        if (newEmail.Length == 0)
        {
            throw new UserAdminActionRefusedException("Email не указан");
        }
        var user = await userManager.FindRequiredByIdAsync(userId);
        var token = await userManager.GenerateChangeEmailTokenAsync(user, newEmail);
        var result = await userManager.ChangeEmailAsync(user, newEmail, token);
        if (!result.Succeeded)
        {
            throw new UserAdminActionRefusedException(GetChangeEmailRefusal(result));
        }
    }

    private static string GetChangeEmailRefusal(IdentityResult result)
    {
        var codes = result.Errors.Select(e => e.Code).ToHashSet();
        if (codes.Contains(nameof(IdentityErrorDescriber.DuplicateUserName)) || codes.Contains(nameof(IdentityErrorDescriber.DuplicateEmail)))
        {
            return "Этот email уже используется другим пользователем";
        }
        if (codes.Contains(nameof(IdentityErrorDescriber.InvalidUserName)) || codes.Contains(nameof(IdentityErrorDescriber.InvalidEmail)))
        {
            return "Недопустимый email: можно использовать латинские буквы, цифры и символы - . _ @ +";
        }
        // Приставку «Не удалось изменить email» добавляет панель. Остальные коды сюда не должны
        // доходить, их описания от Identity — на английском.
        return string.Join("; ", result.Errors.Select(e => e.Description));
    }

    public Task SetBirthDate(UserIdentification userId, DateOnly? birthDate)
        => userService.SetBirthDate(userId, birthDate);
}
