using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.IntegrationTest.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// POST-ручки сюжетов под мастером: создание и правка папки, создание и правка вводной, удаление
/// папки.
/// </summary>
/// <remarks>
/// Смоук по страницам (<see cref="AllGetPagesSmokeScenario"/>) ходит только по GET, поэтому
/// мутирующие ручки сюжетов не были покрыты вообще — а в прод-логах именно они были самой большой
/// группой ленивых догрузок <c>ProjectAcls</c>: права проверялись через
/// <c>folder.Project.ProjectAcls</c> (#4989).
///
/// Проверку ленивых загрузок делает не сам тест, а <c>LazyLoadAssertingHandler</c> на клиенте
/// фабрики: маршрута нет в <c>lazy-loads-baseline.json</c> — значит догрузок быть не должно
/// (см. docs/lazy-loads-baseline.md). Поэтому тест нарочно ходит через HTTP на реальную БД, а не
/// зовёт <c>IPlotService</c> напрямую: так маршрут попадает в снапшот и защищён от регрессий
/// вместе со всеми остальными.
/// </remarks>
public class PlotMutationPagesScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task PlotMutations_WorkUnderMaster()
    {
        var (masterId, email) = await CreateMasterAsync();
        ProjectIdentification projectId;
        PlotSeedResult seed;
        using (var scope = factory.Services.CreateScope())
        {
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект для правки сюжетов через ручки");
        }

        seed = await factory.Services.RunAsAsync(
            masterId,
            sp => TestPlotHelpers.SeedPlotFolderAsync(sp, projectId, elementCount: 1));

        // Редиректы намеренно не проходим: успех мутации — это 302 на страницу сюжета, а
        // переход по нему замерил бы ленивые загрузки уже другого маршрута.
        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }),
            email,
            followsRedirects: false);

        var project = projectId.Value;
        var folderId = seed.PlotFolderId.PlotFolderId;
        var elementId = seed.ElementIds[0].PlotElementId;

        await PostAsync(
            client,
            formUrl: $"{project}/plots/Create",
            postUrl: $"{project}/plots/Create",
            ["ProjectId", project.ToString()],
            ["PlotFolderTitleAndTags", "Сюжет через ручку #тег"],
            ["TodoField", "надо доделать"]);

        await PostAsync(
            client,
            formUrl: $"{project}/plots/Edit?plotFolderId={folderId}",
            postUrl: $"{project}/plots/Edit",
            ["ProjectId", project.ToString()],
            ["PlotFolderId", folderId.ToString()],
            ["PlotFolderTitleAndTags", "Переименованный сюжет #другойтег"],
            ["TodoField", ""]);

        await PostAsync(
            client,
            formUrl: $"{project}/plots/CreateElement?plotFolderId={folderId}",
            postUrl: $"{project}/plots/CreateElement",
            ["projectId", project.ToString()],
            ["plotFolderId", folderId.ToString()],
            ["content", "Текст новой вводной"],
            ["todoField", ""],
            ["elementType", "RegularPlot"],
            ["publishNow", "false"],
            ["isMasterOnly", "false"]);

        await PostAsync(
            client,
            // У страницы вводной в query лежит составной типизированный id, а не число.
            formUrl: $"{project}/plots/EditElement?elementId={Uri.EscapeDataString(seed.ElementIds[0].ToString())}",
            postUrl: $"{project}/plots/EditElement",
            ["projectId", project.ToString()],
            ["plotFolderId", folderId.ToString()],
            ["plotelementid", elementId.ToString()],
            ["content", "Исправленный текст вводной"],
            ["todoField", ""],
            ["isMasterOnly", "false"]);

        await PostAsync(
            client,
            formUrl: $"{project}/plots/Delete?plotFolderId={folderId}",
            postUrl: $"{project}/plots/Delete",
            ["projectId", project.ToString()],
            ["plotFolderId", folderId.ToString()]);

        // Папка действительно удалена: без этой проверки тест доказывал бы только то, что ручки
        // отвечают редиректом.
        var deletedFolderPage = await client.GetAsync($"{project}/plots/Edit?plotFolderId={folderId}");
        deletedFolderPage.StatusCode.ShouldBe(HttpStatusCode.OK);
        var deleted = await deletedFolderPage.AsHtmlDocument();
        deleted.DocumentNode.InnerText.ShouldNotContain("Переименованный сюжет");
    }

    /// <summary>
    /// Отправляет форму: сначала GET страницы за antiforgery-токеном, потом POST. Ответ должен быть
    /// редиректом — контроллеры сюжетов на ошибке отдают 200 с той же формой.
    /// </summary>
    private static async Task PostAsync(
        HttpClient client,
        string formUrl,
        string postUrl,
        params string[][] fields)
    {
        var token = await client.GetAntiforgeryTokenAsync(formUrl);

        var response = await client.PostFormAsync(
            postUrl,
            token,
            [.. fields.Select(field => (field[0], field[1]))]);

        response.StatusCode.ShouldBe(
            HttpStatusCode.Found,
            $"POST {postUrl} не сохранил изменения: {await response.DescribeValidationErrorsAsync()}");
    }

    private async Task<(UserIdentification MasterId, string Email)> CreateMasterAsync()
    {
        using var scope = factory.Services.CreateScope();
        return await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
    }
}
