namespace JoinRpg.Common.Telegram;

public enum TelegramAuthorizationResult
{
    InvalidHash,
    MissingFields,
    InvalidAuthDateFormat,
    TooOld,
    Valid
}
