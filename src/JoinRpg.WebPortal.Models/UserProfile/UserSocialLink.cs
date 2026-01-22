namespace JoinRpg.Web.Models.UserProfile;

internal class UserSocialLink
{
    public static Link? GetVKUri(string? vk) => string.IsNullOrWhiteSpace(vk) ? null : new Link(new Uri($"https://vk.com/{vk}"), $"vk.com/{vk}");

    public static Link? GetTelegramUri(string? telegram) => string.IsNullOrWhiteSpace(telegram) ? null : new Link(new Uri($"https://t.me/{telegram.TrimStart('@')}"), $"t.me/{telegram.TrimStart('@')}");

    public static Link? GetEmailUri(string? email) => string.IsNullOrWhiteSpace(email) ? null : new Link(new Uri($"mailto:{email}"), email);
}

public record Link(Uri Uri, string Label);
