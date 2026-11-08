namespace JoinRpg.XGameApi.Contract;

/// <summary>
/// Player contacts
/// </summary>
public record PlayerContacts(string Email, string? Phone, string? VKontakte, string? Telegram, long? TelegramChatId = null)
{
    /// <summary>
    /// Email (verified, always exists)
    /// </summary>
    public string Email { get; set; } = Email;

    /// <summary>
    /// Phone (not verified, just as entered by user)
    /// </summary>
    public string? Phone { get; set; } = Phone;

    /// <summary>
    /// VKontakte (verified). If not verified, will be returned null.
    /// </summary>
    public string? VKontakte { get; set; } = VKontakte;

    /// <summary>
    /// Telegram (not verified, just as entered by user)
    /// </summary>
    public string? Telegram { get; set; } = Telegram?.TrimStart('@');

    /// <summary>
    /// Telegram ChatId (verified). Filled only when the user has a linked Telegram login; a bot can write to this chat directly
    /// </summary>
    public long? TelegramChatId { get; set; } = TelegramChatId;
}
