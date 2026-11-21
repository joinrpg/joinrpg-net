using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Interfaces;
using JoinRpg.Services.Impl.Finances;
using JoinRpg.Services.Impl.Projects;
using Microsoft.Extensions.Options;

namespace JoinRpg.Services.Impl.Test.Finances;

/// <summary>
/// Юнит-тесты еженедельной (по вторникам) джобы-алерта об отсутствии фактических
/// расходов в проекте бюджета за предыдущий календарный месяц. По образцу BlockedProjectsReminderJobTest.
/// </summary>
public class BudgetExpensesAlertJobTest
{
    // Ноябрь 2026: 10-е — вторник, 11-е — среда
    private static readonly DateTimeOffset Tuesday = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Wednesday = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);

    // Ноябрь 2026: 1-е — воскресенье, предыдущий месяц — октябрь
    private static readonly DateTimeOffset FirstTuesdayOfMonth = new(2026, 12, 1, 9, 0, 0, TimeSpan.Zero);

    private static readonly DateTime PreviousMonthStart = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime CurrentMonthStart = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly FakeNotificationService notificationService = new();

    private BudgetExpensesAlertJob CreateJob(
        IExpenseRepository expenseRepository,
        MockedProject mock,
        DateTimeOffset now)
    {
        var metadataRepository = new FakeProjectMetadataRepository(mock);
        return new(
            expenseRepository,
            metadataRepository,
            new MasterEmailService(
                new MastersNotificationService(notificationService, new FakeUserRepository()),
                metadataRepository, new FakeVirtualUsersService()),
            Options.Create(new BudgetOptions { Project = new ProjectIdentification(mock.Project.ProjectId) }),
            new FixedTimeProvider(now),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<BudgetExpensesAlertJob>.Instance);
    }

    [Fact]
    public async Task RunOnce_NotTuesday_SendsNothing()
    {
        var job = CreateJob(
            new StubExpenseRepository(_ => throw new NotSupportedException("Вне вторника расходы не читаются")),
            new MockedProject(),
            Wednesday);

        await job.RunOnce(CancellationToken.None);

        notificationService.Queued.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunOnce_Tuesday_HasExpensesInPreviousMonth_SendsNothing()
    {
        var job = CreateJob(
            new StubExpenseRepository(_ => Task.FromResult(true)),
            new MockedProject(),
            Tuesday);

        await job.RunOnce(CancellationToken.None);

        notificationService.Queued.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunOnce_Tuesday_NoExpensesInPreviousMonth_SendsAlert()
    {
        var mock = new MockedProject();
        var stub = new StubExpenseRepository(_ => Task.FromResult(false));
        var job = CreateJob(stub, mock, Tuesday);

        await job.RunOnce(CancellationToken.None);

        stub.LastFromUtc.ShouldBe(PreviousMonthStart);
        stub.LastToUtc.ShouldBe(CurrentMonthStart);
        var letter = notificationService.Queued.ShouldHaveSingleItem();
        letter.EntityReference.ShouldBe(new ProjectIdentification(mock.Project.ProjectId));
        letter.NotificationClass.ShouldBe(NotificationClass.MasterProject);
    }

    [Fact]
    public async Task RunOnce_FirstTuesdayOfMonth_ChecksWholePreviousMonth()
    {
        // Первый вторник месяца: проверяется ровно предыдущий календарный месяц
        var stub = new StubExpenseRepository(_ => Task.FromResult(true));
        var job = CreateJob(stub, new MockedProject(), FirstTuesdayOfMonth);

        await job.RunOnce(CancellationToken.None);

        stub.LastFromUtc.ShouldBe(new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc));
        stub.LastToUtc.ShouldBe(new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc));
        notificationService.Queued.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunOnce_Tuesday_ProjectBlocked_SendsNothing()
    {
        var mock = new MockedProject();
        mock.Project.IsBlocked = true;
        mock.ReInitProjectInfo();
        var job = CreateJob(
            new StubExpenseRepository(_ => Task.FromResult(false)),
            mock,
            Tuesday);

        await job.RunOnce(CancellationToken.None);

        notificationService.Queued.ShouldBeEmpty();
    }

    private sealed class StubExpenseRepository(Func<ProjectIdentification, Task<bool>> hasActualExpensesInPeriod) : IExpenseRepository
    {
        public DateTime? LastFromUtc { get; private set; }

        public DateTime? LastToUtc { get; private set; }

        public Task<IReadOnlyCollection<DomainTypes.Finances.ExpenseInfo>> GetExpenses(ProjectIdentification projectId)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<DomainTypes.Finances.ExpenseInfo>> GetExpensesByMaster(ProjectIdentification projectId, UserIdentification masterId)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<DomainTypes.Finances.ExpenseCategoryInfo>> GetCategories(ProjectIdentification projectId)
            => throw new NotSupportedException();

        public Task<bool> HasActualExpensesInPeriod(ProjectIdentification projectId, DateTime fromUtc, DateTime toUtc)
        {
            LastFromUtc = fromUtc;
            LastToUtc = toUtc;
            return hasActualExpensesInPeriod(projectId);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
