using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;

namespace JoinRpg.Dal.Notifications.Test;

/// <summary>
/// Уведомления пользователя для /profile/notifications на настоящем Postgres: новые сверху, «Ранее», фильтры (#5595).
/// </summary>
public class UserNotificationsTest : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    private static readonly UserIdentification Alice = new(10);
    private static readonly UserIdentification Bob = new(20);
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    async Task IAsyncLifetime.InitializeAsync()
    {
        await postgres.StartAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync() => await postgres.DisposeAsync();

    private NotificationsDataDbContext CreateContext()
        => new(new DbContextOptionsBuilder<NotificationsDataDbContext>()
            .UseNpgsql(postgres.GetConnectionString())
            .Options);

    private static NotificationsRepository CreateRepository(NotificationsDataDbContext context)
        => new(context, NullLogger<NotificationsRepository>.Instance);

    [Fact]
    public async Task NewestFirstAndLimited()
    {
        await using var context = CreateContext();
        INotificationRepository repository = CreateRepository(context);
        await repository.InsertNotifications([
            Notification("first", Alice, NotificationAddress.Ui()),
            Notification("second", Alice, NotificationAddress.Ui()),
            Notification("third", Alice, NotificationAddress.Ui()),
        ]);

        var result = await repository.GetLastNotificationsForUser(Alice, NotificationChannel.ShowInUi, KeySetPagination.Descending(null, 2));

        result.Select(m => m.Message.Header).ShouldBe(["third", "second"]);
    }

    [Fact]
    public async Task BeforeReturnsOnlyOlder()
    {
        await using var context = CreateContext();
        INotificationRepository repository = CreateRepository(context);
        await repository.InsertNotifications([
            Notification("first", Alice, NotificationAddress.Ui()),
            Notification("second", Alice, NotificationAddress.Ui()),
            Notification("third", Alice, NotificationAddress.Ui()),
        ]);
        var newest = await repository.GetLastNotificationsForUser(Alice, NotificationChannel.ShowInUi, KeySetPagination.Descending(null, 2));

        var older = await repository.GetLastNotificationsForUser(Alice, NotificationChannel.ShowInUi, KeySetPagination.Descending(newest.Last().Id.Value, 2));

        older.Select(m => m.Message.Header).ShouldBe(["first"]);
    }

    [Fact]
    public async Task OnlyThisRecipientAndChannel()
    {
        await using var context = CreateContext();
        INotificationRepository repository = CreateRepository(context);
        await repository.InsertNotifications([
            Notification("alice ui", Alice, NotificationAddress.Ui()),
            Notification("alice email only", Alice, new NotificationAddress(NotificationChannel.Email, "alice@example.com")),
            Notification("bob ui", Bob, NotificationAddress.Ui()),
        ]);

        var result = await repository.GetLastNotificationsForUser(Alice, NotificationChannel.ShowInUi, KeySetPagination.Descending(null, 100));

        result.Select(m => m.Message.Header).ShouldBe(["alice ui"]);
    }

    private static NotificationMessageCreateDto Notification(string header, UserIdentification recipient, params NotificationAddress[] channels)
        => new(
            new NotificationMessageForRecipient(new MarkdownString("body"), new UserIdentification(1), header, recipient, EntityReference: null, Now),
            channels);
}
