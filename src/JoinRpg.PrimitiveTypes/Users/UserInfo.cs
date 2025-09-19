namespace JoinRpg.PrimitiveTypes.Users;
public record class UserInfo(UserIdentification UserIdentification, UserDisplayName Name, UserSocialNetworks Social)
{
    public UserNotificationSettings NotificationSettings { get; } = new UserNotificationSettings(true);//TODO из базы грузить
}

public record class UserSocialNetworks(TelegramId? TelegramId);
public record class UserNotificationSettings(bool TelegramDigestEnabled);
