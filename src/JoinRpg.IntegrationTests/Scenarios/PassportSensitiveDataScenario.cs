using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.Domain.Problems;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.IntegrationTests.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Регрессия на #4763: даже если проект требует паспорт/адрес регистрации, игрок,
/// не разрешивший доступ к чувствительным данным, всё равно должен иметь возможность
/// подать заявку — согласие спрашивается на уровне заявки, а не блокирует её подачу.
/// Уровень проблемы (Warning на SensitiveDataNotAllowed) при этом не меняется.
/// </summary>
public class PassportSensitiveDataScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task PlayerCanSendClaim_WhenSensitiveDataNotAllowed_EvenIfPassportRequired()
    {
        // 1. Мастер, проект с обязательным паспортом, игрок
        UserIdentification masterId;
        ProjectIdentification projectId;
        UserIdentification playerId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId);
            playerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        }

        var characterId = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectService = sp.GetRequiredService<IProjectService>();
            var metadataRepository = sp.GetRequiredService<IProjectMetadataRepository>();
            var projectInfo = await metadataRepository.GetProjectMetadata(projectId);

            await projectService.SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });

            await projectService.SetContactSettings(
                projectId,
                ProjectProfileRequirementSettings.AllNotRequired with { RequirePassport = MandatoryStatus.Required });

            var characterService = sp.GetRequiredService<ICharacterService>();
            return await characterService.AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: FieldLayerContainer.Empty(projectInfo)));
        });

        // 2. Игрок подаёт заявку, явно не разрешая доступ к чувствительным данным
        // (у игрока и так не заполнены ни паспорт, ни адрес регистрации).
        var claimId = await factory.Services.RunAsAsync(playerId, async sp =>
        {
            var claimService = sp.GetRequiredService<IClaimService>();
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await claimService.AddClaimFromUser(
                characterId, "Хочу эту роль", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: false);
        });

        // 3. Заявка должна быть создана (сам факт того, что до сюда дошли без исключения,
        // уже подтверждает, что подача не заблокирована отсутствием паспорта/согласия).
        using (var scope = factory.Services.CreateScope())
        {
            var claimsRepository = scope.ServiceProvider.GetRequiredService<IClaimsRepository>();
            var claim = await claimsRepository.GetClaim(claimId)
                ?? throw new InvalidOperationException("Claim not found");
            claim.ClaimStatus.ShouldBe(ClaimStatus.AddedByUser);
            claim.PlayerAllowedSenstiveData.ShouldBeFalse();

            // 4. Проблема "нет доступа к чувствительным данным" должна быть ровно одна,
            // с той же severity (Warning), что была до перевода на UserProfileItemType —
            // а не MissingPassport/MissingRegistrationAddress по отдельности.
            var problemValidator = scope.ServiceProvider.GetRequiredService<IProblemValidator<Claim>>();
            var metadataRepository = scope.ServiceProvider.GetRequiredService<IProjectMetadataRepository>();
            var projectInfo = await metadataRepository.GetProjectMetadata(projectId);

            var problems = problemValidator.Validate(claim, projectInfo).ToList();

            problems.ShouldContain(p => p.ProblemType == ClaimProblemType.SensitiveDataNotAllowed && p.Severity == ProblemSeverity.Warning);
            problems.ShouldNotContain(p => p.ProblemType == ClaimProblemType.MissingPassport);
            problems.ShouldNotContain(p => p.ProblemType == ClaimProblemType.MissingRegistrationAddress);
        }
    }
}
