using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Data.Interfaces;

public class UserNotificationInfoDto
{
    public required UserIdentification UserId { get; set; }
    public required UserDisplayName DisplayName { get; set; }
    public required Email Email { get; set; }
    public required TelegramId? TelegramId { get; set; }
}
