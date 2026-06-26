using System.Data.Entity;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Services.Impl;

[Obsolete("Временный хак")]
internal class UnhotNpcJob(IUnitOfWork unitOfWork, ILogger<UnhotNpcJob> logger) : IDailyJob
{
    public async Task RunOnce(CancellationToken cancellationToken)
    {
        var projectIds = await unitOfWork.GetDbSet<Character>()
            .Where(c => c.CharacterType == CharacterType.NonPlayer && c.IsHot)
            .Select(c => c.ProjectId)
            .Distinct()
            .OrderBy(id => id)
            .ToListAsync();

        if (projectIds.Count == 0)
        {
            logger.LogInformation("Горячих NPC не обнаружено");
            return;
        }

        foreach (var projectId in projectIds)
        {
            logger.LogError("Проект {ProjectId} содержит NPC с IsHot=true — требуется сброс флага", projectId);
        }

        var updated = await unitOfWork.ExecuteSqlCommandAsync(
            "UPDATE dbo.Characters SET IsHot = 0 WHERE CharacterType = 1 AND IsHot = 1");

        logger.LogInformation("Сброшен флаг IsHot у {Count} NPC-персонажей в {ProjectCount} играх",
            updated, projectIds.Count);
    }
}
