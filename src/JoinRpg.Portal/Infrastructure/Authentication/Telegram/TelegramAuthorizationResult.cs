namespace JoinRpg.Portal.Infrastructure.Authentication.Telegram;

public enum TelegramAuthorizationResult
{
    InvalidHash,
    MissingFields,
    InvalidAuthDateFormat,
    TooOld,
    Valid
}
