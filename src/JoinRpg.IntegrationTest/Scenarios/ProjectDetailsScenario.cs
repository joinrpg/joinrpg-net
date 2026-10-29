using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

public class ProjectDetailsScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task GetProjectDetails_ReturnsClaimApplyRules()
    {
        UserIdentification masterId;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект с правилами подачи заявки");
        }

        var claimApplyRules = "Заявки принимаются только от совершеннолетних";
        var projectAnnounce = "Анонс проекта";

        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectService = sp.GetRequiredService<IProjectService>();
            await projectService.EditProject(new EditProjectRequest
            {
                ProjectId = projectId,
                ProjectName = "Проект с правилами подачи заявки",
                ProjectAnnounce = projectAnnounce,
            });
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            await projectService.SetClaimSettings(projectId, projectInfo.ClaimSettings, claimApplyRules);
        });

        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var details = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectDetails(projectId);

            details.ClaimApplyRules.Value.ShouldBe(claimApplyRules);
            details.ProjectDescription.Value.ShouldBe(projectAnnounce);
        });
    }

    [Fact]
    public async Task GetProjectDetails_ReturnsPreferentialFeeConditions()
    {
        UserIdentification masterId;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект с льготным взносом");
        }

        var conditions = "Студентам дневных отделений";

        // Условий в БД ещё нет — домен получает пустой markdown, а не null.
        await factory.Services.RunAsAsync(masterId, async sp =>
            (await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectDetails(projectId))
                .PreferentialFeeConditions.Value.ShouldBe(""));

        await factory.Services.RunAsAsync(masterId, async sp =>
            await sp.GetRequiredService<IProjectFinanceSettingsService>().SaveGlobalSettings(new SetFinanceSettingsRequest
            {
                ProjectId = projectId,
                WarnOnOverPayment = true,
                PreferentialFeeEnabled = true,
                PreferentialFeeConditions = conditions,
            }));

        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var details = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectDetails(projectId);

            details.PreferentialFeeConditions.Value.ShouldBe(conditions);
        });
    }
}
