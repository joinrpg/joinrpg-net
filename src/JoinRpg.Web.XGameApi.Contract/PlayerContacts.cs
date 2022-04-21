namespace JoinRpg.Web.XGameApi.Contract;

#nullable enable
/// <summary>
/// Player contacts
/// </summary>
public record PlayerContacts(string Email, string? Phone, string? VKontakte, string? Telegram)
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
    public string? Telegram { get; set; } = Telegram;
}
