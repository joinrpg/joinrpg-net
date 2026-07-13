using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.IntegrationTests.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.ProjectMetadata;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

public class CloneProjectScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task CloneProject_WithRolesLists_CopiesThemToNewProject()
    {
        UserIdentification masterId;
        ProjectIdentification originalProjectId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            originalProjectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Исходный проект для клонирования");
        }

        string rolesListAName = "Публичная сетка";
        string rolesListBName = "Сетка по группе";

        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var metadataRepository = sp.GetRequiredService<IProjectMetadataRepository>();
            var fieldSetupService = sp.GetRequiredService<IFieldSetupService>();
            var characterGroupService = sp.GetRequiredService<ICharacterGroupService>();
            var characterService = sp.GetRequiredService<ICharacterService>();
            var rolesListService = sp.GetRequiredService<IProjectRolesListService>();

            var projectInfo = await metadataRepository.GetProjectMetadata(originalProjectId);
            var rootGroupId = projectInfo.RootCharacterGroupId;
            var nameFieldId = (projectInfo.CharacterNameField
                ?? throw new InvalidOperationException("В проекте нет поля имени"))
                .Id.ProjectFieldId;

            // Строковое поле для колонки сетки
            var extraFieldId = await fieldSetupService.AddField(new CreateFieldRequest(
                originalProjectId,
                ProjectFieldType.String,
                "Цитата",
                fieldHint: "",
                canPlayerEdit: false,
                canPlayerView: true,
                isPublic: true,
                fieldBoundTo: FieldBoundTo.Character,
                MandatoryStatus.Optional,
                showForGroups: [],
                validForNpc: false,
                includeInPrint: false,
                showForUnapprovedClaims: false,
                price: 0,
                masterFieldHint: "",
                programmaticValue: null));

            // Группа персонажей
            var groupId = await characterGroupService.AddCharacterGroup(
                originalProjectId,
                "Северяне",
                isPublic: true,
                parentCharacterGroupIds: [rootGroupId],
                description: "");

            // Шаблонный персонаж
            await characterService.AddCharacter(new AddCharacterRequest(
                originalProjectId,
                ParentCharacterGroupIds: [rootGroupId],
                new CharacterTypeInfo(CharacterType.Slot, IsHot: false, SlotLimit: 5, SlotName: "Северянин", CharacterVisibility.Public),
                FieldValues: new Dictionary<int, string?> { [nameFieldId] = "Шаблон Северянина" }));

            // Сетка A: от корня, со строковым полем
            await rolesListService.CreateAsync(new ProjectRolesList(
                new ProjectRolesListIdentification(originalProjectId, -1),
                rolesListAName,
                CharacterGroupId: null,
                PublicMode: true,
                Fields: [extraFieldId],
                ContactsColumn: ProjectRolesListVisibilityMode.None,
                GroupsColumn: ProjectRolesListVisibilityMode.PublicOnly,
                ShowCharacterGroups: true,
                ShowRolesFilter: ShowRolesFilter.All));

            // Сетка B: от конкретной группы, без полей
            await rolesListService.CreateAsync(new ProjectRolesList(
                new ProjectRolesListIdentification(originalProjectId, -1),
                rolesListBName,
                CharacterGroupId: groupId,
                PublicMode: false,
                Fields: [],
                ContactsColumn: ProjectRolesListVisibilityMode.None,
                GroupsColumn: ProjectRolesListVisibilityMode.None,
                ShowCharacterGroups: false,
                ShowRolesFilter: ShowRolesFilter.VacantOnly));
        });

        // Клонирование проекта на уровне SettingsFieldsGroupsAndTemplates
        var cloneProjectId = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var createProjectService = sp.GetRequiredService<ICreateProjectService>();
            var result = await createProjectService.CreateProject(
                new CloneProjectRequest(
                    new ProjectName("Клон проекта"),
                    originalProjectId,
                    ProjectCopySettingsDto.SettingsFieldsGroupsAndTemplates));

            return result switch
            {
                SuccessCreateProjectResult r => r.ProjectId,
                PartiallySuccessCreateProjectResult r => r.ProjectId,
                _ => throw new InvalidOperationException($"Не удалось склонировать проект: {result}")
            };
        });

        // Проверяем, что сетки ролей скопировались
        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var metadataRepository = sp.GetRequiredService<IProjectMetadataRepository>();
            var cloneInfo = await metadataRepository.GetProjectMetadata(cloneProjectId);

            cloneInfo.ProjectRolesLists.Count.ShouldBe(2);

            foreach (var rolesList in cloneInfo.ProjectRolesLists)
            {
                // Все сетки принадлежат новому проекту
                rolesList.ProjectRolesListId.ProjectId.ShouldBe(cloneProjectId);

                // Поля ссылаются на новый проект
                foreach (var field in rolesList.Fields)
                {
                    field.ProjectId.ShouldBe(cloneProjectId);
                }

                // CharacterGroupId, если задан, ссылается на новый проект
                if (rolesList.CharacterGroupId is { } cgId)
                {
                    cgId.ProjectId.ShouldBe(cloneProjectId);
                }
            }

            var names = cloneInfo.ProjectRolesLists.Select(r => r.Name).ToHashSet();
            names.ShouldContain(rolesListAName);
            names.ShouldContain(rolesListBName);

            // Сетка A: без привязки к группе, с полем, публичная
            var cloneA = cloneInfo.ProjectRolesLists.Single(r => r.Name == rolesListAName);
            cloneA.CharacterGroupId.ShouldBeNull();
            cloneA.Fields.Count.ShouldBe(1);
            cloneA.PublicMode.ShouldBeTrue();
            cloneA.GroupsColumn.ShouldBe(ProjectRolesListVisibilityMode.PublicOnly);
            cloneA.ShowRolesFilter.ShouldBe(ShowRolesFilter.All);

            // Сетка B: с привязкой к группе, без полей, приватная
            var cloneB = cloneInfo.ProjectRolesLists.Single(r => r.Name == rolesListBName);
            cloneB.CharacterGroupId.ShouldNotBeNull();
            cloneB.Fields.Count.ShouldBe(0);
            cloneB.PublicMode.ShouldBeFalse();
            cloneB.ShowCharacterGroups.ShouldBeFalse();
            cloneB.ShowRolesFilter.ShouldBe(ShowRolesFilter.VacantOnly);
        });
    }
}
