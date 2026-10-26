using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
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

    private static string BadgesUrl(SeedContext context, int? templateId)
        => $"{context.ProjectId.Value}/print/badges?characterIds={Uri.EscapeDataString(new CompressedIntList([context.CharacterId.CharacterId]).ToString())}"
            + (templateId is null ? "" : $"&badgeTemplateId={templateId}");

    // Кнопка «Бейджи» над списком персонажей ведёт без шаблона: из нескольких шаблонов мастер выбирает.
    [Fact]
    public async Task Badges_WithoutTemplate_OffersChoiceWhenSeveral()
    {
        var context = await SeedAsync();
        foreach (var name in new[] { "Шаблон для игроков", "Шаблон для мастеров" })
        {
            _ = await factory.Services.RunAsAsync(
                context.MasterId,
                sp => sp.GetRequiredService<IProjectBadgeTemplateService>().CreateAsync(
                    context.ProjectId, name, new BadgeLayout(90, 55, null, [new BadgeLine(null, name, null, 10, 8, 20, Bold: false)])));
        }

        var text = await GetPrintPageText(context, "badges");

        text.ShouldContain("Выберите шаблон бейджа");
        text.ShouldContain("Шаблон для игроков");
        text.ShouldContain("Шаблон для мастеров");

        // Ссылки выбора несут и шаблон, и персонажей — по ним открывается сама печать.
        var choice = await (await context.MasterClient.GetAsync(BadgesUrl(context, templateId: null))).AsHtmlDocument();
        var link = WebUtility.HtmlDecode(choice.DocumentNode.SelectNodes("//a[contains(@href, 'badgeTemplateId=')]").First().GetAttributeValue("href", ""));
        var printed = await (await context.MasterClient.GetAsync(link)).AsHtmlDocument();
        printed.DocumentNode.SelectSingleNode("//title").InnerText.ShouldStartWith("Бейджи");
        printed.DocumentNode.SelectNodes("//*[contains(@class, 'join-printable-badge-line')]").ShouldNotBeNull();
    }

    // Нет шаблонов — пояснение; ровно один — печать сразу, без страницы выбора.
    [Fact]
    public async Task Badges_WithoutTemplate_NoneOrSingle()
    {
        var context = await SeedAsync();
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, context.MasterId, "Проект без шаблонов бейджей");
        }
        var url = $"{projectId.Value}/print/badges?characterIds={Uri.EscapeDataString(new CompressedIntList([1]).ToString())}";

        var none = await (await context.MasterClient.GetAsync(url)).AsHtmlDocument();
        WebUtility.HtmlDecode(none.DocumentNode.InnerText).ShouldContain("нет шаблонов бейджей");

        _ = await factory.Services.RunAsAsync(
            context.MasterId,
            sp => sp.GetRequiredService<IProjectBadgeTemplateService>().CreateAsync(
                projectId, "Единственный шаблон", new BadgeLayout(90, 55, null, [])));

        var single = await (await context.MasterClient.GetAsync(url)).AsHtmlDocument();
        WebUtility.HtmlDecode(single.DocumentNode.SelectSingleNode("//title").InnerText).ShouldBe("Бейджи: Единственный шаблон");
    }

    // Права в WebApi проверяются по projectId из query — шаблон чужого проекта по своему projectId не отдаётся:
    // ни по числу (его склеивают с проверенным проектом), ни по полному id (его отклоняет биндер типизированных id).
    [Fact]
    public async Task BadgeTemplateApi_DoesNotLeakOtherProjectTemplate()
    {
        var context = await SeedAsync();
        const string secret = "Шаблон чужого проекта";
        ProjectBadgeTemplateIdentification foreignId;
        using (var scope = factory.Services.CreateScope())
        {
            var otherMaster = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            var otherProject = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, otherMaster, "Чужой проект с бейджами");
            foreignId = await factory.Services.RunAsAsync(
                otherMaster,
                sp => sp.GetRequiredService<IProjectBadgeTemplateService>().CreateAsync(
                    otherProject, secret, new BadgeLayout(90, 55, null, [])));
        }

        foreach (var templateParam in new[] { foreignId.ProjectBadgeTemplateId.ToString(), foreignId.ToString() })
        {
            var response = await context.MasterClient.GetAsync(
                $"webapi/badge-template/getbyid?projectId={context.ProjectId.Value}&badgeTemplateId={Uri.EscapeDataString(templateParam)}");

            response.StatusCode.ShouldNotBe(HttpStatusCode.OK, templateParam);
            (await response.Content.ReadAsStringAsync()).ShouldNotContain(secret);
        }
    }

    // Со страницы шаблона — печать всех активных персонажей этим шаблоном.
    [Fact]
    public async Task TemplatePage_LinksToPrintingAllCharacters()
    {
        var context = await SeedAsync();
        const string staticText = "Печать со страницы шаблона";
        var templateId = await factory.Services.RunAsAsync(
            context.MasterId,
            sp => sp.GetRequiredService<IProjectBadgeTemplateService>().CreateAsync(
                context.ProjectId,
                "Шаблон для печати всех",
                new BadgeLayout(90, 55, null, [new BadgeLine(null, staticText, null, 10, 8, 20, Bold: false)])));

        var page = await (await context.MasterClient.GetAsync(
            $"{context.ProjectId.Value}/tools/badges/edit/{templateId.ProjectBadgeTemplateId}")).AsHtmlDocument();
        var link = page.DocumentNode.SelectNodes($"//a[contains(@href, 'badgeTemplateId={templateId.ProjectBadgeTemplateId}')]")
            .ShouldNotBeNull("Нет ссылки на печать этим шаблоном").First();

        var printed = await (await context.MasterClient.GetAsync(WebUtility.HtmlDecode(link.GetAttributeValue("href", "")))).AsHtmlDocument();
        WebUtility.HtmlDecode(printed.DocumentNode.InnerText).ShouldContain(staticText);
    }

    // Пачки на «Распечатках» и конверты С5 — без шаблонов (слотов): печатать их незачем.
    [Fact]
    public async Task PrintIndex_SkipsSlots()
    {
        var context = await SeedAsync();
        const string slotName = DefaultSlotName;

        var index = await (await context.MasterClient.GetAsync($"{context.ProjectId.Value}/print/index")).AsHtmlDocument();
        var stickersLink = index.DocumentNode.SelectNodes("//a[contains(@href, '/print/stickers')]")
            .ShouldNotBeNull("Нет ссылки на печать наклеек").First();
        var stickers = await (await context.MasterClient.GetAsync(WebUtility.HtmlDecode(stickersLink.GetAttributeValue("href", "")))).AsHtmlDocument();
        var stickersText = WebUtility.HtmlDecode(stickers.DocumentNode.InnerText);
        stickersText.ShouldContain(context.CharacterName);
        stickersText.ShouldNotContain(slotName);

        var envelopes = await (await context.MasterClient.GetAsync($"{context.ProjectId.Value}/print/envelopesc5")).AsHtmlDocument();
        var envelopesText = WebUtility.HtmlDecode(envelopes.DocumentNode.InnerText);
        envelopesText.ShouldContain(context.CharacterName);
        envelopesText.ShouldNotContain(slotName);
    }

    // «Распечатать все» над списком персонажей шлёт весь список, а в нём бывают шаблоны — печать их отсекает сама.
    [Theory]
    [InlineData("characterlist")]
    [InlineData("handoutonly")]
    [InlineData("stickers")]
    public async Task PrintByIds_SkipsSlots(string action)
    {
        var context = await SeedAsync();

        var response = await context.MasterClient.GetAsync(
            $"{context.ProjectId.Value}/print/{action}?characterIds={await CharacterAndSlotIds(context)}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var text = WebUtility.HtmlDecode((await response.AsHtmlDocument()).DocumentNode.InnerText);

        text.ShouldContain(context.CharacterName);
        text.ShouldNotContain(DefaultSlotName);
    }

    [Fact]
    public async Task BadgesByIds_SkipSlots()
    {
        var context = await SeedAsync();
        var templateId = await factory.Services.RunAsAsync(
            context.MasterId,
            sp => sp.GetRequiredService<IProjectBadgeTemplateService>().CreateAsync(
                context.ProjectId,
                "Бейдж без шаблонов персонажей",
                new BadgeLayout(90, 55, null, [new BadgeLine(null, "Бейдж", null, 10, 8, 20, Bold: false)])));

        var page = await (await context.MasterClient.GetAsync(
            $"{context.ProjectId.Value}/print/badges?characterIds={await CharacterAndSlotIds(context)}&badgeTemplateId={templateId.ProjectBadgeTemplateId}"))
            .AsHtmlDocument();

        page.DocumentNode.SelectNodes("//div[@class='join-printable-badge']").ShouldNotBeNull().Count.ShouldBe(1);
    }

    // Шаблон, который создаётся вместе с проектом.
    private const string DefaultSlotName = "Хочу на игру";

    private async Task<string> CharacterAndSlotIds(SeedContext context)
    {
        var slotId = await factory.Services.RunAsAsync(
            context.MasterId,
            async sp => (await sp.GetRequiredService<ICharacterInfoRepository>().GetCharactersForList(context.ProjectId))
                .Single(character => character.CharacterName == DefaultSlotName).Id);
        return Uri.EscapeDataString(new CompressedIntList([context.CharacterId.CharacterId, slotId.CharacterId]).ToString());
    }

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
