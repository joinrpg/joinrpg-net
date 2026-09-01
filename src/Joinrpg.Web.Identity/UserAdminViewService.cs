using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
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
        var user = await userRepository.GetRequiredUserInfo(userId);
        return new UserAdminPanelViewModel(user.IsAdmin, user.VerifiedProfileFlag, user.Social.Vk is not null);
    }

    public async Task RemoveVkLink(UserIdentification userId)
    {
        var user = await userManager.FindByIdAsync(userId.Value.ToString());
        var logins = await userManager.GetLoginsAsync(user);
        var vkLogin = logins.FirstOrDefault(l =>
            string.Equals(l.LoginProvider, UserExternalLogin.VkProvider, StringComparison.OrdinalIgnoreCase));
        if (vkLogin is not null)
        {
            await externalLoginProfileExtractor.RemoveLogin(user, vkLogin.LoginProvider, vkLogin.ProviderKey);
        }
        else
        {
            // ВК мог быть привязан без записи в AspNetUserLogins (легаси-данные) — чистим профиль напрямую.
            await userService.RemoveVkFromProfile(userId);
        }
    }

    public async Task SetAdminFlag(UserIdentification userId, bool value)
    {
        await userService.SetAdminFlag(userId.Value, value);
        var user = await userManager.FindByIdAsync(userId.Value.ToString());
        _ = await userManager.UpdateSecurityStampAsync(user);
    }

    public Task SetVerificationFlag(UserIdentification userId, bool value)
        => userService.SetVerificationFlag(userId.Value, value);

    public async Task ChangeEmail(UserIdentification userId, string newEmail)
    {
        var user = await userManager.FindByIdAsync(userId.Value.ToString());
        var token = await userManager.GenerateChangeEmailTokenAsync(user, newEmail);
        var result = await userManager.ChangeEmailAsync(user, newEmail, token);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Не удалось изменить email: {result}");
        }
    }
}
