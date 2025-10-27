using Joinrpg.Web.Identity;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Portal.Infrastructure.DailyJobs;

internal class JobRunner<TJob>(IVirtualUsersService virtualUsersService, ICurrentUserSetAccessor currentUser) where TJob : class, IDailyJob
{
    public async Task RunJob(CancellationToken cancellationToken, IServiceScope serviceScope)
    {
        try
        {
            currentUser.StartImpersonate(virtualUsersService.RobotUser);
            var job = serviceScope.ServiceProvider.GetRequiredService<TJob>();
            await job.RunOnce(cancellationToken);
        }
        finally
        {
            currentUser.StopImpersonate();
        }

    }
}
