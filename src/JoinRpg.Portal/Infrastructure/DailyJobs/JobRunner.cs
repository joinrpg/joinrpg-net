using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Portal.Infrastructure.DailyJobs;

internal class JobRunner<TJob>(IVirtualUsersService virtualUsersService, IImpersonateAccessor currentUser) : IJobRunner where TJob : class, IDailyJob
{
    public async Task RunJob(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        try
        {
            DataModel.User robotUser = virtualUsersService.RobotUser;
            currentUser.StartImpersonate(robotUser.GetId(), robotUser.ExtractDisplayName(), robotUser.Auth.IsAdmin);
            var job = serviceScope.ServiceProvider.GetRequiredService<TJob>();
            await job.RunOnce(cancellationToken);
        }
        finally
        {
            currentUser.StopImpersonate();
        }
    }

    public string Name => typeof(TJob).Name;
}

public interface IJobRunner
{
    Task RunJob(IServiceScope serviceScope, CancellationToken cancellationToken);
    string Name { get; }
}
