using System.Diagnostics.CodeAnalysis;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models;


public abstract record ProviderDescViewModel(string ProviderId, string FriendlyName)
{
    public static readonly ProviderDescViewModel Vk = new VkDescViewModel();

    public static readonly ProviderDescViewModel Telegram = new TelegramDescViewModel();

    [return: NotNullIfNotNull(nameof(providerKey))]
    public abstract Uri? GetProfileUri(string? providerKey);
}

public record VkDescViewModel() : ProviderDescViewModel("Vkontakte", "ВК")
{
    [return: NotNullIfNotNull(nameof(providerKey))]
    public override Uri? GetProfileUri(string? providerKey) => providerKey is null ? null : new Uri($"https://vk.com/{providerKey}");
}

public record TelegramDescViewModel() : ProviderDescViewModel(ProviderId: "telegram", "Телеграм")
{
    [return: NotNullIfNotNull(nameof(providerKey))]
    public override Uri? GetProfileUri(string? providerKey) => providerKey is null ? null : new Uri($"https://t.me/{providerKey}");
}

public record UserLoginInfoViewModel
{
    public required ProviderDescViewModel LoginProvider { get; init; }

    public required Uri? ProviderLink { get; set; }

    public required string? ProviderKey { get; set; }

    public required bool AllowLink { get; set; }
    public required bool AllowUnlink { get; set; }
    public required bool NeedToReLink { get; set; }
}

public static class UserLoginInfoViewModelBuilder
{
    private static UserExternalLogin? TryGetExternalLoginByProviderId(User user, string providerId)
    {
        return user.ExternalLogins.SingleOrDefault(l => l.Provider.Equals(providerId, StringComparison.InvariantCultureIgnoreCase));
    }
    public static IEnumerable<UserLoginInfoViewModel> GetSocialLogins(this User user)
    {
        yield return GetModel(ProviderDescViewModel.Vk, user.Extra?.Vk);

        yield return GetModel(ProviderDescViewModel.Telegram, user.Extra?.Telegram);

        UserLoginInfoViewModel GetModel(ProviderDescViewModel provider, string? idFromProfile)
        {
            if (TryGetExternalLoginByProviderId(user, provider.ProviderId) is UserExternalLogin login)
            {
                return new UserLoginInfoViewModel()
                {
                    AllowLink = false,
                    AllowUnlink = user.PasswordHash != null || user.ExternalLogins.Count > 1,
                    LoginProvider = provider,
                    ProviderKey = login.Key,
                    NeedToReLink = false,
                    ProviderLink = provider.GetProfileUri(idFromProfile),
                };
            }
            else
            {
                return new UserLoginInfoViewModel()
                {
                    AllowLink = idFromProfile is null,
                    AllowUnlink = false,
                    LoginProvider = provider,
                    ProviderKey = null,
                    NeedToReLink = idFromProfile is not null,
                    ProviderLink = provider.GetProfileUri(idFromProfile),
                };
            }
        }
    }
}
