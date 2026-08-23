using System.Text.Json.Serialization;

namespace JoinRpg.Common.PrimitiveTypes;

/// <summary>
/// Числовой идентификатор адресата в Telegram Bot API (chat_id). Bot API не различает личные чаты,
/// группы и каналы — везде один и тот же числовой id, который может быть отрицательным (супергруппы/каналы).
/// </summary>
[method: JsonConstructor]
[TypedEntityId(AllowNonPositive = true, AdditionalPrefixes = ["Telegram", "TelegramId"])]
public partial record TelegramChatId(long Value)
{
}
