using System;
using System.Threading;
using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JoinRpg.Portal.Infrastructure.HealthChecks
{
    public class HealthCheckLoadProjects : IHealthCheck
    {
        private readonly IProjectRepository projectRepository;

        public HealthCheckLoadProjects(IProjectRepository projectRepository)
        {
            this.projectRepository = projectRepository;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var x = await projectRepository.GetActiveProjectsWithClaimCount(userId: null);
                return HealthCheckResult.Healthy();
            }
            catch (Exception exception)
            {
                return HealthCheckResult.Unhealthy("Fail to load projects", exception);
                throw;
            }
        }
    }
}
