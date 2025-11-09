namespace JoinRpg.Common.Telegram;
public class TelegramLoginOptions
{
    public required string BotName { get; set; }
    public required long? BotId { get; set; }
    public required string BotSecret { get; set; }

    /// <summary>
    /// How old (in seconds) can authorization attempts be to be considered valid (compared to the auth_date field)
    /// </summary>
    public TimeSpan AllowedTimeOffset { get; set; } = TimeSpan.FromSeconds(30);
}
