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
    public override Uri? GetProfileUri(string? providerKey) => providerKey is null ? null : new Uri($"https://vk.com/id{providerKey}");
}

public record TelegramDescViewModel() : ProviderDescViewModel(ProviderId: "telegram", "Телеграм")
{
    [return: NotNullIfNotNull(nameof(providerKey))]
    public override Uri? GetProfileUri(string? providerKey) => providerKey is null ? null : new Uri($"https://t.me/{providerKey}");
}

public record UserLoginInfoViewModel
{
    public required ProviderDescViewModel LoginProvider { get; init; }

    public Uri? ProviderLink { get; set; }

    public string? ProviderKey { get; set; }

    public bool AllowLink { get; set; }
    public bool AllowUnlink { get; set; }
    public bool NeedToReLink { get; set; }
    public bool Present => ProviderLink is not null && ProviderKey is not null;
}

public static class UserLoginInfoViewModelBuilder
{
    public static IEnumerable<UserLoginInfoViewModel> GetSocialLogins(this User user)
    {
        yield return GetModel(ProviderDescViewModel.Vk, user.Extra?.Vk);

        yield return GetModel(ProviderDescViewModel.Telegram, user.Extra?.Telegram);

        UserLoginInfoViewModel GetModel(ProviderDescViewModel provider, string? idFromProfile)
        {
            if (user.ExternalLogins.SingleOrDefault(l =>
                l.Provider.Equals(provider.ProviderId, StringComparison.InvariantCultureIgnoreCase)
                ) is UserExternalLogin login)
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
