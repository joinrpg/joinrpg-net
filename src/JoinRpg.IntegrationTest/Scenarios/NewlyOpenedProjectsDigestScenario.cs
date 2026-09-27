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
    public async Task GetPublicProjectsOpenedForClaimsInLastWeek_SelectsOnlyFreshOrClaimlessPublicOpenProjects()
    {
        // 1. Мастер и четыре публичных открытых проекта: без заявок, со свежей заявкой,
        // со старой заявкой, и (для контроля) непубличный/закрытый.
        UserIdentification masterId;
        ProjectIdentification withoutClaims;
        ProjectIdentification withFreshClaim;
        ProjectIdentification withOldClaim;
        ProjectIdentification notPublic;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            withoutClaims = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId, "Без заявок");
            withFreshClaim = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId, "Со свежей заявкой");
            withOldClaim = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId, "Со старой заявкой");
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
        var oldCharacter = await OpenClaimsAndAddCharacter(withOldClaim);
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
        await AddClaim(oldCharacter);

        // 2. Заявку в "старом" проекте отодвигаем на 10 дней назад — за пределы недели.
        using (var scope = factory.Services.CreateScope())
        {
            var myDb = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            var claim = await myDb.Set<Claim>().SingleAsync(c => c.ProjectId == withOldClaim.Value);
            claim.CreateDate = DateTime.UtcNow.AddDays(-10);
            await myDb.SaveChangesAsync();
        }

        // 3. Выборка: проект без заявок и проект со свежей заявкой — кандидаты;
        // проект со старой заявкой и непубличный проект — нет.
        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectRepository = sp.GetRequiredService<IProjectRepository>();
            var candidates = await projectRepository.GetPublicProjectsOpenedForClaimsInLastWeek();
            var candidateIds = candidates.Select(c => c.ProjectId).ToHashSet();

            candidateIds.ShouldContain(withoutClaims);
            candidateIds.ShouldContain(withFreshClaim);
            candidateIds.ShouldNotContain(withOldClaim);
            candidateIds.ShouldNotContain(notPublic);
        });
    }
}
