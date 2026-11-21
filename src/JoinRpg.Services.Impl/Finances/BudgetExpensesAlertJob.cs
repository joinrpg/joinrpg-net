using JoinRpg.Services.Impl.Projects;
using Microsoft.Extensions.Options;

namespace JoinRpg.Services.Impl.Finances;

/// <summary>
/// Еженедельная (по вторникам) проверка фактических расходов проекта бюджета (см. спеку
/// бюджетных алертов): если в предыдущем календарном месяце не зафиксированы расходы,
/// мастерам, ответственным за финансы, отправляется алерт. Джоба ежедневная, день недели
/// проверяет сама — как BlockedProjectsReminderJob.
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
    public async Task RunOnce(CancellationToken cancellationToken)
    {
        if (timeProvider.GetUtcNow().DayOfWeek != DayOfWeek.Tuesday)
        {
            logger.LogInformation("Сегодня ({day}) не вторник — алерт об отсутствии расходов в бюджете не отправляется", timeProvider.GetUtcNow().DayOfWeek);
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var previousMonthStart = currentMonthStart.AddMonths(-1);

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
