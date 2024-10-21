using JoinRpg.Dal.CommonEfCore;

namespace JoinRpg.Dal.Notifications;

public class NotificationsDataDbContext(DbContextOptions<NotificationsDataDbContext> options) : JoinPostgreSqlEfContextBase(options)
{
    internal DbSet<NotificationMessage> Notifications { get; set; } = null!;

    internal DbSet<NotificationMessageChannel> NotificationMessageChannels { get; set; } = null!;


}
