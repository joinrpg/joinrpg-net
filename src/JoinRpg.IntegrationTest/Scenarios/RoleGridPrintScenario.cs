using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.ProjectMetadata;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>Печать сохранённой и классической сетки — статический рендер того же компонента (ADR029).</summary>
public class RoleGridPrintScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task SavedAndClassicGridPrint_RenderCharacters_WithoutInteractiveControls()
    {
        UserIdentification masterId;
        string masterEmail;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            (masterId, masterEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId, "Проект с печатью сетки");
        }

        var (rolesListId, firstName, secondName) = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            var rootGroupId = projectInfo.GroupTree.RootGroupId;
            var nameFieldId = projectInfo.CharacterNameField!.Id.ProjectFieldId;

            string[] names = [$"Первый печатный {Guid.NewGuid():N}", $"Второй печатный {Guid.NewGuid():N}"];
            foreach (var name in names)
            {
                _ = await sp.GetRequiredService<ICharacterService>().AddCharacter(new AddCharacterRequest(
                    projectId,
                    ParentCharacterGroupIds: [rootGroupId],
                    new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                    FieldValues: new FieldLayerContainer(projectInfo, new Dictionary<int, string?> { [nameFieldId] = name })));
            }

            var rolesList = await sp.GetRequiredService<IProjectRolesListService>().CreateAsync(new ProjectRolesList(
                new ProjectRolesListIdentification(projectId, -1),
                "Печатная сетка",
                CharacterGroupId: null,
                PublicMode: true,
                Fields: [],
                ContactsColumn: PlayerColumnMode.NameOnly,
                GroupsColumn: ProjectRolesListVisibilityMode.None,
                GroupsViewMode: RolesGridGroupsViewMode.Sections,
                ShowRolesFilter: ShowRolesFilter.All));

            return (rolesList.ProjectRolesListId!, names[0], names[1]);
        });

        // Мастер имеет кнопки редактирования на обычной сетке — на печати их быть не должно.
        var masterClient = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(factory.CreateClient(), masterEmail);

        var savedPrint = WebUtility.HtmlDecode(await masterClient.GetStringAsync(
            $"{projectId.Value}/roleslist/{rolesListId.ProjectRolesListId}/print"));
        savedPrint.ShouldContain("Печатная сетка");
        savedPrint.ShouldContain(firstName);
        savedPrint.ShouldContain(secondName);
        savedPrint.ShouldNotContain("Настройки сетки");
        savedPrint.ShouldNotContain("Редактировать персонажа");
        savedPrint.ShouldNotContain("Заявиться");

        var classicPrint = WebUtility.HtmlDecode(await masterClient.GetStringAsync($"{projectId.Value}/roles/print"));
        classicPrint.ShouldContain(firstName);
        classicPrint.ShouldContain(secondName);
        classicPrint.ShouldNotContain("Настройки сетки");
        classicPrint.ShouldNotContain("Редактировать персонажа");
        classicPrint.ShouldNotContain("Заявиться");
    }
}
