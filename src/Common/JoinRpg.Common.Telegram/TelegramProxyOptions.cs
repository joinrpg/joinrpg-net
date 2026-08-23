namespace JoinRpg.Common.Telegram;

public class TelegramProxyOptions
{
    /// <summary>
    /// Адрес proxy в виде URI, например http://host:8080 или socks5://host:1080
    /// </summary>
    public required string Address { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}
