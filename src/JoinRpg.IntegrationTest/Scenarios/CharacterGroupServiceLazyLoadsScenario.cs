using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Удаление группы и перестановка персонажа в группе (<see cref="ICharacterGroupService"/>) не
/// должны лениво догружать связи (#5097, #4670).
/// </summary>
/// <remarks>
/// Обе операции дёргаются только POST-ручками, которые ни один тест не вызывал, поэтому долг не
/// был виден ни в одном замере. Счётчик здесь заводится вручную (см. docs/lazy-loads-baseline.md).
/// </remarks>
public class CharacterGroupServiceLazyLoadsScenario(JoinApplicationFactory factory)
    : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task DeleteGroupWithCharacters_LazyLoads()
    {
        var (masterId, groupId, _) = await SeedGroupAsync(characterCount: 2);

        var count = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var service = sp.GetRequiredService<ICharacterGroupService>();
            using var lazyLoads = LazyLoadCounter.BeginScope();
            await service.DeleteCharacterGroup(groupId);
            return lazyLoads.Count;
        });

        // Долг #5097: CharacterGroup.Characters — фильтр поверх Project.Characters, который
        // write-хэндл проекта не грузит.
        count.ShouldBe(1, $"DeleteCharacterGroup дал {count} ленивых загрузок, см. #5097");
    }

    [Fact]
    public async Task DeleteEmptyGroup_LazyLoads()
    {
        var (masterId, groupId, _) = await SeedGroupAsync(characterCount: 0);

        var count = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var service = sp.GetRequiredService<ICharacterGroupService>();
            using var lazyLoads = LazyLoadCounter.BeginScope();
            await service.DeleteCharacterGroup(groupId);
            return lazyLoads.Count;
        });

        // Долг #5097: кроме Project.Characters пустую группу можно удалить насовсем, и проверка
        // этого трогает DirectlyRelatedPlotElements (PlotElementCharacterGroups).
        count.ShouldBe(2, $"DeleteCharacterGroup дал {count} ленивых загрузок, см. #5097");
    }

    [Fact]
    public async Task MoveCharacterAfter_LazyLoads()
    {
        var (masterId, groupId, characters) = await SeedGroupAsync(characterCount: 3);

        var (count, order) = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var service = sp.GetRequiredService<ICharacterGroupService>();
            using var lazyLoads = LazyLoadCounter.BeginScope();
            var order = await service.MoveCharacterAfter(groupId, characters[0], characters[2]);
            return (lazyLoads.Count, order);
        });

        order.ShouldBe([characters[1], characters[2], characters[0]]);

        // Долг #5097: порядок персонажей собирается из Project.Characters, который write-хэндл
        // проекта не грузит.
        count.ShouldBe(1, $"MoveCharacterAfter дал {count} ленивых загрузок, см. #5097");
    }

    private async Task<(UserIdentification MasterId, CharacterGroupIdentification GroupId, IReadOnlyList<CharacterIdentification> Characters)>
        SeedGroupAsync(int characterCount)
    {
        UserIdentification masterId;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId);
        }

        return await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            var groupId = await sp.GetRequiredService<ICharacterGroupService>().AddCharacterGroup(
                projectId,
                "Группа под удаление",
                isPublic: true,
                parentCharacterGroupIds: [projectInfo.GroupTree.RootGroupId],
                description: "");

            var characterService = sp.GetRequiredService<ICharacterService>();
            var characters = new List<CharacterIdentification>(characterCount);
            for (var i = 0; i < characterCount; i++)
            {
                characters.Add(await characterService.AddCharacter(new AddCharacterRequest(
                    projectId,
                    ParentCharacterGroupIds: [groupId],
                    new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                    FieldValues: FieldLayerContainer.Empty(projectInfo))));
            }

            return (masterId, groupId, (IReadOnlyList<CharacterIdentification>)characters);
        });
    }
}
