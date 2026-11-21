using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Interfaces;
using JoinRpg.Services.Impl.Finances;
using JoinRpg.Services.Impl.Projects;
using Microsoft.Extensions.Options;

namespace JoinRpg.Services.Impl.Test.Finances;

/// <summary>
/// Юнит-тесты ежедневной джобы-алерта об отсутствии фактических расходов
/// в проекте бюджета за предыдущий календарный месяц. По образцу BlockedProjectsReminderJobTest.
/// </summary>
public class BudgetExpensesAlertJobTest
{
    // Ноябрь 2026: конец прошлого месяца — 2026-11-01, полная неделя с него — 2026-11-08,
    // окно алерта — первый прогон после 2026-11-08 00:00 (у джобы он в 09:00)
    private static readonly DateTimeOffset WednesdayInFirstWeek = new(2026, 11, 5, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ExactlyFullWeekAfterMonthEnd = new(2026, 11, 8, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FirstRunAfterFullWeek = new(2026, 11, 8, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset WednesdayInSecondWeek = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);

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
    public async Task RunOnce_LessThanFullWeekSinceMonthEnd_SendsNothing()
    {
        var job = CreateJob(
            new StubExpenseRepository(_ => throw new NotSupportedException("В первую неделю месяца расходы не читаются")),
            new MockedProject(),
            WednesdayInFirstWeek);

        await job.RunOnce(CancellationToken.None);

        notificationService.Queued.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunOnce_ExactlyFullWeekSinceMonthEnd_SendsNothing()
    {
        var job = CreateJob(
            new StubExpenseRepository(_ => throw new NotSupportedException("Ровно полная неделя — не больше, алерт рано")),
            new MockedProject(),
            ExactlyFullWeekAfterMonthEnd);

        await job.RunOnce(CancellationToken.None);

        notificationService.Queued.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunOnce_AlertWindowAlreadyPassed_SendsNothing()
    {
        var job = CreateJob(
            new StubExpenseRepository(_ => throw new NotSupportedException("Окно алерта однодневное — после него расходы не читаются")),
            new MockedProject(),
            WednesdayInSecondWeek);

        await job.RunOnce(CancellationToken.None);

        notificationService.Queued.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunOnce_AfterFullWeek_HasExpensesInPreviousMonth_SendsNothing()
    {
        var job = CreateJob(
            new StubExpenseRepository(_ => Task.FromResult(true)),
            new MockedProject(),
            FirstRunAfterFullWeek);

        await job.RunOnce(CancellationToken.None);

        notificationService.Queued.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunOnce_AfterFullWeek_NoExpensesInPreviousMonth_SendsAlert()
    {
        var mock = new MockedProject();
        var stub = new StubExpenseRepository(_ => Task.FromResult(false));
        var job = CreateJob(stub, mock, FirstRunAfterFullWeek);

        await job.RunOnce(CancellationToken.None);

        stub.LastFromUtc.ShouldBe(PreviousMonthStart);
        stub.LastToUtc.ShouldBe(CurrentMonthStart);
        var letter = notificationService.Queued.ShouldHaveSingleItem();
        letter.EntityReference.ShouldBe(new ProjectIdentification(mock.Project.ProjectId));
        letter.NotificationClass.ShouldBe(NotificationClass.MasterProject);
    }

    [Fact]
    public async Task RunOnce_ProjectBlocked_SendsNothing()
    {
        var mock = new MockedProject();
        mock.Project.IsBlocked = true;
        mock.ReInitProjectInfo();
        var job = CreateJob(
            new StubExpenseRepository(_ => Task.FromResult(false)),
            mock,
            FirstRunAfterFullWeek);

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
