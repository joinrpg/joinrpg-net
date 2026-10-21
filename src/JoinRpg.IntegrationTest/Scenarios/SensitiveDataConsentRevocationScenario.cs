using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// #5706: когда проект начинает запрашивать новые защищённые данные, согласие у его заявок
/// отзывается в том же <c>SaveChanges</c>, что и новые настройки. Проверяется на настоящей базе: юнит-тесты
/// видят только фейк, а здесь важны настоящий запрос и то, что чужой проект он не задевает.
/// </summary>
public class SensitiveDataConsentRevocationScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task NewSensitiveDataRevokesConsentOnlyInThatProject()
    {
        UserIdentification masterId;
        UserIdentification playerId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            playerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        }

        var (changedProject, changedClaim) = await CreateProjectWithConsentedClaim(masterId, playerId);
        var (otherProject, otherClaim) = await CreateProjectWithConsentedClaim(masterId, playerId);

        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(changedProject);
            await sp.GetRequiredService<IProjectService>().SetContactSettings(
                changedProject,
                projectInfo.ProfileRequirementSettings with { RequireBirthDate = MandatoryStatus.Recommended });
        });

        using (var scope = factory.Services.CreateScope())
        {
            var claimsRepository = scope.ServiceProvider.GetRequiredService<IClaimsRepository>();
            (await claimsRepository.GetClaim(changedClaim)).ShouldNotBeNull().PlayerAllowedSenstiveData.ShouldBeFalse();
            (await claimsRepository.GetClaim(otherClaim)).ShouldNotBeNull().PlayerAllowedSenstiveData.ShouldBeTrue();

            var metadataRepository = scope.ServiceProvider.GetRequiredService<IProjectMetadataRepository>();
            (await metadataRepository.GetProjectMetadata(changedProject)).ProfileRequirementSettings.BirthDateRequested.ShouldBeTrue();
            (await metadataRepository.GetProjectMetadata(otherProject)).ProfileRequirementSettings.BirthDateRequested.ShouldBeFalse();
        }
    }

    /// <summary>Проект, запрашивающий паспорт, и заявка игрока, давшего согласие.</summary>
    private async Task<(ProjectIdentification ProjectId, ClaimIdentification ClaimId)> CreateProjectWithConsentedClaim(
        UserIdentification masterId, UserIdentification playerId)
    {
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId);
        }

        var characterId = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectService = sp.GetRequiredService<IProjectService>();
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);

            await projectService.SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });
            await projectService.SetContactSettings(
                projectId,
                ProjectProfileRequirementSettings.AllNotRequired with { RequirePassport = MandatoryStatus.Recommended });

            return await sp.GetRequiredService<ICharacterService>().AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: FieldLayerContainer.Empty(projectInfo)));
        });

        var claimId = await factory.Services.RunAsAsync(playerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<IClaimService>().AddClaimFromUser(
                characterId, "Хочу эту роль", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: true);
        });

        return (projectId, claimId);
    }
}
