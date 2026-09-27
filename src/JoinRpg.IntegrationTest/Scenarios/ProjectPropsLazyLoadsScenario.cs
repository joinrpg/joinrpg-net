using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Сборка метаданных проекта и доменные операции над ними (ADR009) не должны лениво догружать
/// связи: загрузчик обязан привезти весь нужный граф сразу (#4987, #4670).
/// </summary>
public class ProjectPropsLazyLoadsScenario(JoinApplicationFactory factory)
    : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task GetProjectMetadata_DoesNotLazyLoad()
    {
        var (masterId, projectId) = await CreateMasterAndProjectAsync();

        var count = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            using var lazyLoads = LazyLoadCounter.BeginScope();
            _ = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return lazyLoads.Count;
        });

        count.ShouldBe(0, $"Сборка ProjectInfo дала {count} ленивых загрузок, см. #4987");
    }

    [Fact]
    public async Task ChangeProjectProperties_DoesNotLazyLoad()
    {
        var (masterId, projectId) = await CreateMasterAndProjectAsync();

        var count = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectService = sp.GetRequiredService<IProjectService>();
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);

            // Мутация грузит проект дважды — до изменения и на пересборку кэша метаданных, —
            // поэтому недогруженная связь стоит здесь двух ленивых загрузок, а не одной.
            using var lazyLoads = LazyLoadCounter.BeginScope();
            await projectService.SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });
            return lazyLoads.Count;
        });

        count.ShouldBe(0, $"SetClaimSettings дал {count} ленивых загрузок, см. #4987");
    }

    private async Task<(UserIdentification MasterId, ProjectIdentification ProjectId)> CreateMasterAndProjectAsync()
    {
        using var scope = factory.Services.CreateScope();
        var masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        var projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId);
        return (masterId, projectId);
    }
}
