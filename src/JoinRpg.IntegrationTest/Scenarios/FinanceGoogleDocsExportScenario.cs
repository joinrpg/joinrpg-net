using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces.ProjectAccess;
using JoinRpg.Web.ProjectMasterTools.Finance;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Панель «Синхронизация с GoogleDocs» выдаёт ссылку с токеном мастера, а <c>SummaryByMaster</c> по ней
/// отдаёт сводку без входа на сайт. Формат токена у выдачи и у разбора свой — проверяем, что они сходятся.
/// </summary>
public class FinanceGoogleDocsExportScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task ExportLinkFromPanelOpensSummaryWithoutLogin()
    {
        UserIdentification masterId;
        string email;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            (masterId, email) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId, "Проект с выгрузкой в таблицы");
        }
        var master = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(factory.CreateClient(), email);

        var export = await master.GetFromJsonAsync<FinanceGoogleDocsExportViewModel>(
            $"webapi/{projectId.Value}/finance-settings/GetGoogleDocsExport");

        var exportUri = export.ShouldNotBeNull().ExportUri;
        exportUri.IsAbsoluteUri.ShouldBeTrue();
        // Таблица Google ходит без cookie мастера — значит, и мы без них
        var summary = await factory.CreateClient().GetStringAsync(exportUri.PathAndQuery);
        summary.ShouldNotContain("Unauthorized");
        summary.ShouldContain("Мастер");
    }

    [Fact]
    public async Task RemovedMasterLinkStopsWorking()
    {
        UserIdentification ownerId, formerId;
        string formerEmail;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            ownerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            (formerId, formerEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, ownerId, "Проект со снятым мастером");
        }
        await factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IProjectAccessService>().GrantAccess(new GrantAccessRequest
            {
                ProjectId = projectId,
                UserId = formerId,
                Role = new("Казначей"),
                Description = new MarkdownString(""),
                IsPublic = false,
                Permissions = [Permission.CanManageMoney],
            }));
        var former = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(factory.CreateClient(), formerEmail);
        var export = await former.GetFromJsonAsync<FinanceGoogleDocsExportViewModel>(
            $"webapi/{projectId.Value}/finance-settings/GetGoogleDocsExport");

        await factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IProjectAccessService>().RemoveAccess(projectId, formerId, newResponsibleMasterId: null));

        // Токен снятого мастера больше не открывает сводку (ADR019)
        var summary = await factory.CreateClient().GetStringAsync(export.ShouldNotBeNull().ExportUri.PathAndQuery);
        summary.ShouldBe("Unauthorized");
    }
}
