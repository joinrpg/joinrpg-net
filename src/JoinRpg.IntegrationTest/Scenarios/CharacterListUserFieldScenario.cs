using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// В сетке ролей поле-ссылка на пользователя (ADR017) показывается ссылкой на пользователя, а не сырым id.
/// </summary>
/// <remarks>
/// Значение такого поля хранится как id через запятую, и <c>FieldWithValue.DisplayString</c> отдаёт
/// его как есть. Сетка раньше выводила именно его — в ячейке была только цифра.
/// </remarks>
public class CharacterListUserFieldScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    private const string Password = "Password123!";

    [Fact]
    public async Task UserLinkField_ShowsUserLink_NotRawId()
    {
        UserIdentification masterId;
        string email;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            (masterId, email) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(
                scope.ServiceProvider, password: Password);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект с полем-ссылкой на пользователя");
        }

        var fieldId = await factory.Services.RunAsAsync(
            masterId,
            sp => sp.GetRequiredService<IFieldSetupService>().AddField(new CreateFieldRequest(
                projectId,
                ProjectFieldType.UserLink,
                "Куратор",
                fieldHint: "",
                canPlayerEdit: false,
                canPlayerView: true,
                isPublic: true,
                FieldBoundTo.Character,
                MandatoryStatus.Optional,
                showForGroups: [],
                validForNpc: true,
                includeInPrint: false,
                showForUnapprovedClaims: true,
                price: 0,
                masterFieldHint: "",
                programmaticValue: null)));

        // Персонаж создаётся отдельной областью видимости: метаданные проекта кешируются на
        // область, и в той, где поле только что создано, ProjectInfo о нём ещё не знает.
        await factory.Services.RunAsAsync(
            masterId,
            async sp =>
            {
                var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>()
                    .GetProjectMetadata(projectId);
                var nameFieldId = (projectInfo.CharacterNameField
                        ?? throw new InvalidOperationException("В проекте нет поля имени персонажа"))
                    .Id.ProjectFieldId;

                _ = await sp.GetRequiredService<ICharacterService>().AddCharacter(
                    new AddCharacterRequest(
                        projectId,
                        ParentCharacterGroupIds: [projectInfo.GroupTree.RootGroupId],
                        new CharacterTypeInfo(
                            CharacterType.NonPlayer,
                            IsHot: false,
                            SlotLimit: null,
                            SlotName: null,
                            CharacterVisibility.Public),
                        FieldValues: new FieldLayerContainer(
                            projectInfo,
                            new Dictionary<int, string?>
                            {
                                [nameFieldId] = "Персонаж с куратором",
                                [fieldId.ProjectFieldId] = masterId.Value.ToString(),
                            })));
            });

        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(), email, Password);

        var response = await client.GetAsync($"{projectId.Value}/characters/active");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var document = await response.AsHtmlDocument();
        var userLinks = document.DocumentNode.SelectNodes("//table//a[contains(@class, 'join-user')]");

        userLinks.ShouldNotBeNull("в сетке нет ни одной ссылки на пользователя");
        var linkText = WebUtility.HtmlDecode(userLinks.Single().InnerText).Trim();
        linkText.ShouldNotBeEmpty();
        linkText.ShouldNotBe(masterId.Value.ToString());
    }
}
