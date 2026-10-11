using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Claims;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.DomainTypes.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;

namespace JoinRpg.Dal.Notifications.Test;

/// <summary>
/// Выборка уведомлений по заявкам проекта (DRP, #5481) на настоящем Postgres: префиксный LIKE по
/// EntityReference должен ловить ровно заявки, комментарии и финансовые операции нужного проекта.
/// </summary>
public class ClaimNotificationsForProjectTest : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    private static readonly ProjectIdentification Project = new(1);
    private static readonly ClaimIdentification Claim = new(Project, 5);
    private static readonly DateTimeOffset LostAt = new(2026, 10, 7, 22, 30, 49, TimeSpan.Zero);

    async Task IAsyncLifetime.InitializeAsync()
    {
        await postgres.StartAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync() => await postgres.DisposeAsync();

    private readonly List<string> sqlLog = [];

    private NotificationsDataDbContext CreateContext()
        => new(new DbContextOptionsBuilder<NotificationsDataDbContext>()
            .UseNpgsql(postgres.GetConnectionString())
            .LogTo(sqlLog.Add, [DbLoggerCategory.Database.Command.Name])
            .Options);

    [Fact]
    public async Task ReturnsClaimCommentAndFinanceNotificationsOfProjectBeforeCutoff()
    {
        await using var context = CreateContext();
        INotificationRepository repository = new NotificationsRepository(context, NullLogger<NotificationsRepository>.Instance);

        await repository.InsertNotifications([
            Notification("finance", new FinanceOperationIdentification(Claim, 9), LostAt.AddDays(-1)),
            Notification("claim", Claim, LostAt.AddDays(-3)),
            Notification("comment", new ClaimCommentIdentification(Claim, 7), LostAt.AddDays(-2)),
            // Проект 12 начинается с той же цифры — префикс «ClaimId(1-» его ловить не должен.
            Notification("other project", new ClaimIdentification(new ProjectIdentification(12), 3), LostAt.AddDays(-2)),
            Notification("project itself", Project, LostAt.AddDays(-2)),
            Notification("no reference", null, LostAt.AddDays(-2)),
            Notification("after loss", new ClaimIdentification(Project, 6), LostAt.AddSeconds(1)),
        ]);

        var result = await repository.GetClaimNotificationsForProject(Project, LostAt);

        result.Select(m => m.Header).ShouldBe(["claim", "comment", "finance"]);
        result.Select(m => m.EntityReference).ShouldBe<IProjectEntityId?>([
            Claim,
            new ClaimCommentIdentification(Claim, 7),
            new FinanceOperationIdentification(Claim, 9),
        ]);
    }

    [Fact]
    public async Task CutoffInLocalOffsetIsExclusive()
    {
        await using var context = CreateContext();
        INotificationRepository repository = new NotificationsRepository(context, NullLogger<NotificationsRepository>.Instance);
        var claim = new ClaimIdentification(new ProjectIdentification(2), 1);

        await repository.InsertNotifications([
            Notification("before", claim, LostAt.AddSeconds(-1)),
            Notification("exactly at", claim, LostAt),
        ]);

        // Npgsql не принимает в timestamptz DateTimeOffset с ненулевым смещением — репозиторий приводит к UTC.
        var result = await repository.GetClaimNotificationsForProject(claim.ProjectId, LostAt.ToOffset(TimeSpan.FromHours(3)));

        result.Select(m => m.Header).ShouldBe(["before"]);
    }

    [Fact]
    public async Task FiltersByPrefixLikeSoIndexIsUsable()
    {
        await using var context = CreateContext();
        var repository = new NotificationsRepository(context, NullLogger<NotificationsRepository>.Instance);
        sqlLog.Clear();

        _ = await repository.GetClaimNotificationsForProject(Project, LostAt);

        // Индекс text_pattern_ops работает только для префиксного LIKE; left(...) = ... или strpos его не используют.
        var select = sqlLog.Last(entry => entry.Contains("SELECT"));
        select.ShouldContain("\"EntityReference\" LIKE");
        select.ShouldNotContain("left(");
    }

    private static NotificationMessageCreateDto Notification(string header, IProjectEntityId? reference, DateTimeOffset createdAt)
        => new(
            new NotificationMessageForRecipient(new MarkdownString("body"), new UserIdentification(1), header, new UserIdentification(2), reference, createdAt),
            [NotificationAddress.Ui()]);
}
