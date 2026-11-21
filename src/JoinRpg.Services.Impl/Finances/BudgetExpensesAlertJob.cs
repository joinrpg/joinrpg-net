using JoinRpg.Services.Impl.Projects;
using Microsoft.Extensions.Options;

namespace JoinRpg.Services.Impl.Finances;

/// <summary>
/// Ежедневная проверка фактических расходов проекта бюджета (см. спеку бюджетных
/// алертов): если в предыдущем календарном месяце не зафиксированы расходы, а с его
/// конца прошло больше полной недели, мастерам, ответственным за финансы, однократно
/// отправляется алерт (окно отправки — один день: джоба ежедневная, поэтому сработает
/// в первый прогон после полной недели). Ограничение по датам джоба проверяет сама —
/// вне окна расходы даже не читаются.
/// </summary>
[JobDelay(9)] // 09:00 UTC = 12:00 МСК
internal class BudgetExpensesAlertJob(
    IExpenseRepository expenseRepository,
    IProjectMetadataRepository projectMetadataRepository,
    MasterEmailService masterEmailService,
    IOptions<BudgetOptions> budgetOptions,
    TimeProvider timeProvider,
    ILogger<BudgetExpensesAlertJob> logger) : IJob
{
    private const int FullWeekDays = 7; // столько должно пройти от конца месяца перед алертом
    private const int AlertWindowDays = 8; // окно отправки — один день: алерт однократный

    public async Task RunOnce(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var previousMonthStart = currentMonthStart.AddMonths(-1);

        if (now <= currentMonthStart.AddDays(FullWeekDays) || now > currentMonthStart.AddDays(AlertWindowDays))
        {
            logger.LogInformation("Сегодня ({today:yyyy-MM-dd}) вне окна алерта ({from:yyyy-MM-dd} — {to:yyyy-MM-dd} после конца прошлого месяца) — алерт об отсутствии расходов в бюджете не отправляется", now, currentMonthStart.AddDays(FullWeekDays), currentMonthStart.AddDays(AlertWindowDays));
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var projectId = budgetOptions.Value.Project;
        var metadata = await projectMetadataRepository.GetProjectMetadata(projectId);
        if (!metadata.ProjectStatus.AllowsChanges())
        {
            logger.LogInformation("Проект {projectId} в статусе {status}, алерт об отсутствии расходов не отправляется", projectId, metadata.ProjectStatus);
            return;
        }

        if (await expenseRepository.HasActualExpensesInPeriod(projectId, previousMonthStart, currentMonthStart))
        {
            logger.LogInformation("В бюджете проекта {projectId} есть фактические расходы за {month:yyyy-MM}, алерт не нужен", projectId, previousMonthStart);
            return;
        }

        logger.LogInformation("В бюджете проекта {projectId} нет фактических расходов за {month:yyyy-MM} — отправляем алерт", projectId, previousMonthStart);
        await masterEmailService.EmailBudgetExpensesAlert(new() { ProjectId = projectId });
    }
}
