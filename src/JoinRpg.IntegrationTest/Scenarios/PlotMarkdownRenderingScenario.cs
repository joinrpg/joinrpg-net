using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.IntegrationTests.TestInfrastructure;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Директивы вида <c>%персонаж</c> и <c>%группа</c> в тексте вводной должны разворачиваться в ссылки.
/// </summary>
/// <remarks>
/// Их рендерит <c>JoinrpgMarkdownLinkRenderer</c>, которому нужен EF-граф проекта — персонажи и группы.
/// Тест функциональный: он ловит поломку рендеринга директив, а не состав eager-загрузки.
/// Состав он проверить не может — при недогруженном графе EF6 просто подтянет персонажей лениво,
/// и директива всё равно развернётся (проверено откатом загрузки). Так что eager loading здесь —
/// вопрос числа запросов, а не корректности; измерять его нужно счётчиком из #4914.
///
/// Важно, что вводная в тесте НЕ имеет таргетов: ссылку на персонажа-таргета рисует
/// <c>PlotTargetDisplay</c>, и тест с таргетами проходил бы, даже если директива не развернулась.
///
/// Существующий <c>RendererIntegratedTest</c> проверяет сам рендерер на моках; здесь проверяется, что
/// на реальной странице ему достаются нужные данные.
/// </remarks>
public class PlotMarkdownRenderingScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task PlotEditPage_RendersCharacterAndGroupDirectives()
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
                scope.ServiceProvider, masterId, "Проект с директивами в сюжете");
        }

        // Персонаж и группа, на которые будут ссылаться директивы, приезжают из общего сида.
        var seed = await factory.Services.RunAsAsync(
            masterId,
            sp => TestPlotHelpers.SeedPlotFolderAsync(sp, projectId, elementCount: 1));

        var characterId = seed.TargetCharacterId;
        var groupId = seed.TargetGroupId;

        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var plotService = sp.GetRequiredService<IPlotService>();
            var versionId = await plotService.CreatePlotElement(
                seed.PlotFolderId,
                content: $"Встреча: %персонаж{characterId.CharacterId} и %группа{groupId.CharacterGroupId}",
                todoField: "",
                targetGroups: [],
                targetChars: [],
                elementType: PlotElementType.RegularPlot,
                isMasterOnly: false);
            await plotService.PublishElementVersion(versionId, sendNotification: false, commentText: null);
            return versionId;
        });

        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(), email, password);

        var url = $"{projectId.Value}/plots/edit?plotFolderId={seed.PlotFolderId.PlotFolderId}";
        var response = await client.GetAsync(url);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var document = await response.AsHtmlDocument();
        var body = document.DocumentNode.SelectSingleNode("//body")
            ?? throw new InvalidOperationException("Страница не содержит body");

        body.SelectSingleNode($"//a[contains(@href, '/character/{characterId.CharacterId}')]")
            .ShouldNotBeNull("Директива %персонаж не развернулась в ссылку на персонажа");
        body.SelectSingleNode($"//a[contains(@href, '/roles/{groupId.CharacterGroupId}/details')]")
            .ShouldNotBeNull("Директива %группа не развернулась в ссылку на группу");

        // Нераскрытая директива осталась бы в тексте как есть.
        var text = WebUtility.HtmlDecode(body.InnerText);
        text.ShouldNotContain($"%персонаж{characterId.CharacterId}");
        text.ShouldNotContain($"%группа{groupId.CharacterGroupId}");
    }
}
