using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.Services.Impl.Projects.Metadata;

/// <summary>
/// Разовая починка данных под issue #4878: скрывает публичные группы, до которых нет публичного
/// пути от корня.
/// </summary>
/// <remarks>
/// Джоба разовая по смыслу, но написана идемпотентной и оставлена в расписании как страж: после
/// первого прохода находить нечего, а если что-то найдётся снова — значит правило где-то обходится
/// (валидация стоит только на создании и редактировании группы), и в логах будет предупреждение.
/// </remarks>
[Obsolete("Разовая починка данных под #4878, удалить вместе с ней — см. #4974")]
internal class HidePublicGroupsUnderPrivateJob(
    ICharacterGroupRepository characterGroupRepository,
    IPublicGroupVisibilityFixer fixer,
    ILogger<HidePublicGroupsUnderPrivateJob> logger) : IDailyJob
{
    public async Task RunOnce(CancellationToken cancellationToken)
    {
        var candidates = await characterGroupRepository.GetProjectsWithPublicGroupUnderPrivateParent();
        if (candidates.Count == 0)
        {
            logger.LogDebug("Публичных групп под непубличными не найдено");
            return;
        }

        logger.LogInformation("Проектов с публичной группой под непубличной: {projectCount}", candidates.Count);

        var hiddenTotal = 0;
        foreach (var projectId in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Проект обрабатываем отдельной операцией: одна сломавшаяся не должна утащить остальные.
            try
            {
                var hidden = await fixer.HideGroupsWithoutPublicPath(projectId);
                if (hidden.Count == 0)
                {
                    // Прямое ребро «публичная под непубличной» есть, а нарушения нет: так бывает,
                    // когда у группы есть второй, публичный путь наверх. Чинить нечего.
                    logger.LogDebug("{projectId}: публичный путь есть у всех групп", projectId);
                    continue;
                }

                hiddenTotal += hidden.Count;
                logger.LogWarning(
                    "{projectId}: скрыто групп без публичного пути наверх — {groupCount}: {groupNames}",
                    projectId, hidden.Count, string.Join(", ", hidden));
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "{projectId}: не удалось скрыть группы без публичного пути", projectId);
            }
        }

        logger.LogInformation("Всего скрыто групп: {groupCount}", hiddenTotal);
    }
}
