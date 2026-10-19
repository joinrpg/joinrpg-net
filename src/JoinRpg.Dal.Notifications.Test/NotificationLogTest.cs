using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.DomainTypes.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;

namespace JoinRpg.Dal.Notifications.Test;

/// <summary>
/// Лог уведомлений для админки на настоящем Postgres: новые сверху, лимит, фильтры по получателю и каналу.
/// </summary>
public class NotificationLogTest : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    private static readonly UserIdentification Alice = new(10);
    private static readonly UserIdentification Bob = new(20);
    // В прошлом: отправщик берёт из очереди только строки с SendAfter раньше CURRENT_TIMESTAMP.
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
            Notification("second", Bob, NotificationAddress.Ui()),
            Notification("third", Alice, NotificationAddress.Ui()),
        ]);

        var result = await repository.GetNotificationLog(recipient: null, channel: null, limit: 2);

        result.Select(m => m.Message.Header).ShouldBe(["third", "second"]);
    }

    [Fact]
    public async Task FiltersByRecipient()
    {
        await using var context = CreateContext();
        INotificationRepository repository = CreateRepository(context);
        await repository.InsertNotifications([
            Notification("to alice", Alice, NotificationAddress.Ui()),
            Notification("to bob", Bob, NotificationAddress.Ui()),
        ]);

        var result = await repository.GetNotificationLog(Alice, channel: null, limit: 100);

        result.Select(m => m.Message.Header).ShouldBe(["to alice"]);
    }

    [Fact]
    public async Task ReturnsAllChannelsWithAddressesAndStatuses()
    {
        await using var context = CreateContext();
        INotificationRepository repository = CreateRepository(context);
        await repository.InsertNotifications([
            Notification("both", Alice, NotificationAddress.Ui(), new NotificationAddress(NotificationChannel.Email, "alice@example.com")),
        ]);

        var item = (await repository.GetNotificationLog(recipient: null, channel: null, limit: 100)).ShouldHaveSingleItem();

        item.Channels.ShouldBe([
            new NotificationDeliveryDto(NotificationChannel.ShowInUi, "", NotificationMessageStatus.Queued, 0, Now),
            // Так адрес пишет в базу InsertNotifications — ToString() типизированного значения.
            new NotificationDeliveryDto(NotificationChannel.Email, "Email(alice@example.com)", NotificationMessageStatus.Queued, 0, Now),
        ]);
    }

    [Fact]
    public async Task ShowsUnparsableAddressAsIs()
    {
        await using var context = CreateContext();
        INotificationRepository repository = CreateRepository(context);
        await repository.InsertNotifications([
            Notification("broken", Alice, new NotificationAddress(NotificationChannel.Email, "alice@example.com")),
        ]);
        // Такие адреса оставались в очереди с прошлых форматов (#5466) — отправить их нельзя, но показать надо.
        _ = await context.NotificationMessageChannels.ExecuteUpdateAsync(s => s.SetProperty(c => c.ChannelSpecificValue, "not an email"));

        var delivery = (await repository.GetNotificationLog(recipient: null, channel: null, limit: 100)).ShouldHaveSingleItem().Channels.ShouldHaveSingleItem();

        delivery.Address.ShouldBe("not an email");
    }

    [Fact]
    public async Task FilterByChannelKeepsOnlyThatChannel()
    {
        await using var context = CreateContext();
        INotificationRepository repository = CreateRepository(context);
        await repository.InsertNotifications([
            Notification("ui only", Alice, NotificationAddress.Ui()),
            Notification("ui and email", Alice, NotificationAddress.Ui(), new NotificationAddress(NotificationChannel.Email, "alice@example.com")),
        ]);

        var item = (await repository.GetNotificationLog(recipient: null, NotificationChannel.Email, limit: 100)).ShouldHaveSingleItem();

        item.Message.Header.ShouldBe("ui and email");
        item.Channels.Select(c => c.Channel).ShouldBe([NotificationChannel.Email]);
    }

    [Fact]
    public async Task FiltersByRecipientAndChannelTogether()
    {
        await using var context = CreateContext();
        INotificationRepository repository = CreateRepository(context);
        var email = new NotificationAddress(NotificationChannel.Email, "user@example.com");
        await repository.InsertNotifications([
            Notification("alice email", Alice, email),
            Notification("alice ui", Alice, NotificationAddress.Ui()),
            Notification("bob email", Bob, email),
        ]);

        var result = await repository.GetNotificationLog(Alice, NotificationChannel.Email, limit: 100);

        result.Select(m => m.Message.Header).ShouldBe(["alice email"]);
    }

    [Fact]
    public async Task ChannelFilterAppliesBeforeLimit()
    {
        await using var context = CreateContext();
        INotificationRepository repository = CreateRepository(context);
        await repository.InsertNotifications([
            Notification("older email", Alice, new NotificationAddress(NotificationChannel.Email, "alice@example.com")),
            Notification("newest ui", Alice, NotificationAddress.Ui()),
        ]);

        var result = await repository.GetNotificationLog(recipient: null, NotificationChannel.Email, limit: 1);

        result.Select(m => m.Message.Header).ShouldBe(["older email"]);
    }

    [Fact]
    public async Task RecipientFilterAppliesBeforeLimit()
    {
        await using var context = CreateContext();
        INotificationRepository repository = CreateRepository(context);
        await repository.InsertNotifications([
            Notification("older alice", Alice, NotificationAddress.Ui()),
            Notification("newest bob", Bob, NotificationAddress.Ui()),
        ]);

        var result = await repository.GetNotificationLog(Alice, channel: null, limit: 1);

        result.Select(m => m.Message.Header).ShouldBe(["older alice"]);
    }

    [Fact]
    public async Task ReadsStatusAndAttemptsAfterSending()
    {
        await using var context = CreateContext();
        INotificationRepository repository = CreateRepository(context);
        var email = new NotificationAddress(NotificationChannel.Email, "alice@example.com");
        await repository.InsertNotifications([Notification("sent", Alice, email)]);

        var sending = (await repository.SelectNextNotificationForSending(NotificationChannel.Email)).ShouldNotBeNull();
        await repository.MarkSendingSucceeded(sending.MessageId, NotificationChannel.Email);

        var delivery = (await repository.GetNotificationLog(recipient: null, channel: null, limit: 100)).ShouldHaveSingleItem().Channels.ShouldHaveSingleItem();
        delivery.Status.ShouldBe(NotificationMessageStatus.Sent);
        delivery.Attempts.ShouldBe(1);
    }

    private static NotificationMessageCreateDto Notification(string header, UserIdentification recipient, params NotificationAddress[] channels)
        => new(
            new NotificationMessageForRecipient(new MarkdownString("body"), new UserIdentification(1), header, recipient, EntityReference: null, Now),
            channels);
}
