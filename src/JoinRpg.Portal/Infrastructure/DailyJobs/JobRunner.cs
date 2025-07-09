using Joinrpg.Web.Identity;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Portal.Infrastructure.DailyJobs;

internal class JobRunner<TJob>(TJob job, IVirtualUsersService virtualUsersService, ICurrentUserSetAccessor currentUser) where TJob : class, IDailyJob
{
    public async Task RunJob(CancellationToken cancellationToken)
    {
        try
        {
            currentUser.StartImpersonate(virtualUsersService.RobotUser);
            await job.RunOnce(cancellationToken);
        }
        finally
        {
            currentUser.StopImpersonate();
        }

    }
}
