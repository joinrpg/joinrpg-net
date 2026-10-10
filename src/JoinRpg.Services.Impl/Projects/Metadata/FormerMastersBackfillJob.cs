using JoinRpg.Services.Interfaces.ProjectAccess;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Services.Impl.Projects.Metadata;

/// <summary>
/// Бэкфилл бывших мастеров (ADR019, §6): до мягкого удаления снятый мастер исчезал из ProjectAcls,
/// и факт «был мастером» оставался только в следах: комментариях, деньгах, авторстве персонажей и групп. Джоба записывает таких людей строками Removed.
/// Идемпотентна: повторный прогон не находит пар без ACL. Временная — удаляется после отработки на проде.
/// </summary>
internal class FormerMastersBackfillJob(
    IProjectRepository projectRepository,
    IServiceScopeFactory scopeFactory,
    ICurrentUserAccessor currentUserAccessor,
    ILogger<FormerMastersBackfillJob> logger
    ) : IDailyJob
{
    public async Task RunOnce(CancellationToken cancellationToken)
    {
        var candidates = await projectRepository.GetFormerMasterCandidates();
        logger.LogInformation("Найдено {candidateCount} бывших мастеров без записи ACL", candidates.Count);

        foreach (var project in candidates.GroupBy(c => c.ProjectId))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Свой scope, а с ним свой DbContext, на каждый проект: сущности упавшего SaveChanges не остаются
            // в трекере и не валят следующие проекты, а трекер не копит графы всех проектов за прогон.
            // Имперсонация робота (её ставит JobRunner) живёт в scope — в новом её надо повторить.
            using var scope = scopeFactory.CreateScope();
            var impersonate = scope.ServiceProvider.GetRequiredService<IImpersonateAccessor>();
            impersonate.StartImpersonate(currentUserAccessor.UserIdentification, currentUserAccessor.DisplayName, currentUserAccessor.IsAdmin);
            try
            {
                await scope.ServiceProvider.GetRequiredService<IProjectAccessService>()
                    .RegisterFormerMasters(project.Key, [.. project.Select(c => c.UserId)]);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Следующий прогон доделает этот проект.
                logger.LogError(ex, "Не удалось записать бывших мастеров проекта {projectId}", project.Key);
            }
            finally
            {
                impersonate.StopImpersonate();
            }
        }
    }
}
