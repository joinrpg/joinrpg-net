namespace JoinRpg.Web.UserProfile;

public sealed record ProviderDescViewModel(string ProviderId, string FriendlyName)
{
    public static readonly ProviderDescViewModel Vk = new("Vkontakte", "ВК");

    public static readonly ProviderDescViewModel Telegram = new("telegram", "Телеграм");
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
