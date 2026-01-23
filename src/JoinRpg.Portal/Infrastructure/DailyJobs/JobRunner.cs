using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Portal.Infrastructure.DailyJobs;

internal class JobRunner<TJob>(IVirtualUsersService virtualUsersService, IImpersonateAccessor currentUser) where TJob : class, IDailyJob
{
    public async Task RunJob(CancellationToken cancellationToken, IServiceScope serviceScope)
    {
        try
        {
            DataModel.User robotUser = virtualUsersService.RobotUser;
            currentUser.StartImpersonate(robotUser.GetId(), robotUser.ExtractDisplayName());
            var job = serviceScope.ServiceProvider.GetRequiredService<TJob>();
            await job.RunOnce(cancellationToken);
        }
        finally
        {
            currentUser.StopImpersonate();
        }

    }
}
