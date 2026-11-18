namespace JoinRpg.Common.WebInfrastructure.Job;

/// <summary>
/// Раннер джобы: запускает её под имперсонацией из <see cref="IJobImpersonator"/> в переданном scope.
/// </summary>
internal class JobRunner(Type jobType, IJobImpersonator impersonator) : IJobRunner
{
    public async Task RunJob(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        impersonator.StartAsRobot();
        try
        {
            var job = (IJob)serviceScope.ServiceProvider.GetRequiredService(jobType);
            await job.RunOnce(cancellationToken);
        }
        finally
        {
            impersonator.Stop();
        }
    }

    public string Name => jobType.Name;
    public string FullName => jobType.FullName!;
    public Type JobType => jobType;
}

/// <summary>
/// Создаёт <see cref="JobRunner"/>; живёт в scope вместе с <see cref="IJobImpersonator"/>.
/// </summary>
internal class JobRunnerFactory(IJobImpersonator impersonator) : IJobRunnerFactory
{
    public IJobRunner Create(Type jobType) => jobType.IsAssignableTo(typeof(IJob))
        ? new JobRunner(jobType, impersonator)
        : throw new ArgumentException($"Тип {jobType.FullName} не реализует IJob — раннер может запускать только джобы.");
}
