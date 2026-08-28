using System.Data.Entity;
using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Projects;
using JoinRpg.DomainTypes;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.IntegrationTests.TestInfrastructure;
using JoinRpg.Web.AdminTools;
using JoinRpg.Web.ProjectCommon.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

public class KogdaIgraMissingGamesScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    // Регрессия: EF6 не умел транслировать DateTime.Add(TimeSpan) в SQL и падал
    // System.NotSupportedException на реальном запросе GetProjectsForAdmin(ActiveWithoutKogdaIgra),
    // когда у проекта была активная привязка к КогдаИгра, закончившаяся давно.
    [Fact]
    public async Task GetProjectsForAdmin_ActiveWithoutKogdaIgra_ProjectWithLongEndedActiveGame_ShouldAppearInList()
    {
        const string adminEmail = "admin-ki@example.com";
        const string password = "Password123!";

        UserIdentification adminId;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            (adminId, _) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(
                scope.ServiceProvider, adminEmail, password);

            var myDb = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            var dbAdmin = myDb.Set<JoinRpg.DataModel.User>().Include("Auth").Single(u => u.Email == adminEmail);
            dbAdmin.Auth.IsAdmin = true;
            await myDb.SaveChangesAsync();

            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, adminId, "Проект-сериал с давно закончившейся игрой");
        }

        // Активная привязка к КогдаИгра, закончившаяся 90 дней назад — как раз тот случай,
        // на котором предикат вызывал DateTime.Add(TimeSpan) при трансляции в SQL.
        using (var scope = factory.Services.CreateScope())
        {
            var myDb = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            var project = await myDb.Set<Project>()
                .Include(p => p.KogdaIgraGames)
                .Include(p => p.Details)
                .SingleAsync(p => p.ProjectId == projectId.Value);

            // Тестовый проект создаётся с ShouldNotBeOnKogdaIgra — снимаем флаг,
            // иначе предикат исключит проект независимо от игр.
            project.Details.DisableKogdaIgraMapping = false;

            project.KogdaIgraGames.Add(new KogdaIgraGame
            {
                KogdaIgraGameId = projectId.Value + 1_000_000,
                Name = "Тестовая игра КогдаИгра",
                JsonGameData = "{}",
                Active = true,
                End = DateTime.UtcNow.AddDays(-90),
                UpdateRequestedAt = DateTimeOffset.UtcNow,
                LastUpdatedAt = DateTimeOffset.UtcNow,
            });
            await myDb.SaveChangesAsync();
        }

        var client = factory.CreateClient();
        client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(client, adminEmail, password);

        var response = await client.GetAsync(
            $"webapi/projects/GetProjectsForAdmin?criteria={ProjectSelectionCriteria.ActiveWithoutKogdaIgra}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var projects = await response.Content.ReadFromJsonAsync<List<ProjectAdminListItemViewModel>>();
        projects.ShouldNotBeNull();
        projects.ShouldContain(p => p.ProjectId.Value == projectId.Value);
    }
}
