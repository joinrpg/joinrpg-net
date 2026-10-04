using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Web.Games.Projects;
using JoinRpg.Web.ProjectMasterTools.Settings;

namespace JoinRpg.IntegrationTest.Scenarios;

public class ProjectTimeZoneScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task CreateProject_FromForm_SavesSelectedTimeZone()
    {
        var masterId = await CreateMaster();

        var result = await factory.Services.RunAsAsync(masterId, sp => sp.GetRequiredService<IProjectCreateClient>().CreateProject(
            new ProjectCreateViewModel()
            {
                ProjectName = "Проект в Екатеринбурге",
                RulesApproved = true,
                ProjectType = ProjectTypeViewModel.Larp,
                KogdaIgraChoice = KogdaIgraLinkChoiceViewModel.ShouldNotBeOnKogdaIgra,
                TimeZoneId = "Asia/Yekaterinburg",
            }));

        result.Error.ShouldBeNull();
        var projectId = result.ProjectId.ShouldNotBeNull();

        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            projectInfo.TimeZone.Id.ShouldBe("Asia/Yekaterinburg");
        });
    }

    [Fact]
    public async Task SettingsPanel_SaveAndLoad_RoundTripsTimeZone()
    {
        var masterId = await CreateMaster();
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId, "Проект с часовым поясом");
        }

        var initial = await factory.Services.RunAsAsync(masterId, sp => sp.GetRequiredService<IProjectSettingsClient>().GetTimeZoneSettings(projectId));
        initial.TimeZoneId.ShouldBe("Europe/Moscow");

        await factory.Services.RunAsAsync(masterId, sp => sp.GetRequiredService<IProjectSettingsClient>().SaveTimeZoneSettings(
            new ProjectTimeZoneSettingsViewModel()
            {
                ProjectId = projectId,
                ProjectStatus = initial.ProjectStatus,
                TimeZoneId = "Asia/Vladivostok",
            }));

        var saved = await factory.Services.RunAsAsync(masterId, sp => sp.GetRequiredService<IProjectSettingsClient>().GetTimeZoneSettings(projectId));
        saved.TimeZoneId.ShouldBe("Asia/Vladivostok");
    }

    private async Task<UserIdentification> CreateMaster()
    {
        using var scope = factory.Services.CreateScope();
        return await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
    }
}
