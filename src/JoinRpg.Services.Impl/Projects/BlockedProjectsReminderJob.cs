namespace JoinRpg.Services.Impl.Projects;

/// <summary>
/// Еженедельное (по вторникам) напоминание действующим мастерам заблокированных проектов (ADR023)
/// определиться с судьбой проекта: закрыть его или восстановить данные по инструкции.
/// Джоба ежедневная, день недели проверяет сама — как AdvertisementJob.
/// </summary>
[JobDelay(9)] // 09:00 UTC = 12:00 МСК
internal class BlockedProjectsReminderJob(
    IProjectRepository projectRepository,
    IProjectMetadataRepository projectMetadataRepository,
    MasterEmailService masterEmailService,
    TimeProvider timeProvider,
    ILogger<BlockedProjectsReminderJob> logger) : IJob
{
    public async Task RunOnce(CancellationToken cancellationToken)
    {
        if (timeProvider.GetUtcNow().DayOfWeek != DayOfWeek.Tuesday)
        {
            logger.LogInformation("Сегодня ({day}) не вторник — напоминания заблокированным проектам не отправляются", timeProvider.GetUtcNow().DayOfWeek);
            return;
        }

        var blockedProjects = await projectRepository.GetBlockedProjects();
        logger.LogInformation("Найдено {count} заблокированных проектов для еженедельного напоминания", blockedProjects.Count);

        foreach (var project in blockedProjects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var metadata = await projectMetadataRepository.GetProjectMetadata(new(project.ProjectId));
                if (metadata.ProjectStatus != ProjectLifecycleStatus.Blocked)
                {
                    // Проект с IsBlocked должен быть в статусе Blocked, но не полагаемся на это
                    // вслепую: статус вычисляется из нескольких флагов, а проверка дешевле письма не тому.
                    logger.LogInformation("Проект {projectId} в статусе {status} вместо Blocked, пропускаем", project.ProjectId, metadata.ProjectStatus);
                    continue;
                }
                await masterEmailService.EmailProjectBlockedReminder(new() { ProjectId = metadata.ProjectId });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Следующий вторник досылает упавший проект.
                logger.LogError(ex, "Не удалось отправить напоминание мастерам заблокированного проекта {projectId}", project.ProjectId);
            }
        }
    }
}
