using System.Security.Claims;
using AspNet.Security.OAuth.Vkontakte;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Joinrpg.Web.Identity;

/// <summary>
/// Task of this class is to extract useful data from social logins
/// </summary>
public class ExternalLoginProfileExtractor(IUserService userService, JoinUserManager userManager)
{
    /// <summary>
    /// Removes external login and cleans up profile fields populated from it.
    /// Shared by self-service (<c>ManageController.RemoveLogin</c>) and admin (<c>UserAdminService.RemoveVkLink</c>) unlink paths.
    /// </summary>
    public async Task<IdentityResult> RemoveLogin(JoinIdentityUser user, string loginProvider, string? providerKey)
    {
        // Пустой providerKey означает, что настоящей привязки (ExternalLogin) нет — это
        // legacy-контакт без подтверждения (см. UserLoginInfoViewModelBuilder), для него нечего
        // отвязывать через RemoveLoginAsync, только очистить legacy-поле.
        var result = string.IsNullOrEmpty(providerKey)
            ? IdentityResult.Success
            : await userManager.RemoveLoginAsync(user, loginProvider, providerKey);
        if (result.Succeeded)
        {
            await CleanAfterLogin(user, loginProvider);
        }
        return result;
    }

    public async Task TryExtractProfile(JoinIdentityUser user, ExternalLoginInfo loginInfo)
    {
        UserFullName userFullName = TryGetUserName(loginInfo);
        await userService.SetNameIfNotSetWithoutAccessChecks(user.Id, userFullName);

        if (TryGetVkId(loginInfo) is VkSocialLink vk)
        {
            var avatar = AvatarInfo.FromOptional(loginInfo.Principal.FindFirstValue(VkontakteAuthenticationConstants.Claims.PhotoUrl));

            await userService.SetVkIfNotSetWithoutAccessChecks(user.Id, vk, avatar);
        }
    }

    public async Task TryExtractTelegramProfile(JoinIdentityUser user, Dictionary<string, string> loginInfo)
    {
        var bornName = BornName.FromOptional(loginInfo.GetValueOrDefault("first_name"));
        var surName = SurName.FromOptional(loginInfo.GetValueOrDefault("last_name"));
        var prefferedName = PrefferedName.FromOptional(loginInfo.GetValueOrDefault("username"));

        var userFullName = new UserFullName(prefferedName, bornName, surName, FatherName: null);


        await userService.SetNameIfNotSetWithoutAccessChecks(user.Id, userFullName);

        var avatar = AvatarInfo.FromOptional(loginInfo.GetValueOrDefault("photo_url"));

        var telegram = new TelegramSocialLink(new TelegramChatId(long.Parse(loginInfo["id"])), prefferedName, isVerified: true);

        await userService.SetTelegramIfNotSetWithoutAccessChecks(user.Id, telegram, avatar);
    }

    private static UserFullName TryGetUserName(ExternalLoginInfo loginInfo)
    {
        var bornName = BornName.FromOptional(loginInfo.Principal.FindFirstValue(ClaimTypes.GivenName));
        var surName = SurName.FromOptional(loginInfo.Principal.FindFirstValue(ClaimTypes.Surname));
        var prefferedName = new PrefferedName(loginInfo.Principal.FindFirstValue(ClaimTypes.Name)!);

        return new UserFullName(prefferedName, bornName, surName, FatherName: null);
    }

    private static VkSocialLink? TryGetVkId(ExternalLoginInfo loginInfo)
    {
        return loginInfo.LoginProvider == UserExternalLogin.VkProvider
            && loginInfo.Principal.FindFirstValue(ClaimTypes.NameIdentifier) is string id
            && long.TryParse(id, out var vkId)
            ? new VkSocialLink(vkId, isVerified: true)
            : null;
    }

    public async Task CleanAfterLogin(JoinIdentityUser user, string loginProvider)
    {
        if (loginProvider == UserExternalLogin.VkProvider)
        {
            await userService.RemoveVkFromProfile(new UserIdentification(user.Id));
        }
        else if (loginProvider == "telegram")
        {
            await userService.RemoveTelegramFromProfile(user.Id);
        }
    }
}
