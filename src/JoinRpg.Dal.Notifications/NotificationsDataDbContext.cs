namespace JoinRpg.Dal.Notifications;

public class NotificationsDataDbContext(DbContextOptions<NotificationsDataDbContext> options) : DbContext(options)
{
    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(npgSqlOptions =>
        {
            npgSqlOptions.MapEnum<NotificationChannel>();
            npgSqlOptions.MapEnum<NotificationMessageStatus>();
        });

        // InternalSelectNextNotificationAsync (NotificationsRepository) делает FirstOrDefaultAsync() без OrderBy
        // поверх FromSqlRaw с "LIMIT 1 FOR UPDATE SKIP LOCKED" — это намеренная выборка произвольной свободной
        // задачи из очереди, порядок строки не важен. EF Core не разбирает сырой SQL и всегда предупреждает
        // об отсутствии OrderBy в LINQ-части, а точечного подавления для одного запроса в EF Core нет
        // (см. https://github.com/dotnet/efcore/issues/30411).
        optionsBuilder.ConfigureWarnings(b => b.Ignore(CoreEventId.FirstWithoutOrderByAndFilterWarning));
    }

    internal DbSet<NotificationMessage> Notifications { get; set; } = null!;

    internal DbSet<NotificationMessageChannel> NotificationMessageChannels { get; set; } = null!;
}
