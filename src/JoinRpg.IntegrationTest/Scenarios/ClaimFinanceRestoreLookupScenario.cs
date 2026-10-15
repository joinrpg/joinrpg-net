using System.Data.Entity;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Чтение для плана восстановления ручных финансовых операций (DRP, #5561) на настоящем SQL Server:
/// <see cref="IProjectRestoreLookupRepository.GetClaimFinanceForRestore"/> и
/// <see cref="IProjectRestoreLookupRepository.GetBlockedProjects"/>.
/// </summary>
public class ClaimFinanceRestoreLookupScenario(JoinApplicationFactory factory)
    : IClassFixture<JoinApplicationFactory>
{
    /// <summary>
    /// Свои операции и финансовые комментарии не заняты, нефинансовый комментарий той же заявки и чужой
    /// комментарий — заняты; время — UTC; типы оплаты и мастера с правом на деньги — проекта.
    /// </summary>
    [Fact]
    public async Task FinanceLookup_ReadsOperationsCommentsAndTakenIds()
    {
        UserIdentification masterId, playerId, otherPlayerId;
        ProjectIdentification projectId, otherProjectId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId);
            otherProjectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId);
            playerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            otherPlayerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        }
        var cashId = await CreateCash(masterId, projectId);

        var claimId = await AddClaim(playerId, await AddCharacter(masterId, projectId));
        var neighbourClaimId = await AddClaim(otherPlayerId, await AddCharacter(masterId, projectId));
        var foreignClaimId = await AddClaim(playerId, await AddCharacter(masterId, otherProjectId));

        // Мастер принял взнос; игрок сообщил об оплате, мастер подтвердил; плюс обычные комментарии.
        await AcceptFee(masterId, claimId, cashId, 1000);
        await AcceptFee(playerId, claimId, cashId, 500);
        int masterOperationId, playerOperationId, moderationId, plainId, foreignId;
        using (var scope = factory.Services.CreateScope())
        {
            var ids = await scope.ServiceProvider.GetRequiredService<MyDbContext>().Set<FinanceOperation>()
                .Where(f => f.ClaimId == claimId.ClaimId).OrderBy(f => f.CommentId).Select(f => f.CommentId).ToListAsync();
            (masterOperationId, playerOperationId) = (ids[0], ids[1]);
        }
        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var claimService = sp.GetRequiredService<IClaimService>();
            await claimService.ModerateFinanceOperation(claimId, playerOperationId, "Пришло", FinanceOperationAction.Approve);
            await claimService.AddComment(claimId, parentCommentId: null, isVisibleToPlayer: true, "Обычный");
            await claimService.AddComment(foreignClaimId, parentCommentId: null, isVisibleToPlayer: true, "Другой проект");
        });

        var createdAt = new DateTime(2026, 7, 2, 10, 0, 0, DateTimeKind.Utc);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            moderationId = await db.Set<Comment>().Where(c => c.ParentCommentId == playerOperationId).Select(c => c.CommentId).SingleAsync();
            int LastIdByText(string text) => db.Set<Comment>().Where(c => c.CommentText.Text.Contents == text).Max(c => c.CommentId);
            plainId = LastIdByText("Обычный");
            foreignId = LastIdByText("Другой проект");
            _ = await db.Database.ExecuteSqlCommandAsync(
                "UPDATE dbo.FinanceOperations SET Created = @p1 WHERE CommentId = @p0", masterOperationId, createdAt);
            _ = await db.Database.ExecuteSqlCommandAsync(
                "UPDATE dbo.Projects SET IsBlocked = 1 WHERE ProjectId = @p0", projectId.Value);
        }

        // Несуществующих id больше пачки запроса (1000) — занятые находятся во всех пачках.
        int[] notified = [masterOperationId, playerOperationId, plainId, .. Enumerable.Range(int.MaxValue - 3000, 1500), moderationId, foreignId];

        var (lookup, blocked) = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var repository = sp.GetRequiredService<IProjectRestoreLookupRepository>();
            return (await repository.GetClaimFinanceForRestore(projectId, notified), await repository.GetBlockedProjects());
        });

        blocked.ShouldContain(projectId);
        blocked.ShouldNotContain(otherProjectId);

        lookup.Claims.ShouldBe([claimId, neighbourClaimId], ignoreOrder: true);

        lookup.Operations.Select(o => o.OperationId).ShouldBe([masterOperationId, playerOperationId], ignoreOrder: true);
        var masterOperation = lookup.Operations.Single(o => o.OperationId == masterOperationId);
        masterOperation.ClaimId.ShouldBe(claimId);
        masterOperation.Type.ShouldBe(FinanceOperationType.Submit);
        masterOperation.State.ShouldBe(FinanceOperationState.Approved);
        masterOperation.Money.ShouldBe(1000);
        masterOperation.CreatedAt.Offset.ShouldBe(TimeSpan.Zero);
        masterOperation.CreatedAt.ShouldBe(new DateTimeOffset(createdAt));
        lookup.Operations.Single(o => o.OperationId == playerOperationId).State.ShouldBe(FinanceOperationState.Approved);

        // Финансовые комментарии — операции и решение; обычный комментарий сюда не попадает.
        lookup.Comments.Select(c => (c.CommentId, c.ExtraAction)).ShouldBe(
            [
                (masterOperationId, CommentExtraAction.PaidFee),
                (playerOperationId, CommentExtraAction.PaidFee),
                (moderationId, CommentExtraAction.ApproveFinance),
            ],
            ignoreOrder: true);
        var moderation = lookup.Comments.Single(c => c.CommentId == moderationId);
        moderation.ClaimId.ShouldBe(claimId);
        moderation.Author.ShouldBe(masterId);
        moderation.CreatedAt.Offset.ShouldBe(TimeSpan.Zero);

        // Заняты: нефинансовый комментарий той же заявки и комментарий другого проекта.
        lookup.TakenCommentIds.ShouldBe([plainId, foreignId], ignoreOrder: true);

        var cash = lookup.PaymentTypes.Single(p => p.PaymentTypeId == cashId);
        cash.Kind.ShouldBe(PaymentTypeKind.Cash);
        cash.Owner.ShouldBe(masterId);
        cash.IsActive.ShouldBeTrue();
        cash.Name.ShouldNotBeNullOrWhiteSpace();
        lookup.PaymentTypes.ShouldAllBe(p => p.PaymentTypeId.ProjectId == projectId);

        lookup.MoneyMasters.ShouldBe([new MoneyMasterForRestore(masterId, IsOwner: true)]);
    }

    private Task<PaymentTypeIdentification> CreateCash(UserIdentification masterId, ProjectIdentification projectId)
        => factory.Services.RunAsAsync(masterId, async sp =>
        {
            await sp.GetRequiredService<IProjectFinanceSettingsService>().CreatePaymentType(new CreatePaymentTypeRequest
            {
                ProjectId = projectId,
                TargetMasterId = masterId,
                TypeKind = PaymentTypeKind.Cash,
                Name = null,
            });
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return projectInfo.ProjectFinanceSettings.GetCashPaymentType(masterId)!.PaymentTypeId;
        });

    private Task AcceptFee(UserIdentification userId, ClaimIdentification claimId, PaymentTypeIdentification cashId, int money)
        => factory.Services.RunAsAsync(userId, sp => sp.GetRequiredService<IFinanceService>().FeeAcceptedOperation(new FeeAcceptedOperationRequest
        {
            ClaimId = claimId.ClaimId,
            Contents = "Взнос",
            OperationDate = DateTime.UtcNow.Date,
            Money = money,
            PaymentTypeId = cashId,
        }));

    private Task<CharacterIdentification> AddCharacter(UserIdentification masterId, ProjectIdentification projectId)
        => factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            await sp.GetRequiredService<IProjectService>().SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });
            return await sp.GetRequiredService<ICharacterService>().AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: FieldLayerContainer.Empty(projectInfo)));
        });

    private Task<ClaimIdentification> AddClaim(UserIdentification playerId, CharacterIdentification characterId)
        => factory.Services.RunAsAsync(playerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(characterId.ProjectId);
            return await sp.GetRequiredService<IClaimService>().AddClaimFromUser(
                characterId, "Хочу эту роль", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: false);
        });
}
