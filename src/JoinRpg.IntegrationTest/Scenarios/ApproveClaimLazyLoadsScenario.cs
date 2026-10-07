using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
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
/// Принятие заявки мастером (<see cref="IClaimService.ApproveByMaster"/>) не должно лениво
/// догружать связи (#5061, #4670).
/// </summary>
/// <remarks>
/// Остальные сценарии принимают заявки через сервис без счётчика, а HTTP-ручку принятия никто не
/// дёргает, поэтому долг не был виден ни в одном замере. Счётчик здесь заводится вручную (см.
/// docs/lazy-loads-baseline.md). Заявка на слот выделена отдельно: при её принятии из слота
/// создаётся персонаж, и это отдельная ветка кода.
/// </remarks>
public class ApproveClaimLazyLoadsScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task ApproveClaimToCharacter_LazyLoads()
    {
        var (count, _, _) = await ApproveClaimAndCountLazyLoads(
            new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public));

        count.ShouldBe(0, $"ApproveByMaster дал {count} ленивых загрузок, см. #5061");
    }

    [Fact]
    public async Task ApproveClaimToSlot_LazyLoads()
    {
        var (count, claimId, plot) = await ApproveClaimAndCountLazyLoads(
            new CharacterTypeInfo(CharacterType.Slot, IsHot: false, SlotLimit: 3, SlotName: "Стражник", CharacterVisibility.Public));

        // Персонаж, созданный из слота, наследует прямые привязки слота к сюжетам.
        using (var scope = factory.Services.CreateScope())
        {
            var claim = await scope.ServiceProvider.GetRequiredService<IClaimsRepository>().GetClaim(claimId)
                ?? throw new InvalidOperationException("Заявка не найдена");
            claim.Character.CharacterType.ShouldBe(CharacterType.Player);
            claim.Character.DirectlyRelatedPlotElements
                .Select(e => e.PlotElementId)
                .ShouldBe(plot.ElementIds.Select(e => e.PlotElementId), ignoreOrder: true);
        }

        // Долг #5061: персонаж из слота забирает Subscriptions слота, и навигация грузится лениво
        // (UserSubscriptions). Что должно происходить с подписками слота — открытый вопрос.
        count.ShouldBe(1, $"ApproveByMaster на слот дал {count} ленивых загрузок, см. #5061");
    }

    private async Task<(int Count, ClaimIdentification ClaimId, PlotSeedResult Plot)> ApproveClaimAndCountLazyLoads(
        CharacterTypeInfo characterType)
    {
        UserIdentification masterId;
        ProjectIdentification projectId;
        UserIdentification playerId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId);
            playerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        }

        var (characterId, plot) = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            await sp.GetRequiredService<IProjectService>().SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });

            var characterId = await sp.GetRequiredService<ICharacterService>().AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [],
                characterType,
                FieldValues: FieldLayerContainer.Empty(projectInfo)));

            // Персонаж в сюжете: без прямых привязок копировать слоту было бы нечего.
            var plot = await TestPlotHelpers.SeedPlotFolderAsync(sp, projectId, elementCount: 2, extraTargetChars: [characterId]);
            return (characterId, plot);
        });

        var claimId = await factory.Services.RunAsAsync(playerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<IClaimService>().AddClaimFromUser(
                characterId, "Хочу эту роль", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: false);
        });

        var count = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var claimService = sp.GetRequiredService<IClaimService>();
            using var lazyLoads = LazyLoadCounter.BeginScope();
            await claimService.ApproveByMaster(claimId, "Принято");
            return lazyLoads.Count;
        });
        return (count, claimId, plot);
    }
}
