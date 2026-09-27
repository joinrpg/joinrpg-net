using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Страницы сюжета показывают ту версию вводной, которую попросили, и дают перейти к соседним.
/// </summary>
/// <remarks>
/// Тесты написаны перед сужением выборки версий в репозитории: сейчас
/// <c>IPlotRepository</c> тянет тексты всех версий всех вводных, хотя DTO несёт только
/// отображаемую версию плюс даты соседних. Сужение ломается тихо — все прежние проверки смотрели
/// исключительно на последнюю версию, так что подмена «любая версия → последняя» осталась бы
/// незамеченной. Здесь закрыто именно это.
///
/// Проверки идут по HTTP, а не по репозиторию, потому что менять предстоит SQL: юнит-тест над уже
/// собранным графом такую замену не увидит.
/// </remarks>
public class PlotVersionsScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    private const int VersionCount = 3;

    [Fact]
    public async Task ShowElementVersion_ShowsRequestedVersionOnly()
    {
        var ctx = await SeedAsync(publishVersion: null);

        for (var version = 0; version < VersionCount; version++)
        {
            var text = await GetPageTextAsync(ctx, VersionUrl(ctx, version));

            text.ShouldContain(
                ctx.Element.VersionContents[version],
                customMessage: $"Страница версии {version} не показала её текст");

            foreach (var other in Enumerable.Range(0, VersionCount).Where(v => v != version))
            {
                text.ShouldNotContain(
                    ctx.Element.VersionContents[other],
                    customMessage: $"Страница версии {version} показала заодно текст версии {other}");
            }
        }
    }

    [Fact]
    public async Task ShowElementVersion_LinksToNeighbourVersions()
    {
        // Без публикации: ссылка на предыдущую версию рисуется только когда опубликованной версии нет
        // либо она не является предыдущей — см. условие в EditElementPartial.cshtml.
        var ctx = await SeedAsync(publishVersion: null);

        var middle = await GetPageAsync(ctx, VersionUrl(ctx, 1));
        HasVersionLink(middle, ctx, 0).ShouldBeTrue("На средней версии нет ссылки на предыдущую");
        HasVersionLink(middle, ctx, 2).ShouldBeTrue("На средней версии нет ссылки на следующую");

        var last = await GetPageAsync(ctx, VersionUrl(ctx, VersionCount - 1));
        HasVersionLink(last, ctx, VersionCount).ShouldBeFalse("На последней версии не должно быть ссылки на следующую");

        var first = await GetPageAsync(ctx, VersionUrl(ctx, 0));
        HasVersionLink(first, ctx, -1).ShouldBeFalse("На первой версии не должно быть ссылки на предыдущую");
    }

    [Fact]
    public async Task EditElement_ShowsRequestedVersionButTodoFromLast()
    {
        var ctx = await SeedAsync(publishVersion: null);

        var text = await GetPageTextAsync(
            ctx,
            $"{ctx.ProjectId.Value}/plots/editElement"
                + $"?elementId={Uri.EscapeDataString(ctx.Element.ElementId.ToString())}&version=0");

        text.ShouldContain(
            ctx.Element.VersionContents[0],
            customMessage: "Редактор не показал текст запрошенной версии");
        text.ShouldContain(
            ctx.Element.LastVersionTodoField,
            customMessage: "Редактор показал TODO не от последней версии");
    }

    [Fact]
    public async Task PlotFolder_ShowsHasNewVersionStatus_WhenPublishedIsNotLast()
    {
        var ctx = await SeedAsync(publishVersion: 1);

        var text = await GetPageTextAsync(
            ctx,
            $"{ctx.ProjectId.Value}/plots/edit?plotFolderId={ctx.PlotFolderId.PlotFolderId}");

        // «Доработка» — бейдж PlotStatus.HasNewVersion: опубликована версия 1, а последняя — 2.
        text.ShouldContain(
            "Доработка",
            customMessage: "Вводная с опубликованной не последней версией не помечена как доработка");
    }

    [Fact]
    public async Task PlotFolder_ShowsElementsInStoredOrder()
    {
        var ctx = await SeedAsync(publishVersion: null);

        var text = await GetPageTextAsync(
            ctx,
            $"{ctx.ProjectId.Value}/plots/edit?plotFolderId={ctx.PlotFolderId.PlotFolderId}");

        // В папке помимо многоверсионной вводной лежат вводные из общего сида — проверяем,
        // что они идут в том порядке, в котором создавались.
        var positions = ctx.Seed.ElementContents
            .Select(content => text.IndexOf(content, StringComparison.Ordinal))
            .ToArray();

        positions.ShouldAllBe(p => p >= 0, "Не все вводные папки попали на страницу");
        positions.ShouldBe([.. positions.Order()], "Вводные показаны не в том порядке, в котором лежат в папке");
    }

    private string VersionUrl(TestContext ctx, int version)
        => $"{ctx.ProjectId.Value}/plots/showElementVersion"
            + $"?plotFolderId={ctx.PlotFolderId.PlotFolderId}"
            + $"&plotElementId={ctx.Element.ElementId.PlotElementId}&version={version}";

    /// <summary>Есть ли на странице ссылка на указанную версию этой же вводной.</summary>
    /// <remarks>
    /// Разбирает query, а не ищет подстроку: в разметке параметры называются <c>PlotElementId</c>
    /// и <c>Version</c> с большой буквы, а XPath <c>contains</c> регистрозависим — на подстроках
    /// проверка молча не находила существующие ссылки. Сравнение по параметрам заодно не путает
    /// <c>Version=1</c> с <c>Version=11</c>.
    /// </remarks>
    private static bool HasVersionLink(HtmlAgilityPack.HtmlNode body, TestContext ctx, int version)
    {
        var links = body.SelectNodes("//a[@href]") ?? new HtmlAgilityPack.HtmlNodeCollection(body);
        return links
            .Select(a => WebUtility.HtmlDecode(a.GetAttributeValue("href", "")))
            .Where(href => href.Contains("howelementversion", StringComparison.OrdinalIgnoreCase))
            .Select(href => href[(href.IndexOf('?', StringComparison.Ordinal) + 1)..].Split('&'))
            .Any(query =>
                query.Contains($"PlotElementId={ctx.Element.ElementId.PlotElementId}", StringComparer.OrdinalIgnoreCase)
                && query.Contains($"Version={version}", StringComparer.OrdinalIgnoreCase));
    }

    private async Task<HtmlAgilityPack.HtmlNode> GetPageAsync(TestContext ctx, string url)
    {
        var response = await ctx.MasterClient.GetAsync(url);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, $"Страница {url} не открылась");

        var document = await response.AsHtmlDocument();
        return document.DocumentNode.SelectSingleNode("//body")
            ?? throw new InvalidOperationException($"Страница {url} не содержит body");
    }

    private async Task<string> GetPageTextAsync(TestContext ctx, string url)
        // Кириллица приезжает числовыми HTML-сущностями, InnerText их не раскрывает.
        => WebUtility.HtmlDecode((await GetPageAsync(ctx, url)).InnerText);

    private sealed record TestContext(
        ProjectIdentification ProjectId,
        PlotFolderIdentification PlotFolderId,
        PlotSeedResult Seed,
        PlotVersionsSeedResult Element,
        HttpClient MasterClient);

    private async Task<TestContext> SeedAsync(int? publishVersion)
    {
        const string password = "Password123!";

        UserIdentification masterId;
        string email;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            (masterId, email) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(
                scope.ServiceProvider, password: password);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект с версиями вводных");
        }

        var seed = await factory.Services.RunAsAsync(
            masterId,
            sp => TestPlotHelpers.SeedPlotFolderAsync(sp, projectId, elementCount: 2));

        var element = await factory.Services.RunAsAsync(
            masterId,
            sp => TestPlotHelpers.SeedElementWithVersionsAsync(
                sp, seed.PlotFolderId, seed.TargetCharacterId, VersionCount, publishVersion));

        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(), email, password);

        return new TestContext(projectId, seed.PlotFolderId, seed, element, client);
    }
}
