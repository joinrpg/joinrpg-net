using JoinRpg.Interfaces;
using JoinRpg.Services.Impl.Finances;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Notification;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace JoinRpg.Services.Impl.Test.Finances;

/// <summary>
/// Юнит-тесты джобы ежемесячного финансового отчёта в Telegram. Режим Monthly отправляет
/// только 16-го числа (день, когда месяц начинает показываться в виджете на /about),
/// режим Daily (отладка) — каждый день. Джоба берёт самый свежий завершённый месяц
/// и YearToDate из BudgetReport виджета.
/// </summary>
public class BudgetMonthlyReportJobTest
{
    // 22-е — не 16-е; 16-е — день отправки отчёта
    private static readonly DateTimeOffset Not16th = new(2026, 11, 22, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Day16th = new(2026, 11, 16, 9, 0, 0, TimeSpan.Zero);

    private const long TestChatId = -1004315256401;

    private static readonly BudgetReport Report = new(
    [
        new BudgetPeriodSums(BudgetPeriodKind.Month, new DateOnly(2026, 10, 1), Income: 100000m, Expense: 60000m),
        new BudgetPeriodSums(BudgetPeriodKind.Month, new DateOnly(2026, 9, 1), Income: 1m, Expense: 1m),
        new BudgetPeriodSums(BudgetPeriodKind.Month, new DateOnly(2026, 8, 1), Income: 1m, Expense: 1m),
        new BudgetPeriodSums(BudgetPeriodKind.YearToDate, new DateOnly(2026, 1, 1), Income: 500000m, Expense: 300000m),
        new BudgetPeriodSums(BudgetPeriodKind.CompletedYear, new DateOnly(2025, 1, 1), Income: 1m, Expense: 1m),
    ]);

    [Fact]
    public async Task RunOnce_Daily_Not16th_SendsReport()
    {
        var widgetService = new FakeBudgetWidgetService(Report);
        var telegramService = new FakeTelegramService();
        var job = CreateJob(widgetService, telegramService, BudgetMonthlyReportSchedule.Daily, Not16th);

        await job.RunOnce(CancellationToken.None);

        widgetService.WasCalled.ShouldBeTrue();
        telegramService.LastChatId.ShouldBe(new TelegramChatId(TestChatId));
        telegramService.LastMessage.ShouldNotBeNull();
        telegramService.LastMessage!.Contents.ShouldContain("За октябрь 2026:");
        telegramService.LastMessage.Contents.ShouldContain("За весь 2026 год:");
    }

    [Fact]
    public async Task RunOnce_Monthly_Not16th_SendsNothing()
    {
        var widgetService = new FakeBudgetWidgetService(Report);
        var telegramService = new FakeTelegramService();
        var job = CreateJob(widgetService, telegramService, BudgetMonthlyReportSchedule.Monthly, Not16th);

        await job.RunOnce(CancellationToken.None);

        widgetService.WasCalled.ShouldBeFalse();
        telegramService.LastMessage.ShouldBeNull();
    }

    [Fact]
    public async Task RunOnce_Monthly_16th_SendsReport()
    {
        var widgetService = new FakeBudgetWidgetService(Report);
        var telegramService = new FakeTelegramService();
        var job = CreateJob(widgetService, telegramService, BudgetMonthlyReportSchedule.Monthly, Day16th);

        await job.RunOnce(CancellationToken.None);

        telegramService.LastMessage.ShouldNotBeNull();
        telegramService.LastMessage!.Contents.ShouldContain("За октябрь 2026:");
        telegramService.LastMessage.Contents.ShouldContain("За весь 2026 год:");
    }

    private static BudgetMonthlyReportJob CreateJob(
        FakeBudgetWidgetService widgetService,
        FakeTelegramService telegramService,
        BudgetMonthlyReportSchedule schedule,
        DateTimeOffset now)
        => new(
            widgetService,
            telegramService,
            Options.Create(new BudgetOptions { Project = new ProjectIdentification(183) }),
            Options.Create(new BudgetMonthlyReportOptions { TelegramChatId = new TelegramChatId(TestChatId), Schedule = schedule }),
            new FixedTimeProvider(now),
            NullLogger<BudgetMonthlyReportJob>.Instance);

    private sealed class FakeBudgetWidgetService(BudgetReport report) : IBudgetWidgetService
    {
        public bool WasCalled { get; private set; }

        public Task<BudgetReport> LoadBudgetReport(ProjectIdentification budgetProject, DateOnly today)
        {
            WasCalled = true;
            return Task.FromResult(report);
        }
    }

    private sealed class FakeTelegramService : ITelegramNotificationService
    {
        public TelegramChatId? LastChatId { get; private set; }

        public TelegramHtmlString? LastMessage { get; private set; }

        public Task<SendingResult> SendTelegramNotification(TelegramChatId chatId, TelegramHtmlString contents)
        {
            LastChatId = chatId;
            LastMessage = contents;
            return Task.FromResult(SendingResult.Success());
        }

        public Task<string?> GetMyUserName(CancellationToken cancellationToken) => Task.FromResult<string?>(null);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
