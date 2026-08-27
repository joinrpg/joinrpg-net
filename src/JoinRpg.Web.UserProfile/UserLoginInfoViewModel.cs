using System.Diagnostics.CodeAnalysis;

namespace JoinRpg.Web.UserProfile;


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

    /// <summary>
    /// True когда привязка есть, но отвязать её нельзя, т.к. это единственный способ входа в аккаунт.
    /// </summary>
    public required bool IsOnlyLoginMethod { get; set; }
}
