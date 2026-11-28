using System.Data.Entity;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Projects;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

public class AdvertisementRegionFilterScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    // Региональный фильтр расписания рекламы: при заданном фильтре кандидатами являются только
    // проекты с будущей активной карточкой КогдаИгры со статусом Ок в одном из регионов фильтра.
    [Fact]
    public async Task GetPublicProjectsOpenForHotRoleAdvertisement_FiltersCandidatesByRegions()
    {
        UserIdentification masterId;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект для проверки фильтра по регионам");
        }

        // Полноценный кандидат (по образцу AdvertisementProjectRankingScenario) с горячей ролью
        // и будущей активной карточкой КогдаИгры в Москве со статусом Ок.
        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectService = sp.GetRequiredService<IProjectService>();
            var metadataRepository = sp.GetRequiredService<IProjectMetadataRepository>();
            var projectInfo = await metadataRepository.GetProjectMetadata(projectId);
            await projectService.SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { IsAcceptingClaims = true });

            var characterService = sp.GetRequiredService<ICharacterService>();
            await characterService.AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [],
                new CharacterTypeInfo(CharacterType.Player, IsHot: true, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: FieldLayerContainer.Empty(projectInfo)));
        });

        using (var scope = factory.Services.CreateScope())
        {
            var myDb = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            var project = await myDb.Set<Project>()
                .Include(p => p.KogdaIgraGames)
                .Include(p => p.Details)
                .SingleAsync(p => p.ProjectId == projectId.Value);

            project.Details.DisableKogdaIgraMapping = false;
            project.KogdaIgraGames.Add(new KogdaIgraGame
            {
                KogdaIgraGameId = projectId.Value + 3_000_000,
                Name = "Тестовая будущая игра КогдаИгра",
                JsonGameData = "{}",
                Active = true,
                Begin = DateTime.UtcNow.AddDays(30),
                End = DateTime.UtcNow.AddDays(33),
                Status = (int)KogdaIgraGameStatus.Ok,
                RegionId = Region.Moskva,
                UpdateRequestedAt = DateTimeOffset.UtcNow,
                LastUpdatedAt = DateTimeOffset.UtcNow,
            });
            await myDb.SaveChangesAsync();
        }

        using (var scope = factory.Services.CreateScope())
        {
            var projectRepository = scope.ServiceProvider.GetRequiredService<IProjectRepository>();

            var withoutFilter = await projectRepository.GetPublicProjectsOpenForHotRoleAdvertisement();
            withoutFilter.Select(c => c.ProjectId).ShouldContain(projectId);

            var inRegion = await projectRepository.GetPublicProjectsOpenForHotRoleAdvertisement(new HashSet<Region> { Region.Moskva });
            inRegion.Select(c => c.ProjectId).ShouldContain(projectId);

            var otherRegion = await projectRepository.GetPublicProjectsOpenForHotRoleAdvertisement(new HashSet<Region> { Region.SanktPeterburg });
            otherRegion.Select(c => c.ProjectId).ShouldNotContain(projectId);
        }
    }
}
