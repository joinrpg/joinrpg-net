using JoinRpg.Common.BastiliaRatingClient;

namespace JoinRpg.Services.Impl;

internal class BastiliaGamesSyncDailyJob(
    IBastiliaRatingClient bastiliaRatingClient,
    ILogger<BastiliaGamesSyncDailyJob> logger,
    IUserRepository userRepository,
    IProjectMetadataRepository projectMetadataRepository
    ) : IDailyJob
{
    public async Task RunOnce(CancellationToken cancellationToken)
    {
        var members = await bastiliaRatingClient.GetBastiliaActualMembers();
        var projectSet = new HashSet<ProjectIdentification>();
        foreach (var userId in members)
        {
            var userInfo = await userRepository.GetUserInfo(new UserIdentification(userId));
            if (userInfo == null)
            {
                logger.LogWarning("Не найден пользователь {userId}", userId);
                continue;
            }
            logger.LogInformation("Оцениваем игры для {user}", userInfo.Email);

            foreach (var p in userInfo.ActiveClaims.Select(x => x.ProjectId))
            {
                projectSet.Add(p);
            }

            foreach (var p in userInfo.AllProjects)
            {
                projectSet.Add(p);
            }
        }

        var kiSet = new HashSet<KogdaIgraIdentification>();
        foreach (var p in projectSet)
        {
            logger.LogDebug("Загружаем данные по проекту {projectId}", p);
            var details = await projectMetadataRepository.GetProjectDetails(p);
            foreach (var ki in details.KogdaIgraLinkedIds)
            {
                kiSet.Add(ki);
            }
        }

        foreach (var ki in kiSet)
        {
            logger.LogInformation("Обновляем игру {kogdaIgraGameId} на сайте бастильского рейтинга", ki);
            await bastiliaRatingClient.UpdateKogdaIgra(ki);
        }
    }
}
