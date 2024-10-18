using System.Security.Claims;
using AspNet.Security.OAuth.Vkontakte;
using Joinrpg.Web.Identity;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace JoinRpg.Portal.Infrastructure.Authentication;

/// <summary>
/// Task of this class is to extract useful data from social logins
/// </summary>
public class ExternalLoginProfileExtractor
{
    private readonly IUserService userService;

    public ExternalLoginProfileExtractor(IUserService userService) => this.userService = userService;

    public async Task TryExtractProfile(JoinIdentityUser user, ExternalLoginInfo loginInfo)
    {
        UserFullName userFullName = TryGetUserName(loginInfo);
        await userService.SetNameIfNotSetWithoutAccessChecks(user.Id, userFullName);

        if (TryGetVkId(loginInfo) is VkId vkId)
        {
            var vkAvatar = loginInfo.Principal.FindFirstValue(VkontakteAuthenticationConstants.Claims.PhotoUrl);

            if (vkAvatar is not null)
            {
                await userService.SetVkIfNotSetWithoutAccessChecks(user.Id, vkId,
                  new AvatarInfo(new Uri(vkAvatar), 50, 50));
            }
        }
    }

    public async Task TryExtractTelegramProfile(JoinIdentityUser user, Dictionary<string, string> loginInfo)
    {
        var bornName = BornName.FromOptional(loginInfo.GetValueOrDefault("first_name"));
        var surName = SurName.FromOptional(loginInfo.GetValueOrDefault("last_name"));
        var prefferedName = PrefferedName.FromOptional(loginInfo.GetValueOrDefault("username"));

        var userFullName = new UserFullName(prefferedName, bornName, surName, FatherName: null);


        await userService.SetNameIfNotSetWithoutAccessChecks(user.Id, userFullName);

        var avatar = loginInfo["photo_url"];

        await userService.SetTelegramIfNotSetWithoutAccessChecks(user.Id, new TelegramId(long.Parse(loginInfo["id"]), prefferedName), new AvatarInfo(new Uri(avatar), 50, 50));
    }

    private static UserFullName TryGetUserName(ExternalLoginInfo loginInfo)
    {
        var bornName = BornName.FromOptional(loginInfo.Principal.FindFirstValue(ClaimTypes.GivenName));
        var surName = SurName.FromOptional(loginInfo.Principal.FindFirstValue(ClaimTypes.Surname));
        var prefferedName = new PrefferedName(loginInfo.Principal.FindFirstValue(ClaimTypes.Name)!);

        return new UserFullName(prefferedName, bornName, surName, FatherName: null);
    }

    private static VkId? TryGetVkId(ExternalLoginInfo loginInfo)
    {
        return loginInfo.LoginProvider == ProviderDescViewModel.Vk.ProviderId
            && loginInfo.Principal.FindFirstValue(ClaimTypes.NameIdentifier) is string id
            ? new VkId(id)
            : null;
    }

    internal async Task CleanAfterLogin(JoinIdentityUser user, string loginProvider)
    {
        if (loginProvider == "Vkontakte")
        {
            await userService.RemoveVkFromProfile(user.Id);
        }
        else if (loginProvider == "telegram")
        {
            await userService.RemoveTelegramFromProfile(user.Id);
        }
    }
}
