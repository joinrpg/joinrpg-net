using JoinRpg.Data.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JoinRpg.Portal.Infrastructure.HealthChecks;

public class HealthCheckLoadProjects : IHealthCheck
{
    private readonly IProjectRepository projectRepository;

    public HealthCheckLoadProjects(IProjectRepository projectRepository) => this.projectRepository = projectRepository;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await projectRepository.GetProjectsBySpecification(ProjectListSpecification.Active);
            return HealthCheckResult.Healthy();
        }
        catch (OperationCanceledException)
        {
            // Отмена — это сработавший таймаут чека или ушедший клиент. Пусть движок
            // health-чеков отчитается про таймаут, а не мы про «не грузятся проекты».
            throw;
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Fail to load projects", exception);
        }
    }
}
