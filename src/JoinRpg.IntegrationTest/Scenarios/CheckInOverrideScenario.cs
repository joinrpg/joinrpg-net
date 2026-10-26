using System.Data.Entity;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Problems;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.ProjectAccess;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Регистрация в обход: администратор заявок регистрирует игрока, несмотря на незаполненные данные и
/// несданный взнос, — но только с явным подтверждением, а право само по себе проверок не снимает.
/// </summary>
public class CheckInOverrideScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    private const int Fee = 1000;

    [Fact]
    public async Task ClaimsAdmin_WithoutConfirmation_CannotCheckIn()
    {
        var seed = await SeedAsync();

        _ = await Should.ThrowAsync<ClaimWrongStatusException>(() => CheckIn(seed.ClaimsAdminId, seed.ClaimId, checkInOverride: null));

        (await LoadClaimAsync(seed)).Claim.Status.ShouldBe(ClaimStatus.Approved);
    }

    [Fact]
    public async Task MasterWithoutManageClaims_CannotCheckInWithOverride()
    {
        var seed = await SeedAsync();
        var required = await RequiredOverrideAsync(seed);

        _ = await Should.ThrowAsync<NoAccessToProjectException>(() => CheckIn(seed.PlainMasterId, seed.ClaimId, required));

        (await LoadClaimAsync(seed)).Claim.Status.ShouldBe(ClaimStatus.Approved);
    }

    [Fact]
    public async Task ClaimsAdmin_WithOverride_ChecksInAndLeavesDebt()
    {
        var seed = await SeedAsync();
        var required = await RequiredOverrideAsync(seed);

        await CheckIn(seed.ClaimsAdminId, seed.ClaimId, required);

        var claim = await LoadClaimAsync(seed);
        claim.Claim.Status.ShouldBe(ClaimStatus.CheckedIn);
        claim.Claim.Finance.FeePaid.ShouldBe(0);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        var discussionId = await db.Set<Claim>().Where(c => c.ClaimId == seed.ClaimId.ClaimId).Select(c => c.CommentDiscussionId).SingleAsync();
        var history = await db.Set<Comment>()
            .Where(c => c.CommentDiscussionId == discussionId && c.ExtraAction == CommentExtraAction.CheckedIn)
            .Select(c => c.CommentText.Text.Contents)
            .SingleAsync();
        history.ShouldNotBeNull();
        history.ShouldContain("в обход");
        history.ShouldContain("данных профиля");
        history.ShouldContain($"долг {Fee}");
    }

    /// <summary>
    /// Подтверждение относится к тому, что мастер видел: появилось новое препятствие — регистрация не проходит.
    /// </summary>
    [Fact]
    public async Task ClaimsAdmin_StaleOverride_CannotCheckIn()
    {
        var seed = await SeedAsync();
        var stale = (await RequiredOverrideAsync(seed)) with { Problems = [] };

        _ = await Should.ThrowAsync<ClaimWrongStatusException>(() => CheckIn(seed.ClaimsAdminId, seed.ClaimId, stale));
    }

    private Task CheckIn(UserIdentification userId, ClaimIdentification claimId, CheckInOverride? checkInOverride)
        => factory.Services.RunAsAsync(userId, sp => sp.GetRequiredService<IClaimService>().CheckInClaim(claimId, checkInOverride));

    private async Task<CheckInOverride> RequiredOverrideAsync(Seed seed)
    {
        var claimInfo = await LoadClaimAsync(seed);
        using var scope = factory.Services.CreateScope();
        var validator = new ClaimCheckInValidator(claimInfo, scope.ServiceProvider.GetRequiredService<IClaimProblemValidator>());
        return validator.RequiredOverride.ShouldNotBeNull();
    }

    private Task<ClaimInfo> LoadClaimAsync(Seed seed)
        => factory.Services.RunAsAsync(seed.OwnerId, async sp =>
            (await sp.GetRequiredService<IClaimInfoRepository>().GetClaimInfoOrDefault(seed.ClaimId)).ShouldNotBeNull());

    private sealed record Seed(UserIdentification OwnerId, UserIdentification ClaimsAdminId, UserIdentification PlainMasterId, ClaimIdentification ClaimId);

    /// <summary>
    /// Одобренная заявка, которую не зарегистрировать: проект требует паспорт, а его нет, и взнос не сдан.
    /// </summary>
    private async Task<Seed> SeedAsync()
    {
        var (ownerId, _) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(factory.Services);
        var projectId = await TestUserProjectHelpers.CreateProjectAsync(factory.Services, ownerId, "Регистрация в обход");
        var (claimsAdminId, _) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(factory.Services);
        var (plainMasterId, _) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(factory.Services);

        var characterId = await factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var projectService = sp.GetRequiredService<IProjectService>();
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            await projectService.SetClaimSettings(projectId, projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });
            await projectService.SetCheckInSettings(projectId, checkInProgress: true, enableCheckInModule: true, modelAllowSecondRoles: false);
            await projectService.SetContactSettings(projectId,
                ProjectProfileRequirementSettings.AllNotRequired with { RequirePassport = MandatoryStatus.Required });

            var access = sp.GetRequiredService<IProjectAccessService>();
            await access.GrantAccess(new GrantAccessRequest
            {
                ProjectId = projectId,
                UserId = claimsAdminId,
                Role = new("Администратор заявок"),
                Permissions = [Permission.CanManageClaims],
            });
            await access.GrantAccess(new GrantAccessRequest
            {
                ProjectId = projectId,
                UserId = plainMasterId,
                Role = new("Мастер на стойке"),
                Permissions = [],
            });

            return await sp.GetRequiredService<ICharacterService>().AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [projectInfo.GroupTree.RootGroupId],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: FieldLayerContainer.Empty(projectInfo)));
        });

        var (playerId, _) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(factory.Services);
        var claimId = await factory.Services.RunAsAsync(playerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<IClaimService>().AddClaimFromUser(
                characterId, "Заявка от игрока", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: true);
        });

        await factory.Services.RunAsAsync(ownerId, async sp =>
        {
            await sp.GetRequiredService<IClaimService>().ApproveByMaster(claimId, "Принято");
            await sp.GetRequiredService<IFinanceService>().ChangeFee(claimId, Fee);
        });

        return new Seed(ownerId, claimsAdminId, plainMasterId, claimId);
    }
}
