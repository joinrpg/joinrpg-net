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
using JoinRpg.Services.Interfaces.Projects;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// После регистрации игрока страница чек-ина показывает, кого зарегистрировали, и кнопку печати его бейджа.
/// </summary>
/// <remarks>
/// Свой проект, а не общий сид смоука: тест регистрирует заявку, а смоук обходит страницу регистрации
/// заявки сида и зависел бы от порядка тестов.
/// </remarks>
public class CheckInBadgeBannerScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    private const string Password = "Password123!";
    private const string CharacterName = "Геральт из Ривии";

    [Fact]
    public async Task CheckIn_ThenBannerWithBadgeButton()
    {
        var (masterId, masterEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(factory.Services, password: Password);
        var projectId = await TestUserProjectHelpers.CreateProjectAsync(factory.Services, masterId, "Проект с регистрацией и бейджами");
        var claimId = await SeedApprovedClaimAsync(masterId, projectId);

        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }),
            masterEmail,
            Password,
            followsRedirects: false);

        // Одобренная, но ещё не зарегистрированная заявка — баннера «зарегистрирован» быть не должно.
        var beforeCheckIn = await (await client.GetAsync($"{projectId.Value}/checkin/Index?checkedInClaimId={claimId.ClaimId}")).AsHtmlDocument();
        beforeCheckIn.DocumentNode.SelectSingleNode("//div[contains(@class,'alert-success')]").ShouldBeNull();

        var token = await client.GetAntiforgeryTokenAsync($"{projectId.Value}/claim/{claimId.ClaimId}/checkin");
        var checkIn = await client.PostFormAsync(
            $"{projectId.Value}/checkin/DoCheckIn",
            token,
            ("projectId", projectId.Value.ToString()),
            ("claimId", claimId.ClaimId.ToString()),
            ("money", "0"));
        checkIn.StatusCode.ShouldBe(HttpStatusCode.Found, await checkIn.DescribeValidationErrorsAsync());

        var page = await (await client.GetAsync(checkIn.Headers.Location)).AsHtmlDocument();
        var banner = page.DocumentNode.SelectSingleNode("//div[contains(@class,'alert-success')]").ShouldNotBeNull();
        WebUtility.HtmlDecode(banner.InnerText).ShouldContain(CharacterName);

        // Шаблон в проекте единственный — по кнопке сразу печать, без выбора шаблона.
        var href = WebUtility.HtmlDecode(banner.SelectSingleNode(".//a[contains(@href,'/print/')]").ShouldNotBeNull().GetAttributeValue("href", ""));
        var printed = await (await client.GetAsync(href)).AsHtmlDocument();
        WebUtility.HtmlDecode(printed.DocumentNode.InnerText).ShouldContain(CharacterName);
    }

    private async Task<ClaimIdentification> SeedApprovedClaimAsync(UserIdentification masterId, ProjectIdentification projectId)
    {
        var characterId = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectService = sp.GetRequiredService<IProjectService>();
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            await projectService.SetClaimSettings(projectId, projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });
            await projectService.SetCheckInSettings(projectId, checkInProgress: true, enableCheckInModule: true, modelAllowSecondRoles: false);

            var nameField = projectInfo.CharacterNameField ?? throw new InvalidOperationException("В проекте нет поля имени");

            // Имя персонажа — единственная строка бейджа: по нему видно, что напечатан именно он.
            _ = await sp.GetRequiredService<IProjectBadgeTemplateService>().CreateAsync(
                projectId,
                "Игрок",
                new BadgeLayout(90, 55, null, [new BadgeLine(nameField.Id.ProjectFieldId, null, null, 20, 10, 28, Bold: true)]));

            return await sp.GetRequiredService<ICharacterService>().AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [projectInfo.GroupTree.RootGroupId],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: new FieldLayerContainer(projectInfo, new Dictionary<int, string?> { [nameField.Id.ProjectFieldId] = CharacterName })));
        });

        var (playerId, _) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(factory.Services);
        var claimId = await factory.Services.RunAsAsync(playerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<IClaimService>().AddClaimFromUser(
                characterId, "Заявка от игрока", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: true);
        });

        await factory.Services.RunAsAsync(masterId, sp => sp.GetRequiredService<IClaimService>().ApproveByMaster(claimId, "Принято"));
        return claimId;
    }
}
