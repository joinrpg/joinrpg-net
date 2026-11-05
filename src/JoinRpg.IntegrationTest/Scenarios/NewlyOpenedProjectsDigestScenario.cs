using System.Data.Entity;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

public class NewlyOpenedProjectsDigestScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task GetPublicProjectsOpenedForClaimsInLastWeek_SelectsOnlyRecentlyOpenedPublicProjects()
    {
        // 1. Мастер и четыре публичных проекта, открытых для заявок: недавно открытый (без заявок),
        // недавно открытый со свежей заявкой, давно открытый (дата открытия отодвинута назад),
        // открытый, но с незаполненной датой (заявки ни разу не открывались), и (для контроля) непубличный.
        UserIdentification masterId;
        ProjectIdentification withoutClaims;
        ProjectIdentification withFreshClaim;
        ProjectIdentification openedLongAgo;
        ProjectIdentification dateNeverSet;
        ProjectIdentification notPublic;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            withoutClaims = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId, "Без заявок");
            withFreshClaim = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId, "Со свежей заявкой");
            openedLongAgo = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId, "Давно открытый");
            dateNeverSet = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId, "Дата не задана");
            notPublic = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId, "Непубличный");
        }

        async Task<CharacterIdentification> OpenClaimsAndAddCharacter(ProjectIdentification projectId, bool isPublicProject = true) =>
            await factory.Services.RunAsAsync(masterId, async sp =>
            {
                var projectService = sp.GetRequiredService<IProjectService>();
                var metadataRepository = sp.GetRequiredService<IProjectMetadataRepository>();
                var projectInfo = await metadataRepository.GetProjectMetadata(projectId);
                await projectService.SetClaimSettings(
                    projectId,
                    projectInfo.ClaimSettings with { IsAcceptingClaims = true, IsPublicProject = isPublicProject });

                var characterService = sp.GetRequiredService<ICharacterService>();
                return await characterService.AddCharacter(new AddCharacterRequest(
                    projectId,
                    ParentCharacterGroupIds: [],
                    new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                    FieldValues: FieldLayerContainer.Empty(projectInfo)));
            });

        await OpenClaimsAndAddCharacter(withoutClaims);
        var freshCharacter = await OpenClaimsAndAddCharacter(withFreshClaim);
        await OpenClaimsAndAddCharacter(openedLongAgo);
        await OpenClaimsAndAddCharacter(dateNeverSet);
        await OpenClaimsAndAddCharacter(notPublic, isPublicProject: false);

        async Task AddClaim(CharacterIdentification characterId) =>
            await factory.Services.RunAsAsync(masterId, async sp =>
            {
                var claimService = sp.GetRequiredService<IClaimService>();
                var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(characterId.ProjectId);
                await claimService.AddClaimFromUser(
                    characterId, "Хочу эту роль", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: false);
            });

        await AddClaim(freshCharacter);

        // 2. Дату открытия заявок "давнего" проекта отодвигаем на 10 дней назад — за пределы недели,
        // а у "дата не задана" её стираем вовсе — так выглядят данные, созданные до появления поля
        // (#6109): проект открыт для заявок, но дата первого открытия не зафиксирована.
        // Через сервисный слой такое состояние не воспроизвести — SetClaimSettings всегда заполняет дату.
        using (var scope = factory.Services.CreateScope())
        {
            var myDb = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            var longAgoProject = await myDb.Set<Project>().SingleAsync(p => p.ProjectId == openedLongAgo.Value);
            longAgoProject.ClaimsOpenedDate = DateTime.UtcNow.AddDays(-10);
            var neverOpenedProject = await myDb.Set<Project>().SingleAsync(p => p.ProjectId == dateNeverSet.Value);
            neverOpenedProject.ClaimsOpenedDate = null;
            await myDb.SaveChangesAsync();
        }

        // 3. Выборка по дате первого открытия заявок: недавно открытые проекты (с заявками и без) —
        // кандидаты; давно открытый, с незаполненной датой и непубличный проект — нет.
        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectRepository = sp.GetRequiredService<IProjectRepository>();
            var candidates = await projectRepository.GetPublicProjectsOpenedForClaimsInLastWeek();
            var candidateIds = candidates.Select(c => c.ProjectId).ToHashSet();

            candidateIds.ShouldContain(withoutClaims);
            candidateIds.ShouldContain(withFreshClaim);
            candidateIds.ShouldNotContain(openedLongAgo);
            candidateIds.ShouldNotContain(dateNeverSet);
            candidateIds.ShouldNotContain(notPublic);
        });
    }
}
