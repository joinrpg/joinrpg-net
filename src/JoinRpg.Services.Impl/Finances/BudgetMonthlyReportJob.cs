using JoinRpg.Services.Interfaces.Notification;
using Microsoft.Extensions.Options;

namespace JoinRpg.Services.Impl.Finances;

/// <summary>
/// Ежемесячный финансовый отчёт в Telegram-канал: те же цифры, что показывает виджет бюджета
/// на /about (самый свежий завершённый месяц и «с начала года»). Джоба ежедневная (планировщик
/// AddMidnightJobs), день отправки проверяет сама — как BudgetExpensesAlertJob. В режиме Monthly
/// отправляет 16-го числа: месяц считается завершённым с 16-го следующего (BudgetPeriods) и именно
/// тогда появляется в виджете. Канал и режим — из конфигурации (BudgetMonthlyReportOptions).
/// </summary>
[JobDelay(9)] // 09:00 UTC = 12:00 МСК
internal class BudgetMonthlyReportJob(
    IBudgetWidgetService budgetWidgetService,
    ITelegramNotificationService telegramNotificationService,
    IOptions<BudgetOptions> budgetOptions,
    IOptions<BudgetMonthlyReportOptions> reportOptions,
    TimeProvider timeProvider,
    ILogger<BudgetMonthlyReportJob> logger) : IJob
{
    public async Task RunOnce(CancellationToken cancellationToken)
    {
        var options = reportOptions.Value;
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        if (options.Schedule == BudgetMonthlyReportSchedule.Monthly && today.Day != 16)
        {
            logger.LogInformation("Сегодня ({day}) не 16-е — ежемесячный финансовый отчёт в Telegram не отправляется", today.Day);
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var report = await budgetWidgetService.LoadBudgetReport(budgetOptions.Value.Project, today);
        var month = report.Periods.Where(period => period.Kind == BudgetPeriodKind.Month).MaxBy(period => period.Start);
        var yearToDate = report.Periods.Single(period => period.Kind == BudgetPeriodKind.YearToDate);

        logger.LogInformation("Отправляем ежемесячный финансовый отчёт ({month:yyyy-MM}) в Telegram-канал {chatId}",
            month.Start, options.TelegramChatId);
        await telegramNotificationService.SendTelegramNotification(
            options.TelegramChatId,
            BudgetMonthlyReportMessageBuilder.Build(month, yearToDate));
    }
}
