using System.Data.Entity;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.Domain;
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
/// Восстановление потерянных онлайн-платежей PSCB (ADR026) на настоящем SQL Server: платёж
/// вставляется с прежним номером (<c>orderId</c> банка — это <c>CommentId</c>) строго ниже счётчика
/// комментариев вместе со строкой <c>FinanceOperationBankDetails</c>, занятый номер даёт отказ, а не
/// вставку с новым, номер не ниже счётчика — не потерянный, повторный прогон узнаётся, а возврат
/// вставляется с новым номером и ссылкой на исходную операцию. Отдельно —
/// <see cref="IClaimsRepository.GetClaimIdsForPlayers"/> отдаёт заявки в любом статусе, включая
/// отклонённые: фильтра по статусу в запросе нет, и это единственное место, где факт проверяется
/// по-настоящему.
/// </summary>
public class LostPaymentsRestoreScenario(JoinApplicationFactory factory)
    : IClassFixture<JoinApplicationFactory>
{
    private static readonly DateTime PaymentAt = new(2026, 7, 5, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime RefundAt = new(2026, 8, 2, 14, 0, 0, DateTimeKind.Utc);
    // Момент восстановления — время правки операции; время создания записи — момент денег.
    private static readonly DateTime RestoredAt = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
    private const string PaymentText = "[восстановлено] Онлайн-оплата подтверждена.";
    private const string RefundText = "[восстановлено] Оформлен возврат платежа.";
    private const string BankPaymentKey = "pscb-payment-000123";
    private const string BankRefundKey = "pscb-refund-000456";
    private const int Money = 1500;

    [Fact]
    public async Task LostPayment_InsertsWithBankOrderId_RepeatConflictsAndRefund_BehaveAsDocumented()
    {
        // 1. Проект с онлайн-типом оплаты, игрок с заявкой, служебный пользователь платежей
        var (masterId, projectId, onlineTypeId) = await CreateProjectWithOnlinePaymentType();
        UserIdentification playerId;
        using (var scope = factory.Services.CreateScope())
        {
            playerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        }
        var claimId = await AddClaim(playerId, await AddCharacter(masterId, projectId));
        var paymentsUserId = await factory.Services.RunAsAsync(masterId, sp =>
            Task.FromResult(sp.GetRequiredService<IVirtualUsersService>().PaymentsUser.GetId()));

        // 2. Дырка в счётчике комментариев: потерянный orderId строго ниже счётчика; номер
        // комментария подачи заявки занят комментарием без операции.
        int discussionId, plainCommentId, lostOrderId, identityBefore;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            discussionId = await GetDiscussionIdAsync(db, claimId);
            plainCommentId = await db.Set<Comment>()
                .Where(c => c.CommentDiscussionId == discussionId).MinAsync(c => c.CommentId);
            (await db.Set<FinanceOperation>().AnyAsync(f => f.CommentId == plainCommentId)).ShouldBeFalse();
            lostOrderId = await BurnTwoCommentIdsAsync(db, projectId, discussionId, masterId);
            identityBefore = await GetCommentsIdentityAsync(db);
            identityBefore.ShouldBeGreaterThan(lostOrderId);
        }

        // 3. Платёж вставляется с прежним номером; счётчик не двигается
        LostPaymentRow PaymentRow(int? operationId) => new(
            claimId,
            FinanceOperationType.Online,
            operationId,
            RefundedOperationId: null,
            paymentsUserId,
            IsCommentByPlayer: true,
            Money,
            onlineTypeId,
            CreatedAt: PaymentAt,
            ChangedAt: RestoredAt,
            OperationDate: PaymentAt.Date,
            PaymentText,
            BankPaymentKey,
            BankRefundKey: null);

        (await InsertLostPayment(masterId, PaymentRow(lostOrderId)))
            .ShouldBe(new RestoredFinanceOperationInsert(RestoredCommentInsertResult.Inserted, lostOrderId));

        // 4. Строки — как у боевого онлайн-платежа: комментарий без действия, видимый игроку и
        // со стороны игрока (автор — служебный пользователь), операция Online/Approved и
        // банковские реквизиты с номером платежа и без номера возврата.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            var comment = await db.Set<Comment>().AsNoTracking().SingleAsync(c => c.CommentId == lostOrderId);
            comment.ProjectId.ShouldBe(projectId.Value);
            comment.CommentDiscussionId.ShouldBe(discussionId);
            comment.ParentCommentId.ShouldBeNull();
            comment.AuthorUserId.ShouldBe(paymentsUserId.Value);
            comment.IsCommentByPlayer.ShouldBeTrue();
            comment.IsVisibleToPlayer.ShouldBeTrue();
            // У боевого онлайн-платежа действия нет: PaidFee означало бы взнос, принятый мастером руками.
            comment.ExtraAction.ShouldBeNull();
            comment.CreatedAt.ShouldBe(PaymentAt);
            comment.LastEditTime.ShouldBe(PaymentAt);
            (await db.Set<Comment>().AsNoTracking().Where(c => c.CommentId == lostOrderId)
                .Select(c => c.CommentText.Text.Contents).SingleAsync()).ShouldBe(PaymentText);

            var operation = await db.Set<FinanceOperation>().AsNoTracking().SingleAsync(f => f.CommentId == lostOrderId);
            operation.ProjectId.ShouldBe(projectId.Value);
            operation.ClaimId.ShouldBe(claimId.ClaimId);
            operation.OperationType.ShouldBe(FinanceOperationType.Online);
            operation.State.ShouldBe(FinanceOperationState.Approved);
            operation.MoneyAmount.ShouldBe(Money);
            operation.PaymentTypeId.ShouldBe(onlineTypeId.PaymentTypeId);
            operation.RefundedOperationId.ShouldBeNull();
            operation.Created.ShouldBe(PaymentAt);
            operation.Changed.ShouldBe(RestoredAt);
            operation.OperationDate.ShouldBe(PaymentAt.Date);

            var bankDetails = await db.Set<FinanceOperationBankDetails>().AsNoTracking()
                .SingleAsync(b => b.CommentId == lostOrderId);
            bankDetails.BankOperationKey.ShouldBe(BankPaymentKey);
            bankDetails.BankRefundKey.ShouldBeNull();

            // Вставка ниже счётчика его не двигает.
            (await GetCommentsIdentityAsync(db)).ShouldBe(identityBefore);
        }

        // 5. Повторный прогон: номер уже занят операцией этого же обсуждения — AlreadyExists,
        // ничего не вставляется.
        (await InsertLostPayment(masterId, PaymentRow(lostOrderId)))
            .ShouldBe(new RestoredFinanceOperationInsert(RestoredCommentInsertResult.AlreadyExists, lostOrderId));

        // 6. Конфликты номера: занятый комментарий без операции — TakenElsewhere, значение счётчика
        // и выше — NotLostId. Ничего не вставлено, счётчик не сдвинут.
        (await InsertLostPayment(masterId, PaymentRow(plainCommentId)))
            .ShouldBe(new RestoredFinanceOperationInsert(RestoredCommentInsertResult.TakenElsewhere, plainCommentId));
        (await InsertLostPayment(masterId, PaymentRow(identityBefore)))
            .ShouldBe(new RestoredFinanceOperationInsert(RestoredCommentInsertResult.NotLostId, identityBefore));
        (await InsertLostPayment(masterId, PaymentRow(identityBefore + 1000)))
            .ShouldBe(new RestoredFinanceOperationInsert(RestoredCommentInsertResult.NotLostId, identityBefore + 1000));
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            (await GetCommentsIdentityAsync(db)).ShouldBe(identityBefore);
            (await db.Set<FinanceOperation>().CountAsync(f => f.ClaimId == claimId.ClaimId)).ShouldBe(1);
            (await db.Set<FinanceOperationBankDetails>().CountAsync(b => b.CommentId != lostOrderId)).ShouldBe(0);
        }

        // 7. Возврат: своего номера в банке нет — вставляется с новым номером и ссылкой на исходный
        // платёж, сумма отрицательная, в реквизитах оба ключа.
        LostPaymentRow refundRow = new(
            claimId,
            FinanceOperationType.Refund,
            OperationId: null,
            RefundedOperationId: lostOrderId,
            paymentsUserId,
            IsCommentByPlayer: true,
            -Money,
            onlineTypeId,
            CreatedAt: RefundAt,
            ChangedAt: RestoredAt,
            OperationDate: RefundAt.Date,
            RefundText,
            BankOperationKey: BankPaymentKey,
            BankRefundKey);

        var refundInsert = await InsertLostPayment(masterId, refundRow);
        refundInsert.Result.ShouldBe(RestoredCommentInsertResult.Inserted);
        var refundId = refundInsert.OperationId;
        refundId.ShouldBe(identityBefore + 1);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            var refundComment = await db.Set<Comment>().AsNoTracking().SingleAsync(c => c.CommentId == refundId);
            refundComment.CommentDiscussionId.ShouldBe(discussionId);
            refundComment.AuthorUserId.ShouldBe(paymentsUserId.Value);
            refundComment.IsCommentByPlayer.ShouldBeTrue();
            refundComment.IsVisibleToPlayer.ShouldBeTrue();
            refundComment.ExtraAction.ShouldBeNull();
            refundComment.CreatedAt.ShouldBe(RefundAt);

            var refundOperation = await db.Set<FinanceOperation>().AsNoTracking().SingleAsync(f => f.CommentId == refundId);
            refundOperation.ClaimId.ShouldBe(claimId.ClaimId);
            refundOperation.OperationType.ShouldBe(FinanceOperationType.Refund);
            refundOperation.State.ShouldBe(FinanceOperationState.Approved);
            refundOperation.MoneyAmount.ShouldBe(-Money);
            refundOperation.RefundedOperationId.ShouldBe(lostOrderId);
            refundOperation.OperationDate.ShouldBe(RefundAt.Date);

            var refundBankDetails = await db.Set<FinanceOperationBankDetails>().AsNoTracking()
                .SingleAsync(b => b.CommentId == refundId);
            refundBankDetails.BankOperationKey.ShouldBe(BankPaymentKey);
            refundBankDetails.BankRefundKey.ShouldBe(BankRefundKey);

            (await GetCommentsIdentityAsync(db)).ShouldBe(identityBefore + 1);
        }

        // 8. Повторный прогон возврата узнаётся по уже проведённому возврату той же операции.
        (await InsertLostPayment(masterId, refundRow))
            .ShouldBe(new RestoredFinanceOperationInsert(RestoredCommentInsertResult.AlreadyExists, refundId));
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            (await db.Set<FinanceOperation>().CountAsync(f => f.ClaimId == claimId.ClaimId)).ShouldBe(2);
            (await GetCommentsIdentityAsync(db)).ShouldBe(identityBefore + 1);
        }
    }

    [Fact]
    public async Task GetClaimIdsForPlayers_ReturnsClaimsInAnyStatus_IncludingDeclined()
    {
        UserIdentification masterId, playerId, otherPlayerId;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId);
            playerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            otherPlayerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        }

        // Заявки одного игрока в четырёх разных статусах и заявка другого игрока.
        var addedClaimId = await AddClaim(playerId, await AddCharacter(masterId, projectId));
        var approvedClaimId = await AddClaim(playerId, await AddCharacter(masterId, projectId));
        var declinedByMasterClaimId = await AddClaim(playerId, await AddCharacter(masterId, projectId));
        var declinedByPlayerClaimId = await AddClaim(playerId, await AddCharacter(masterId, projectId));
        var otherPlayerClaimId = await AddClaim(otherPlayerId, await AddCharacter(masterId, projectId));

        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var claimService = sp.GetRequiredService<IClaimService>();
            await claimService.ApproveByMaster(approvedClaimId, "Принята");
            await claimService.DeclineByMaster(declinedByMasterClaimId, ClaimDenialReason.NotSuitable,
                "Не подошла", deleteCharacter: false);
        });
        await factory.Services.RunAsAsync(playerId, sp =>
            sp.GetRequiredService<IClaimService>().DeclineByPlayer(declinedByPlayerClaimId, "Передумал"));

        // Предусловие: статусы заявок действительно разные.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            var statuses = await db.Set<Claim>().AsNoTracking()
                .Where(c => c.ProjectId == projectId.Value)
                .ToDictionaryAsync(c => c.ClaimId, c => c.ClaimStatus);
            statuses[addedClaimId.ClaimId].ShouldBe(ClaimStatus.AddedByUser);
            statuses[approvedClaimId.ClaimId].ShouldBe(ClaimStatus.Approved);
            statuses[declinedByMasterClaimId.ClaimId].ShouldBe(ClaimStatus.DeclinedByMaster);
            statuses[declinedByPlayerClaimId.ClaimId].ShouldBe(ClaimStatus.DeclinedByUser);
        }

        var claims = await factory.Services.RunAsAsync(masterId, sp =>
            sp.GetRequiredService<IUnitOfWork>().GetClaimsRepository()
                .GetClaimIdsForPlayers(projectId, [playerId]));

        // Все заявки игрока проекта — в любом статусе, включая отклонённые; чужие заявки не отдаются.
        claims.ShouldBe(
        [
            new PlayerClaimId(playerId.Value, addedClaimId.ClaimId),
            new PlayerClaimId(playerId.Value, approvedClaimId.ClaimId),
            new PlayerClaimId(playerId.Value, declinedByMasterClaimId.ClaimId),
            new PlayerClaimId(playerId.Value, declinedByPlayerClaimId.ClaimId),
        ], ignoreOrder: true);
        claims.ShouldNotContain(c => c.ClaimId == otherPlayerClaimId.ClaimId);
    }

    private async Task<(UserIdentification MasterId, ProjectIdentification ProjectId, PaymentTypeIdentification OnlineTypeId)> CreateProjectWithOnlinePaymentType()
    {
        UserIdentification masterId;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId);
        }

        var onlineTypeId = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            await sp.GetRequiredService<IProjectFinanceSettingsService>().CreatePaymentType(new CreatePaymentTypeRequest
            {
                ProjectId = projectId,
                TargetMasterId = null,
                TypeKind = PaymentTypeKind.Online,
                Name = null,
            });
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return projectInfo.ProjectFinanceSettings.PaymentTypes
                .Single(pt => pt.TypeKind == PaymentTypeKind.Online).PaymentTypeId;
        });
        return (masterId, projectId, onlineTypeId);
    }

    private Task<RestoredFinanceOperationInsert> InsertLostPayment(UserIdentification masterId, LostPaymentRow row)
        => factory.Services.RunAsAsync(masterId, sp =>
            sp.GetRequiredService<IUnitOfWork>().GetCharacterAggregateWriteRepository().InsertLostPayment(row));

    private Task<CharacterIdentification> AddCharacter(UserIdentification masterId, ProjectIdentification projectId)
        => factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            // «Строго один персонаж» выключен: при нём утверждение одной заявки автоматически
            // отклоняет остальные активные заявки игрока в проекте (ClaimServiceImpl.ApproveCore),
            // а тесту нужны заявки одного игрока в четырёх разных статусах.
            await sp.GetRequiredService<IProjectService>().SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true, StrictlyOneCharacter = false });
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

    private static Task<int> GetDiscussionIdAsync(MyDbContext db, ClaimIdentification claimId)
        => db.Set<Claim>().Where(c => c.ClaimId == claimId.ClaimId).Select(c => c.CommentDiscussionId).SingleAsync();

    /// <summary>
    /// Расходует два значения IDENTITY таблицы комментариев вставками, которые тут же откатываются, и
    /// возвращает первое из них — оно строго ниже счётчика. Переменная откат транзакции переживает.
    /// </summary>
    private static Task<int> BurnTwoCommentIdsAsync(MyDbContext db, ProjectIdentification projectId, int discussionId, UserIdentification authorId)
    {
        const string sql = """
            SET NOCOUNT ON;
            DECLARE @first int;
            BEGIN TRANSACTION;
            INSERT INTO dbo.Comments (ProjectId, CommentDiscussionId, CreatedTime, LastEditTime, AuthorUserId, IsCommentByPlayer, IsVisibleToPlayer)
            VALUES (@p0, @p1, @p3, @p3, @p2, 0, 1);
            SET @first = CAST(SCOPE_IDENTITY() AS int);
            INSERT INTO dbo.Comments (ProjectId, CommentDiscussionId, CreatedTime, LastEditTime, AuthorUserId, IsCommentByPlayer, IsVisibleToPlayer)
            VALUES (@p0, @p1, @p3, @p3, @p2, 0, 1);
            ROLLBACK TRANSACTION;
            SELECT @first;
            """;

        return db.Database.SqlQuery<int>(sql, projectId.Value, discussionId, authorId.Value, DateTime.UtcNow).SingleAsync();
    }

    private static Task<int> GetCommentsIdentityAsync(MyDbContext db)
        => db.Database.SqlQuery<int>("SELECT CAST(IDENT_CURRENT('dbo.Comments') AS int)").SingleAsync();
}
