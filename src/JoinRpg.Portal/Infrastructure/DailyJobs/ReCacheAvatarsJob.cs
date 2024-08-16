using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Portal.Infrastructure.DailyJobs;

public class ReCacheAvatarsJob(IAvatarService avatarService, IUserRepository userRepository) : IDailyJob
{
    public async Task RunOnce(CancellationToken cancellationToken)
    {
        var avatars = await userRepository.GetLegacyAvatarsList();
        foreach (var (userId, avatarId) in avatars)
        {
            await avatarService.RecacheAvatar(userId, avatarId);
        }
    }
}
