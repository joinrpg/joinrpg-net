using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Common.WebInfrastructure.Job;

internal class JobRunner<TJob>(IJobImpersonator impersonator) : IJobRunner where TJob : class, IJob
{
    public async Task RunJob(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        impersonator.StartAsRobot();
        try
        {
            var job = serviceScope.ServiceProvider.GetRequiredService<TJob>();
            await job.RunOnce(cancellationToken);
        }
        finally
        {
            impersonator.Stop();
        }
    }

    public string Name => typeof(TJob).Name;
    public string FullName => typeof(TJob).FullName!;
    public Type JobType => typeof(TJob);
}
