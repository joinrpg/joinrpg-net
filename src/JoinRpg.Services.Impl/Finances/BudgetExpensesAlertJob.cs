using JoinRpg.Services.Impl.Projects;
using Microsoft.Extensions.Options;

namespace JoinRpg.Services.Impl.Finances;

/// <summary>
/// Еженедельная (по вторникам) проверка фактических расходов проекта бюджета (см. спеку
/// бюджетных алертов): если в бюджете не зафиксированы расходы более 5 недель, мастерам,
/// ответственным за финансы, отправляется алерт. Джоба ежедневная, день недели проверяет
/// сама — как BlockedProjectsReminderJob.
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
    private const int AlertWindowDays = 35; // 5 недель без фактических расходов

    public async Task RunOnce(CancellationToken cancellationToken)
    {
        if (timeProvider.GetUtcNow().DayOfWeek != DayOfWeek.Tuesday)
        {
            logger.LogInformation("Сегодня ({day}) не вторник — алерт об отсутствии расходов в бюджете не отправляется", timeProvider.GetUtcNow().DayOfWeek);
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

        var cutoff = timeProvider.GetUtcNow().UtcDateTime.AddDays(-AlertWindowDays);
        if (await expenseRepository.HasActualExpensesSince(projectId, cutoff))
        {
            logger.LogInformation("В бюджете проекта {projectId} есть фактические расходы за последние {days} дней, алерт не нужен", projectId, AlertWindowDays);
            return;
        }

        logger.LogInformation("В бюджете проекта {projectId} нет фактических расходов более {days} дней — отправляем алерт", projectId, AlertWindowDays);
        await masterEmailService.EmailBudgetExpensesAlert(new() { ProjectId = projectId });
    }
}
