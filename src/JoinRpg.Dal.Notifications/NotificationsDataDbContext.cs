using JoinRpg.Dal.CommonEfCore;

namespace JoinRpg.Dal.Notifications;

public class NotificationsDataDbContext(DbContextOptions<NotificationsDataDbContext> options) : JoinPostgreSqlEfContextBase(options)
{
    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(npgSqlOptions =>
        {
            npgSqlOptions.MapEnum<NotificationChannel>();
            npgSqlOptions.MapEnum<NotificationMessageStatus>();
        });
    }

    internal DbSet<NotificationMessage> Notifications { get; set; } = null!;

    internal DbSet<NotificationMessageChannel> NotificationMessageChannels { get; set; } = null!;
}
