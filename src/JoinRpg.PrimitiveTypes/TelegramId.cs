
namespace JoinRpg.PrimitiveTypes;

public record TelegramId(long Id, PrefferedName? UserName)
{
    public static TelegramId? FromOptional(long? id, PrefferedName? userName) => id is null ? null : new TelegramId(id.Value, userName);

    public static TelegramId? FromOptional(string? key, PrefferedName? userName) => string.IsNullOrWhiteSpace(key) ? null : new TelegramId(long.Parse(key), userName);

    public override string ToString() => UserName is null ? $"Telegram({Id})" : $"Telegram({Id}, @{UserName.Value})";
}
