using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Страховочная сетка на HTTP-уровне для GET-страниц <c>PlotController</c>.
/// </summary>
/// <remarks>
/// Тест намеренно проверяет не только код ответа, но и то, что на странице отрисованы
/// текст вводной и имя таргет-персонажа. Таргеты вводных (<c>TargetCharacters</c>/<c>TargetGroups</c>)
/// приезжают ленивой загрузкой, и рефакторинг <c>IPlotRepository.GetPlotFolderAsync</c>
/// легче всего ломает именно их: страница при этом продолжает отдавать 200, но становится пустой.
/// POST-эндпоинты и их error-path сознательно не покрываются.
/// </remarks>
public class PlotPagesSmokeScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    /// <summary>Страницы сюжетов, которые проверяет смоук.</summary>
    public enum PlotPage
    {
        /// <summary>Просмотр/редактирование папки сюжета со списком вводных.</summary>
        Edit,
        /// <summary>Подтверждение удаления папки сюжета (тоже показывает список вводных).</summary>
        Delete,
        /// <summary>Редактирование одной вводной.</summary>
        EditElement,
        /// <summary>Просмотр конкретной версии вводной.</summary>
        ShowElementVersion,
        /// <summary>Создание вводной копированием существующей.</summary>
        CreateElementCopy,
    }

    [Theory]
    [InlineData(PlotPage.Edit)]
    [InlineData(PlotPage.Delete)]
    [InlineData(PlotPage.EditElement)]
    [InlineData(PlotPage.ShowElementVersion)]
    [InlineData(PlotPage.CreateElementCopy)]
    public async Task PlotPage_OpensAndShowsElementWithTarget(PlotPage page)
    {
        var context = await GetSeedAsync();
        var seed = context.Seed;
        var projectId = context.ProjectId.Value;
        var elementId = seed.ElementIds[0];
        var expectedContent = seed.ElementContents[0];

        var url = page switch
        {
            PlotPage.Edit =>
                $"{projectId}/plots/edit?plotFolderId={seed.PlotFolderId.PlotFolderId}",
            PlotPage.Delete =>
                $"{projectId}/plots/delete?plotFolderId={seed.PlotFolderId.PlotFolderId}",
            PlotPage.EditElement =>
                $"{projectId}/plots/editElement?elementId={Uri.EscapeDataString(elementId.ToString())}",
            PlotPage.ShowElementVersion =>
                $"{projectId}/plots/showElementVersion?plotFolderId={elementId.PlotFolderId.PlotFolderId}"
                    + $"&plotElementId={elementId.PlotElementId}&version=1",
            PlotPage.CreateElementCopy =>
                $"{projectId}/plots/createElement?copyFrom={Uri.EscapeDataString(elementId.ToString())}",
            _ => throw new ArgumentOutOfRangeException(nameof(page)),
        };

        var response = await context.MasterClient.GetAsync(url);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Проверяем именно текст страницы, а не сырой HTML: так не поймаем совпадение
        // в атрибуте, в base64-состоянии Blazor или в скрытом поле формы.
        var document = await response.AsHtmlDocument();
        var body = document.DocumentNode.SelectSingleNode("//body")
            ?? throw new InvalidOperationException($"Страница {url} не содержит body");
        // Кириллица в разметке приезжает числовыми HTML-сущностями (&#x422;…), InnerText их не раскрывает.
        var text = WebUtility.HtmlDecode(body.InnerText);

        text.ShouldContain(expectedContent);
        text.ShouldContain(seed.TargetCharacterName);

        // Мало того, что имя персонажа где-то есть на странице — оно должно быть отрисовано
        // именно как таргет вводной. Иначе тест пройдёт и на странице, где таргеты потерялись,
        // а имя попало в общий список персонажей проекта.
        switch (page)
        {
            case PlotPage.Edit or PlotPage.Delete or PlotPage.ShowElementVersion:
                // PlotTargetDisplay рисует таргет ссылкой на персонажа.
                body.SelectSingleNode(
                        $"//a[contains(@href, '/character/{seed.TargetCharacterId.CharacterId}/details')]")
                    .ShouldNotBeNull($"На странице {url} нет ссылки на таргет-персонажа");
                break;
            case PlotPage.EditElement or PlotPage.CreateElementCopy:
                // CharacterSelector рисует таргет выбранным пунктом списка.
                body.SelectNodes("//option[@selected]")
                    .ShouldNotBeNull($"На странице {url} нет выбранных таргетов")
                    .Select(o => o.GetAttributeValue("value", ""))
                    .ShouldContain(seed.TargetCharacterId.ToString());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(page));
        }
    }

    /// <summary>
    /// Вводные рисуются статическим компонентом PlotElementPanelList: свёрнутыми панелями одного
    /// аккордеона, а остров с кнопками вводной внутри панели доезжает до страницы.
    /// </summary>
    [Fact]
    public async Task Edit_RendersElementsAsAccordionPanelsWithIslands()
    {
        var context = await GetSeedAsync();
        var seed = context.Seed;
        var url = $"{context.ProjectId.Value}/plots/edit?plotFolderId={seed.PlotFolderId.PlotFolderId}";

        var response = await context.MasterClient.GetAsync(url);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var document = await response.AsHtmlDocument();

        var panels = seed.ElementIds
            .Select(elementId => document.DocumentNode.SelectSingleNode($"//details[@id='panelPlotElement{elementId.PlotElementId}']")
                ?? throw new InvalidOperationException($"На странице {url} нет панели вводной {elementId}"))
            .ToList();

        // Аккордеон — это общее непустое имя у всех панелей.
        panels.Select(p => p.GetAttributeValue("name", "")).Distinct().ShouldHaveSingleItem().ShouldNotBeEmpty();

        foreach (var (panel, elementId) in panels.Zip(seed.ElementIds))
        {
            panel.Attributes.Contains("open").ShouldBeFalse();
            panel.SelectSingleNode("summary").ShouldNotBeNull();
            panel.SelectNodes(".//comment()")
                .ShouldNotBeNull($"В панели вводной {elementId} нет острова")
                .ShouldContain(c => c.InnerHtml.Contains("PlotElementControls"));
        }
    }

    /// <summary>Просмотр версии показывает одну вводную — сразу раскрытой и вне аккордеона.</summary>
    [Fact]
    public async Task ShowElementVersion_RendersOpenPanelWithoutAccordion()
    {
        var context = await GetSeedAsync();
        var elementId = context.Seed.ElementIds[0];
        var url = $"{context.ProjectId.Value}/plots/showElementVersion?plotFolderId={elementId.PlotFolderId.PlotFolderId}"
            + $"&plotElementId={elementId.PlotElementId}&version=1";

        var response = await context.MasterClient.GetAsync(url);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var document = await response.AsHtmlDocument();
        var panel = document.DocumentNode.SelectSingleNode($"//details[@id='panelPlotElement{elementId.PlotElementId}']")
            ?? throw new InvalidOperationException($"На странице {url} нет панели вводной");
        panel.Attributes.Contains("open").ShouldBeTrue();
        panel.Attributes.Contains("name").ShouldBeFalse();
    }

    /// <summary>
    /// Копирование раздатки должно открывать экран создания именно раздатки:
    /// иначе мастер копирует раздатку и молча получает обычную вводную.
    /// </summary>
    [Fact]
    public async Task CreateElementCopy_Handout_PreselectsHandoutType()
    {
        var context = await GetSeedAsync();
        var projectId = context.ProjectId.Value;

        var url = $"{projectId}/plots/createElement?copyFrom={Uri.EscapeDataString(context.HandoutElementId.ToString())}";

        var response = await context.MasterClient.GetAsync(url);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var document = await response.AsHtmlDocument();
        var handoutRadio = document.DocumentNode.SelectSingleNode("//input[@type='radio'][@value='Handout']")
            ?? throw new InvalidOperationException($"На странице {url} нет радиокнопки раздатки");
        handoutRadio.Attributes.Contains("checked").ShouldBeTrue(
            "При копировании раздатки тип «Элемент раздатки» должен быть выбран по умолчанию");
        document.DocumentNode.SelectSingleNode("//input[@type='radio'][@value='RegularPlot']")
            .ShouldNotBeNull()
            .Attributes.Contains("checked").ShouldBeFalse();
    }

    private sealed record SeedContext(
        ProjectIdentification ProjectId,
        PlotSeedResult Seed,
        PlotElementIdentification HandoutElementId,
        HttpClient MasterClient);

    // Сид одинаков для всех кейсов Theory, а xUnit создаёт новый экземпляр класса на каждый кейс,
    // поэтому кешируем его статически: JoinApplicationFactory всё равно общая на класс.
    private static Task<SeedContext>? seedTask;
    private static readonly SemaphoreSlim SeedLock = new(1, 1);

    private async Task<SeedContext> GetSeedAsync()
    {
        await SeedLock.WaitAsync();
        try
        {
            seedTask ??= SeedAsync();
        }
        finally
        {
            _ = SeedLock.Release();
        }

        return await seedTask;
    }

    private async Task<SeedContext> SeedAsync()
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
                scope.ServiceProvider, masterId, "Проект с сюжетами");
        }

        var (seed, handoutElementId) = await factory.Services.RunAsAsync(
            masterId,
            async sp =>
            {
                var seed = await TestPlotHelpers.SeedPlotFolderAsync(sp, projectId, elementCount: 2);
                var handout = await TestPlotHelpers.SeedHandoutAsync(sp, seed.PlotFolderId, seed.TargetCharacterId);
                return (seed, handout.ElementId);
            });

        var masterClient = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(), email, password);

        return new SeedContext(projectId, seed, handoutElementId, masterClient);
    }
}
