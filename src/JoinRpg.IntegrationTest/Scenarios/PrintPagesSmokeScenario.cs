using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.IntegrationTests.TestInfrastructure;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Страховочная сетка на HTTP-уровне для страниц печати конвертов.
/// </summary>
/// <remarks>
/// Обе страницы собираются из одних и тех же partial-вьюх, и «только раздатка» отличается от
/// «содержимого конвертов» ровно тем, чего на ней быть не должно. Поэтому тест проверяет не только
/// то, что раздатка напечаталась, но и то, что на «только раздатке» нет загрузов и полей персонажа:
/// иначе перенос блока между <c>PrintCharacter.cshtml</c> и <c>CharacterListHeader.cshtml</c>
/// молча превратит её обратно в полный конверт (страница продолжит отдавать 200).
/// </remarks>
public class PrintPagesSmokeScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    private const string PlotsHeader = "Загрузы";
    private const string FieldsHeader = "Поля персонажа";

    [Fact]
    public async Task CharacterList_PrintsHandoutsPlotsAndFields()
    {
        var context = await SeedAsync();

        var text = await GetPrintPageText(context, "characterlist");

        text.ShouldContain(context.HandoutContent);
        text.ShouldContain(context.PlotContent);
        text.ShouldContain(PlotsHeader);
        text.ShouldContain(context.CharacterName);
        text.ShouldContain(FieldsHeader);
    }

    [Fact]
    public async Task HandoutOnly_PrintsHandoutsWithoutPlotsAndFields()
    {
        var context = await SeedAsync();

        var text = await GetPrintPageText(context, "handoutonly");

        // Заглавная страница на месте: надпись на конверте и чек-лист раздатки.
        text.ShouldContain(context.CharacterName);
        text.ShouldContain(context.HandoutContent);

        // А вот содержимого конверта быть не должно.
        text.ShouldNotContain(context.PlotContent);
        text.ShouldNotContain(PlotsHeader);
        text.ShouldNotContain(FieldsHeader);
    }

    private static async Task<string> GetPrintPageText(SeedContext context, string action)
    {
        var characterIds = new CompressedIntList([context.CharacterId.CharacterId]);
        var url = $"{context.ProjectId.Value}/print/{action}?characterIds={Uri.EscapeDataString(characterIds.ToString())}";

        var response = await context.MasterClient.GetAsync(url);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var document = await response.AsHtmlDocument();
        var body = document.DocumentNode.SelectSingleNode("//body")
            ?? throw new InvalidOperationException($"Страница {url} не содержит body");

        // Кириллица в разметке приезжает числовыми HTML-сущностями (&#x422;…), InnerText их не раскрывает.
        return WebUtility.HtmlDecode(body.InnerText);
    }

    private sealed record SeedContext(
        ProjectIdentification ProjectId,
        CharacterIdentification CharacterId,
        string CharacterName,
        string PlotContent,
        string HandoutContent,
        HttpClient MasterClient);

    // Сид общий для всех тестов класса: JoinApplicationFactory всё равно одна на класс,
    // а поднятие проекта с сюжетом — самая долгая часть теста.
    private static Task<SeedContext>? seedTask;
    private static readonly SemaphoreSlim SeedLock = new(1, 1);

    private async Task<SeedContext> SeedAsync()
    {
        await SeedLock.WaitAsync();
        try
        {
            seedTask ??= SeedCoreAsync();
        }
        finally
        {
            _ = SeedLock.Release();
        }

        return await seedTask;
    }

    private async Task<SeedContext> SeedCoreAsync()
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
                scope.ServiceProvider, masterId, "Проект для печати конвертов");
        }

        var (seed, handoutContent) = await factory.Services.RunAsAsync(
            masterId,
            async sp =>
            {
                var seed = await TestPlotHelpers.SeedPlotFolderAsync(sp, projectId, elementCount: 1);
                var handout = await TestPlotHelpers.SeedHandoutAsync(sp, seed.PlotFolderId, seed.TargetCharacterId);
                return (seed, handout);
            });

        var masterClient = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(), email, password);

        return new SeedContext(
            projectId,
            seed.TargetCharacterId,
            seed.TargetCharacterName,
            seed.ElementContents[0],
            handoutContent,
            masterClient);
    }
}
