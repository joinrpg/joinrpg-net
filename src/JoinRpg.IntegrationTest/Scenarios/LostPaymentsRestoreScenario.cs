using System.Data.Entity;
using System.Data.SqlClient;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.DataModel.Mocks.Fakes;
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
/// Восстановление потерянных онлайн-платежей PSCB (ADR027) на настоящем SQL Server: платёж
/// вставляется с прежним номером (<c>orderId</c> банка — это <c>CommentId</c>) строго ниже счётчика
/// комментариев вместе со строкой <c>FinanceOperationBankDetails</c>, номер, занятый комментарием
/// чужого обсуждения, даёт отказ, а не вставку с новым, комментарий своего обсуждения без операции
/// (случай DRP) получает дописанную операцию (Attached) без нового комментария и без правки текста,
/// номер не ниже счётчика — не потерянный, повторный прогон узнаётся, наличный тип оплаты
/// отсекается батчем до записи, а возврат вставляется с новым номером и ссылкой на исходную
/// операцию. Сквозной прогон доменного сервиса <see cref="ILostPaymentsRestoreService"/> —
/// единственное место, где вставка сырым SQL и пересчёт взноса тем же контекстом EF проверяются
/// на настоящей базе вместе. Отдельно —
/// <see cref="IClaimsRepository.GetClaimIdsForPlayers"/> отдаёт заявки в любом статусе, включая
/// отклонённые: фильтра по статусу в запросе нет, и это единственное место, где факт проверяется
/// по-настоящему.
/// </summary>
public class LostPaymentsRestoreScenario(JoinApplicationFactory factory)
    : IClassFixture<JoinApplicationFactory>
{
    private static readonly DateTime PaymentAt = new(2026, 7, 5, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime RefundAt = new(2026, 8, 2, 14, 0, 0, DateTimeKind.Utc);
    // Момент восстановления — время создания и правки записи операции; время комментария —
    // момент денег (OperationDate): комментарий встаёт в историю обсуждения на своё место.
    private static readonly DateTime RestoredAt = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
    private const string PaymentText = "[восстановлено] Онлайн-оплата подтверждена.";
    private const string RefundText = "[восстановлено] Оформлен возврат платежа.";
    private const string BankPaymentKey = "pscb-payment-000123";
    private const string BankRefundKey = "pscb-refund-000456";
    private const int Money = 1500;

    // Ключ платежа сквозного прогона сервиса — свой, чтобы записи двух тестов не пересекались.
    private const string ServiceBankPaymentKey = "pscb-payment-000777";

    // Дата, с которой действует расписание взносов сквозного прогона: CreateFeeSetting переносит
    // самую раннюю строку на Project.CreatedDate, поэтому проект «старят» до этой даты (см. тест).
    private static readonly DateTime FeeEffectiveFrom = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

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
            CreatedAt: RestoredAt,
            ChangedAt: RestoredAt,
            OperationDate: PaymentAt,
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
            // Время разделено (ADR027): запись операции помнит момент восстановления, а её
            // OperationDate и время комментария — момент денег.
            operation.Created.ShouldBe(RestoredAt);
            operation.Changed.ShouldBe(RestoredAt);
            operation.OperationDate.ShouldBe(PaymentAt);

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

        // 6. Случай DRP: номер занят комментарием этого же обсуждения без операции — Attached,
        // операция и банковские реквизиты дописаны к существующему комментарию, его текст не
        // тронут и новый комментарий не создан. Значение счётчика и выше — NotLostId: ничего
        // не вставлено, счётчик не сдвинут.
        string? plainTextBefore;
        int commentsInDiscussionBefore;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            plainTextBefore = await db.Set<Comment>().AsNoTracking()
                .Where(c => c.CommentId == plainCommentId)
                .Select(c => c.CommentText.Text.Contents).SingleAsync();
            commentsInDiscussionBefore = await db.Set<Comment>().CountAsync(c => c.CommentDiscussionId == discussionId);
        }

        (await InsertLostPayment(masterId, PaymentRow(plainCommentId)))
            .ShouldBe(new RestoredFinanceOperationInsert(RestoredCommentInsertResult.Attached, plainCommentId));
        (await InsertLostPayment(masterId, PaymentRow(identityBefore)))
            .ShouldBe(new RestoredFinanceOperationInsert(RestoredCommentInsertResult.NotLostId, identityBefore));
        (await InsertLostPayment(masterId, PaymentRow(identityBefore + 1000)))
            .ShouldBe(new RestoredFinanceOperationInsert(RestoredCommentInsertResult.NotLostId, identityBefore + 1000));

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            // Ни нового комментария, ни правки текста: комментарий DRP остался как был.
            (await db.Set<Comment>().CountAsync(c => c.CommentDiscussionId == discussionId)).ShouldBe(commentsInDiscussionBefore);
            (await db.Set<Comment>().AsNoTracking()
                .Where(c => c.CommentId == plainCommentId)
                .Select(c => c.CommentText.Text.Contents).SingleAsync()).ShouldBe(plainTextBefore);

            var attachedOperation = await db.Set<FinanceOperation>().AsNoTracking().SingleAsync(f => f.CommentId == plainCommentId);
            attachedOperation.ProjectId.ShouldBe(projectId.Value);
            attachedOperation.ClaimId.ShouldBe(claimId.ClaimId);
            attachedOperation.OperationType.ShouldBe(FinanceOperationType.Online);
            attachedOperation.State.ShouldBe(FinanceOperationState.Approved);
            attachedOperation.MoneyAmount.ShouldBe(Money);
            attachedOperation.PaymentTypeId.ShouldBe(onlineTypeId.PaymentTypeId);
            attachedOperation.RefundedOperationId.ShouldBeNull();
            attachedOperation.OperationDate.ShouldBe(PaymentAt);

            var attachedBankDetails = await db.Set<FinanceOperationBankDetails>().AsNoTracking()
                .SingleAsync(b => b.CommentId == plainCommentId);
            attachedBankDetails.BankOperationKey.ShouldBe(BankPaymentKey);
            attachedBankDetails.BankRefundKey.ShouldBeNull();

            // Дописка не трогает IDENTITY, а NotLostId ничего не вставил.
            (await GetCommentsIdentityAsync(db)).ShouldBe(identityBefore);
            (await db.Set<FinanceOperation>().CountAsync(f => f.ClaimId == claimId.ClaimId)).ShouldBe(2);
            (await db.Set<FinanceOperationBankDetails>().CountAsync(b => b.CommentId != lostOrderId && b.CommentId != plainCommentId)).ShouldBe(0);
        }

        // 7. Настоящий конфликт номера: комментарий в обсуждении другой заявки того же проекта —
        // TakenElsewhere, ничего не записано (деньги не должны лечь на непредвиденную заявку).
        var otherClaimId = await AddClaim(playerId, await AddCharacter(masterId, projectId));
        int otherDiscussionId, otherCommentId, identityAfterOtherClaim;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            otherDiscussionId = await GetDiscussionIdAsync(db, otherClaimId);
            otherCommentId = await db.Set<Comment>()
                .Where(c => c.CommentDiscussionId == otherDiscussionId).MinAsync(c => c.CommentId);
            identityAfterOtherClaim = await GetCommentsIdentityAsync(db);
        }

        (await InsertLostPayment(masterId, PaymentRow(otherCommentId)))
            .ShouldBe(new RestoredFinanceOperationInsert(RestoredCommentInsertResult.TakenElsewhere, otherCommentId));
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            (await db.Set<FinanceOperation>().AnyAsync(f => f.CommentId == otherCommentId)).ShouldBeFalse();
            (await db.Set<FinanceOperation>().CountAsync(f => f.ClaimId == claimId.ClaimId)).ShouldBe(2);
            (await GetCommentsIdentityAsync(db)).ShouldBe(identityAfterOtherClaim);
        }

        // 8. Последний рубеж обороны перед записью денег: наличный тип мастера не пройдёт в
        // онлайн-операцию — батч отказывает до вставки, иначе сверка мастера рассинхронизировалась бы.
        var cashTypeId = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            await sp.GetRequiredService<IProjectFinanceSettingsService>().CreatePaymentType(new CreatePaymentTypeRequest
            {
                ProjectId = projectId,
                TargetMasterId = masterId,
                TypeKind = PaymentTypeKind.Cash,
                Name = null,
            });
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return projectInfo.ProjectFinanceSettings.PaymentTypes
                .Single(pt => pt.TypeKind == PaymentTypeKind.Cash).PaymentTypeId;
        });
        var cashException = await Should.ThrowAsync<SqlException>(() => InsertLostPayment(masterId, new LostPaymentRow(
            claimId,
            FinanceOperationType.Online,
            lostOrderId + 1,
            RefundedOperationId: null,
            paymentsUserId,
            IsCommentByPlayer: true,
            Money,
            cashTypeId,
            CreatedAt: RestoredAt,
            ChangedAt: RestoredAt,
            OperationDate: PaymentAt,
            PaymentText,
            BankPaymentKey,
            BankRefundKey: null)));
        cashException.Message.ShouldContain("Online payment type is not found");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            // Отказ до вставки: ни операции, ни комментария с запрошенным номером.
            (await db.Set<FinanceOperation>().CountAsync(f => f.ClaimId == claimId.ClaimId)).ShouldBe(2);
            (await db.Set<Comment>().AnyAsync(c => c.CommentId == lostOrderId + 1)).ShouldBeFalse();
        }

        // 9. Возврат: своего номера в банке нет — вставляется с новым номером и ссылкой на исходный
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
            CreatedAt: RestoredAt,
            ChangedAt: RestoredAt,
            OperationDate: RefundAt,
            RefundText,
            BankOperationKey: BankPaymentKey,
            BankRefundKey);

        var refundInsert = await InsertLostPayment(masterId, refundRow);
        refundInsert.Result.ShouldBe(RestoredCommentInsertResult.Inserted);
        var refundId = refundInsert.OperationId;
        refundId.ShouldBe(identityAfterOtherClaim + 1);

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
            refundComment.LastEditTime.ShouldBe(RefundAt);

            var refundOperation = await db.Set<FinanceOperation>().AsNoTracking().SingleAsync(f => f.CommentId == refundId);
            refundOperation.ClaimId.ShouldBe(claimId.ClaimId);
            refundOperation.OperationType.ShouldBe(FinanceOperationType.Refund);
            refundOperation.State.ShouldBe(FinanceOperationState.Approved);
            refundOperation.MoneyAmount.ShouldBe(-Money);
            refundOperation.RefundedOperationId.ShouldBe(lostOrderId);
            // То же разделение времени, что и у платежа.
            refundOperation.Created.ShouldBe(RestoredAt);
            refundOperation.Changed.ShouldBe(RestoredAt);
            refundOperation.OperationDate.ShouldBe(RefundAt);

            var refundBankDetails = await db.Set<FinanceOperationBankDetails>().AsNoTracking()
                .SingleAsync(b => b.CommentId == refundId);
            refundBankDetails.BankOperationKey.ShouldBe(BankPaymentKey);
            refundBankDetails.BankRefundKey.ShouldBe(BankRefundKey);

            (await GetCommentsIdentityAsync(db)).ShouldBe(identityAfterOtherClaim + 1);
        }

        // 10. Повторный прогон возврата узнаётся по уже проведённому возврату той же операции.
        (await InsertLostPayment(masterId, refundRow))
            .ShouldBe(new RestoredFinanceOperationInsert(RestoredCommentInsertResult.AlreadyExists, refundId));
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            (await db.Set<FinanceOperation>().CountAsync(f => f.ClaimId == claimId.ClaimId)).ShouldBe(3);
            (await GetCommentsIdentityAsync(db)).ShouldBe(identityAfterOtherClaim + 1);
        }
    }

    /// <summary>
    /// Восстановление платежа доменным сервисом <see cref="ILostPaymentsRestoreService"/> на
    /// настоящей базе — единственное место, где связка «сырой SQL + EF» проверяется целиком:
    /// заявка загружается до вставки, операция вставляется батчем репозитория мимо контекста,
    /// а затем тот же контекст делает <c>SaveChangesAsync</c> с пересчётом взноса по настоящему
    /// снимку (<c>IClaimInfoRepository</c>) — трекер изменений не должен спутаться со строкой,
    /// которую он никогда не видел, а взнос обязан действительно зафиксироваться в базе.
    /// Юнит-тестовый фейк <c>FakeCharacterAggregateWriteRepository.InsertLostPayment</c> добавляет
    /// операцию прямо в <c>claim.FinanceOperations</c> той же сущности, то есть ведёт себя иначе,
    /// чем бой, и эту связку смоделировать не может. Уведомление наблюдается по очереди тестового
    /// хоста (<see cref="FakeNotificationRepository"/>).
    /// </summary>
    [Fact]
    public async Task RestoreByService_InsertsByRawSqlAndFixesFee_ThroughTheSameDbContext()
    {
        // 1. Проект с онлайн-типом оплаты, игрок с заявкой и расписание взносов, действующее на
        // историческую дату денег: без ненулевого взноса «взнос зафиксирован» не проверить.
        var (masterId, projectId, onlineTypeId) = await CreateProjectWithOnlinePaymentType();
        UserIdentification playerId;
        using (var scope = factory.Services.CreateScope())
        {
            playerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        }
        var claimId = await AddClaim(playerId, await AddCharacter(masterId, projectId));

        // Расписание заводится боевым сервисом, но CreateFeeSetting не принимает даты старше
        // вчера, а самую раннюю строку переносит на Project.CreatedDate. Поэтому проект сначала
        // «старят» прямо в БД (тот же приём, что BlockProject в FinanceRestoreViewScenario) —
        // тогда боевой сервис сам раздвинет расписание на историческую дату платежа.
        using (var scope = factory.Services.CreateScope())
        {
            _ = await scope.ServiceProvider.GetRequiredService<MyDbContext>().Database.ExecuteSqlCommandAsync(
                "UPDATE dbo.Projects SET CreatedDate = @p0 WHERE ProjectId = @p1",
                FeeEffectiveFrom, projectId.Value);
        }
        await factory.Services.RunAsAsync(masterId, sp =>
            sp.GetRequiredService<IProjectFinanceSettingsService>().CreateFeeSetting(new CreateFeeSettingRequest
            {
                ProjectId = projectId,
                Fee = Money,
                PreferentialFee = null,
                StartDate = DateTime.UtcNow.Date,
            }));

        var paymentsUserId = await factory.Services.RunAsAsync(masterId, sp =>
            Task.FromResult(sp.GetRequiredService<IVirtualUsersService>().PaymentsUser.GetId()));

        // 2. Дырка в счётчике комментариев: потерянный orderId строго ниже счётчика. Заодно
        // состояние заявки до восстановления: взнос не зафиксирован, отметки времени свои.
        int discussionId, lostOrderId, identityBefore;
        DateTimeOffset lastPlayerCommentAtBefore;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            discussionId = await GetDiscussionIdAsync(db, claimId);
            lostOrderId = await BurnTwoCommentIdsAsync(db, projectId, discussionId, masterId);
            identityBefore = await GetCommentsIdentityAsync(db);
            identityBefore.ShouldBeGreaterThan(lostOrderId);

            var claimBefore = await db.Set<Claim>().AsNoTracking().SingleAsync(c => c.ClaimId == claimId.ClaimId);
            claimBefore.CurrentFee.ShouldBeNull();
            // Заявка подана сейчас, а деньги «были» в прошлом — иначе правило «отметка о
            // комментарии игрока не уезжает в прошлое» не отличить от простой перезаписи.
            claimBefore.LastPlayerCommentAt.ShouldNotBeNull();
            claimBefore.LastPlayerCommentAt.Value.UtcDateTime.ShouldBeGreaterThan(PaymentAt);
            lastPlayerCommentAtBefore = claimBefore.LastPlayerCommentAt.Value;
        }

        // 3. Восстановление сервисом от имени сайт-админа: LoadClaimAsync требует
        // ICurrentUserAccessor.IsAdmin, а мастер проекта сам по себе не сайт-админ — поэтому
        // RunAsAsync с isAdmin: true (приём из UserMergeScenario).
        const string payerComment = "За Василия — комментарий плательщика из банка";
        var beforeCall = DateTime.UtcNow;
        var result = await factory.Services.RunAsAsync(masterId, sp =>
            sp.GetRequiredService<ILostPaymentsRestoreService>().RestorePaymentAsync(new LostPaymentToRestore(
                claimId,
                lostOrderId,
                onlineTypeId,
                Money,
                PaymentAt,
                ServiceBankPaymentKey,
                payerComment,
                Refund: null)),
            isAdmin: true);

        result.Status.ShouldBe(LostOperationRestoreStatus.Restored);
        result.OperationId.ShouldBe(lostOrderId);
        result.Refund.ShouldBeNull();
        result.FeeRecalculated.ShouldBeTrue();

        // 4. Всё действительно в базе — свежий скоуп и AsNoTracking: строки прочитаны из базы,
        // а не из чужого трекера изменений.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            var operation = await db.Set<FinanceOperation>().AsNoTracking().SingleAsync(f => f.CommentId == lostOrderId);
            operation.ProjectId.ShouldBe(projectId.Value);
            operation.ClaimId.ShouldBe(claimId.ClaimId);
            operation.OperationType.ShouldBe(FinanceOperationType.Online);
            operation.State.ShouldBe(FinanceOperationState.Approved);
            operation.MoneyAmount.ShouldBe(Money);
            operation.PaymentTypeId.ShouldBe(onlineTypeId.PaymentTypeId);
            operation.RefundedOperationId.ShouldBeNull();
            operation.OperationDate.ShouldBe(PaymentAt);
            // Разделение времени ADR027 сквозным прогоном: Created/Changed — момент восстановления
            // (поставлен сервисом после beforeCall), а не момент денег.
            operation.Created.ShouldBeGreaterThanOrEqualTo(beforeCall);
            operation.Created.ShouldNotBe(operation.OperationDate);
            operation.Changed.ShouldBe(operation.Created);

            var bankDetails = await db.Set<FinanceOperationBankDetails>().AsNoTracking()
                .SingleAsync(b => b.CommentId == lostOrderId);
            bankDetails.BankOperationKey.ShouldBe(ServiceBankPaymentKey);
            bankDetails.BankRefundKey.ShouldBeNull();

            var comment = await db.Set<Comment>().AsNoTracking().SingleAsync(c => c.CommentId == lostOrderId);
            comment.CommentDiscussionId.ShouldBe(discussionId);
            comment.AuthorUserId.ShouldBe(paymentsUserId.Value);
            comment.IsCommentByPlayer.ShouldBeTrue();
            comment.IsVisibleToPlayer.ShouldBeTrue();
            comment.ExtraAction.ShouldBeNull();
            // Время комментария — момент денег: запись встаёт в историю обсуждения на своё место.
            comment.CreatedAt.ShouldBe(PaymentAt);
            comment.LastEditTime.ShouldBe(PaymentAt);
            var commentText = await db.Set<Comment>().AsNoTracking()
                .Where(c => c.CommentId == lostOrderId)
                .Select(c => c.CommentText.Text.Contents).SingleAsync();
            commentText.ShouldStartWith("[восстановлено]");
            commentText.ShouldContain(payerComment);

            // Пересчёт взноса тем же контекстом, который не видел вставки сырым SQL: сохранение
            // прошло, взнос зафиксирован в базе по расписанию на дату денег (GetFeeToFix).
            var claim = await db.Set<Claim>().AsNoTracking().SingleAsync(c => c.ClaimId == claimId.ClaimId);
            claim.CurrentFee.ShouldBe(Money);
            claim.LastUpdateDateTime.ShouldBeGreaterThanOrEqualTo(beforeCall);
            // Отметка о последнем комментарии игрока назад не уехала: деньги исторические, а
            // заявка изменилась сейчас — SettleAsync двигает отметку только вперёд.
            claim.LastPlayerCommentAt.ShouldBe(lastPlayerCommentAtBefore);

            // Вставка с прежним номером не двигала счётчик.
            (await GetCommentsIdentityAsync(db)).ShouldBe(identityBefore);
        }

        // 5. Уведомление об оплате поставлено в очередь после коммита: очередь тестового хоста —
        // FakeNotificationRepository-синглтон, он записывает принятые InsertNotifications сообщения.
        var expectedReference = new FinanceOperationIdentification(claimId, lostOrderId);
        factory.Services.GetRequiredService<INotificationRepository>().ShouldBeOfType<FakeNotificationRepository>()
            .InsertedNotifications.ShouldContain(n =>
                Equals(n.Message.EntityReference, expectedReference)
                && n.Message.Recipient == playerId
                && n.Message.Initiator == paymentsUserId
                && n.Message.Body.Value.Contains("вновь отражена в вашей заявке"));
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
            new PlayerClaimId(playerId, addedClaimId),
            new PlayerClaimId(playerId, approvedClaimId),
            new PlayerClaimId(playerId, declinedByMasterClaimId),
            new PlayerClaimId(playerId, declinedByPlayerClaimId),
        ], ignoreOrder: true);
        claims.ShouldNotContain(c => c.ClaimId == otherPlayerClaimId);
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
