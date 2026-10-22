using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata.Badges;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.ProjectMetadata;
using JoinRpg.Web.ProjectCommon;

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
    private const string PrintableFieldName = "Поле для распечатки";
    private const string PrintableFieldValue = "Значение для распечатки";

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

        // Не только заголовок блока, но и само значение: на одном заголовке тест прошёл бы и при
        // пустом наборе полей — то есть не заметил бы, если бы отбор полей печати уехал.
        text.ShouldContain(PrintableFieldName);
        text.ShouldContain(PrintableFieldValue);
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
        text.ShouldNotContain(PrintableFieldValue);
    }

    [Fact]
    public async Task Badges_PrintFieldValueAndStaticText()
    {
        var context = await SeedAsync();
        const string staticText = "Текст на каждом бейдже";

        var templateId = await factory.Services.RunAsAsync(
            context.MasterId,
            sp => sp.GetRequiredService<IProjectBadgeTemplateService>().CreateAsync(
                context.ProjectId,
                "Бейдж для печати",
                new BadgeLayout(90, 55, null,
                [
                    new BadgeLine(context.PrintableFieldId.ProjectFieldId, null, null, 10, 8, 20, Bold: true),
                    new BadgeLine(null, staticText, null, 40, 8, 12, Bold: false),
                ])));

        var text = await GetPrintPageText(context, "badges", $"&badgeTemplateId={templateId.ProjectBadgeTemplateId}");

        text.ShouldContain(PrintableFieldValue);
        text.ShouldContain(staticText);

        // Страница без layout подключает модуль подгонки шрифта и бандл scoped CSS сама. Если путь к
        // ним уедет, бейджи напечатаются без позиционирования и максимальным шрифтом — а страница
        // по-прежнему отдаст 200.
        var page = await (await context.MasterClient.GetAsync(BadgesUrl(context, templateId.ProjectBadgeTemplateId))).AsHtmlDocument();
        var assets = page.DocumentNode.SelectNodes("//script[@src]|//link[@rel='stylesheet']")
            .Select(node => node.GetAttributeValue("src", null) ?? node.GetAttributeValue("href", ""))
            .ToList();
        foreach (var asset in assets.Where(a => a.Contains("printable-badge") || IsScopedCssBundle(a)))
        {
            (await context.MasterClient.GetAsync(asset)).StatusCode.ShouldBe(HttpStatusCode.OK, asset);
        }
        assets.ShouldContain(a => a.Contains("printable-badge"));
        assets.ShouldContain(a => IsScopedCssBundle(a), string.Join(" | ", assets));
    }

    // MapStaticAssets вставляет в имя отпечаток: /JoinRpg.Portal.omll2v8u1y.styles.css
    private static bool IsScopedCssBundle(string url) => url.StartsWith("/JoinRpg.Portal.") && url.EndsWith(".styles.css");

    private static string BadgesUrl(SeedContext context, int templateId)
        => $"{context.ProjectId.Value}/print/badges?characterIds={Uri.EscapeDataString(new CompressedIntList([context.CharacterId.CharacterId]).ToString())}&badgeTemplateId={templateId}";

    private static async Task<string> GetPrintPageText(SeedContext context, string action, string extraQuery = "")
    {
        var characterIds = new CompressedIntList([context.CharacterId.CharacterId]);
        var url = $"{context.ProjectId.Value}/print/{action}?characterIds={Uri.EscapeDataString(characterIds.ToString())}{extraQuery}";

        var response = await context.MasterClient.GetAsync(url);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var document = await response.AsHtmlDocument();
        var body = document.DocumentNode.SelectSingleNode("//body")
            ?? throw new InvalidOperationException($"Страница {url} не содержит body");

        // Кириллица в разметке приезжает числовыми HTML-сущностями (&#x422;…), InnerText их не раскрывает.
        return WebUtility.HtmlDecode(body.InnerText);
    }

    private sealed record SeedContext(
        UserIdentification MasterId,
        ProjectIdentification ProjectId,
        ProjectFieldIdentification PrintableFieldId,
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
                return (seed, handout.Content);
            });

        var fieldId = await factory.Services.RunAsAsync(
            masterId,
            sp => sp.GetRequiredService<IFieldSetupService>().AddField(new CreateFieldRequest(
                projectId,
                ProjectFieldType.String,
                PrintableFieldName,
                fieldHint: "",
                canPlayerEdit: false,
                canPlayerView: true,
                isPublic: true,
                FieldBoundTo.Character,
                MandatoryStatus.Optional,
                showForGroups: [],
                validForNpc: true,
                includeInPrint: true,
                showForUnapprovedClaims: true,
                price: 0,
                masterFieldHint: "",
                programmaticValue: null)));

        // Значение проставляется отдельной областью видимости: метаданные проекта кешируются на
        // область, и в той, где поле только что создано, ProjectInfo о нём ещё не знает.
        await factory.Services.RunAsAsync(
            masterId,
            async sp =>
            {
                var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>()
                    .GetProjectMetadata(projectId);

                await sp.GetRequiredService<ICharacterService>().SetFields(
                    seed.TargetCharacterId,
                    new FieldLayerContainer(
                        projectInfo,
                        new Dictionary<int, string?> { [fieldId.ProjectFieldId] = PrintableFieldValue }));
            });

        var masterClient = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(), email, password);

        return new SeedContext(
            masterId,
            projectId,
            fieldId,
            seed.TargetCharacterId,
            seed.TargetCharacterName,
            seed.ElementContents[0],
            handoutContent,
            masterClient);
    }
}
