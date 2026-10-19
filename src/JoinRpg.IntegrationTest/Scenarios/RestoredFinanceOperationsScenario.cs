using System.Data.Entity;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Drp;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Восстановление ручных финансовых операций по уведомлениям (DRP, #5561) на настоящем SQL Server:
/// прежние id через <c>SET IDENTITY_INSERT</c>, паритет строк с доменным приёмом взноса и доменным
/// решением по нему, решение по операции, которая уже есть в базе, повтор, конфликты id, тип оплаты в
/// заблокированном проекте и дата последнего обновления проекта, которую восстановление обязано
/// поднять (ADR023).
/// </summary>
public class RestoredFinanceOperationsScenario(JoinApplicationFactory factory)
    : IClassFixture<JoinApplicationFactory>
{
    private static readonly DateTime BackupAt = new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
    // 22:30 UTC — в Москве (часовой пояс проекта по умолчанию) уже 2 июля.
    private static readonly DateTimeOffset MasterAcceptedAt = new(2026, 7, 1, 22, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PlayerPaidAt = new(2026, 7, 5, 10, 0, 0, TimeSpan.FromHours(3));
    private static readonly DateTimeOffset ModeratedAt = new(2026, 7, 6, 10, 0, 0, TimeSpan.FromHours(3));
    private static readonly DateTimeOffset DeclinedAt = new(2026, 7, 7, 10, 0, 0, TimeSpan.FromHours(3));
    private const string MasterText = "Принял {наличными}";
    private const string PlayerText = "Перевёл мастеру";
    private const string ApproveText = "Пришло";

    [Fact]
    public async Task RestoreFinanceOperations_InsertsWithGivenIds_LikeDomainOperations_AndBumpsProjectUpdate()
    {
        // 1. Мастер с наличным типом оплаты, проект, два игрока с заявками
        var (masterId, projectId, cashId) = await CreateProjectWithCash();
        UserIdentification playerId;
        UserIdentification otherPlayerId;
        using (var scope = factory.Services.CreateScope())
        {
            playerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            otherPlayerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        }
        var claimId = await AddClaim(playerId, await AddCharacter(masterId, projectId));
        var otherClaimId = await AddClaim(otherPlayerId, await AddCharacter(masterId, projectId));

        // 2. Эталоны — доменные: мастер принял взнос; игрок сообщил об оплате, мастер подтвердил;
        // и платёж игрока, который ждёт решения (его решение восстановится из уведомлений).
        await AcceptFee(masterId, claimId, cashId, 1000, MasterText);
        await AcceptFee(playerId, claimId, cashId, 500, PlayerText);
        var (approvedReferenceId, playerReferenceId) = await GetTwoOperationIds(claimId);
        await factory.Services.RunAsAsync(masterId, sp => sp.GetRequiredService<IClaimService>()
            .ModerateFinanceOperation(claimId, playerReferenceId, ApproveText, FinanceOperationAction.Approve));
        await AcceptFee(playerId, claimId, cashId, 300, PlayerText);
        int waitingId;
        using (var scope = factory.Services.CreateScope())
        {
            waitingId = await scope.ServiceProvider.GetRequiredService<MyDbContext>().Set<FinanceOperation>()
                .Where(f => f.ClaimId == claimId.ClaimId).MaxAsync(f => f.CommentId);
        }

        // 3. Дырки в счётчике комментариев (как после сдвига счётчиков, #5460); проект «с майскими датами»
        int lostOperationId;
        int lostModerationId;
        int identityBefore;
        CommentRow approvedReferenceComment, playerReferenceComment, moderationReferenceComment;
        FinanceRow approvedReference, playerReference;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            approvedReferenceComment = await GetCommentAsync(db, approvedReferenceId);
            playerReferenceComment = await GetCommentAsync(db, playerReferenceId);
            moderationReferenceComment = await GetCommentAsync(db,
                await db.Set<Comment>().Where(c => c.ParentCommentId == playerReferenceId).Select(c => c.CommentId).SingleAsync());
            approvedReference = await GetFinanceAsync(db, approvedReferenceId);
            playerReference = await GetFinanceAsync(db, playerReferenceId);

            var discussionId = await GetDiscussionIdAsync(db, claimId);
            lostOperationId = await BurnTwoCommentIdsAsync(db, projectId, discussionId, masterId);
            lostModerationId = await BurnTwoCommentIdsAsync(db, projectId, discussionId, masterId);
            identityBefore = await GetCommentsIdentityAsync(db);
            identityBefore.ShouldBeGreaterThan(lostModerationId);

            _ = await BackdateProjectAsync(db, projectId);
        }
        await BlockProject(projectId);
        (await GetProjectLastUpdatedAsync(masterId, projectId)).ShouldBe(DateOnly.FromDateTime(BackupAt));

        RestoredFinanceOperation[] operations =
        [
            new(lostOperationId, masterId, IsCommentByPlayer: false, FinanceOperationType.Submit, 1000, cashId,
                FinanceOperationState.Approved, MasterAcceptedAt, MasterAcceptedAt, MasterText),
            // До 2026-06-06 id в уведомлениях не было — получит новый.
            new(null, playerId, IsCommentByPlayer: true, FinanceOperationType.Submit, 500, cashId,
                FinanceOperationState.Approved, PlayerPaidAt, ModeratedAt, PlayerText),
        ];
        RestoredFinanceModeration[] moderations =
        [
            new(lostModerationId, masterId, Approved: true, ModeratedAt, ApproveText, ParentOperationId: null, ParentRestoredIndex: 1),
            // Решение по платежу, который есть в базе и ждёт решения: переводит его в итоговое состояние.
            new(null, masterId, Approved: false, DeclinedAt, "Не пришло", ParentOperationId: waitingId, ParentRestoredIndex: null),
        ];

        // 4. Восстановление
        var before = DateTime.UtcNow.AddSeconds(-1);
        var result = await Restore(masterId, claimId, operations, moderations);

        result.Operations.ShouldAllBe(e => e.Status == RestoredFinanceEntryStatus.Inserted);
        result.Operations[0].Id.ShouldBe(lostOperationId);
        var newOperationId = result.Operations[1].Id!.Value;
        newOperationId.ShouldBe(identityBefore + 1);
        result.Moderations.ShouldAllBe(e => e.Status == RestoredFinanceEntryStatus.Inserted);

        // 5. Строки — как у доменных; прежний id сохранён, счётчик сдвинули только записи без id
        DateTime claimUpdatedAt;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            var restoredComment = await GetCommentAsync(db, lostOperationId);
            restoredComment.CreatedTime.ShouldBe(MasterAcceptedAt.UtcDateTime);
            Comparable(restoredComment).ShouldBe(Comparable(approvedReferenceComment));
            var restored = await GetFinanceAsync(db, lostOperationId);
            Comparable(restored).ShouldBe(Comparable(approvedReference));
            restored.Created.ShouldBe(MasterAcceptedAt.UtcDateTime);
            restored.Changed.ShouldBe(MasterAcceptedAt.UtcDateTime);
            // Дата операции — как у формы приёма взноса: полночь дня, день — в часовом поясе проекта.
            restored.OperationDate.ShouldBe(new DateTime(2026, 7, 2));

            var restoredByPlayerComment = await GetCommentAsync(db, newOperationId);
            Comparable(restoredByPlayerComment).ShouldBe(Comparable(playerReferenceComment));
            var restoredByPlayer = await GetFinanceAsync(db, newOperationId);
            Comparable(restoredByPlayer).ShouldBe(Comparable(playerReference));
            restoredByPlayer.Changed.ShouldBe(ModeratedAt.UtcDateTime);

            var restoredModeration = await GetCommentAsync(db, lostModerationId);
            restoredModeration.ParentCommentId.ShouldBe(newOperationId);
            moderationReferenceComment.ParentCommentId.ShouldBe(playerReferenceId);
            (Comparable(restoredModeration) with { ParentCommentId = null })
                .ShouldBe(Comparable(moderationReferenceComment) with { ParentCommentId = null });

            var waiting = await GetFinanceAsync(db, waitingId);
            waiting.State.ShouldBe((int)FinanceOperationState.Declined);
            waiting.Changed.ShouldBe(DeclinedAt.UtcDateTime);
            var decline = await db.Set<Comment>().AsNoTracking().SingleAsync(c => c.ParentCommentId == waitingId);
            decline.ExtraAction.ShouldBe(CommentExtraAction.RejectFinance);
            decline.CommentId.ShouldBe(identityBefore + 2);

            // Операция без id, решение без id и запись в истории.
            (await GetCommentsIdentityAsync(db)).ShouldBe(identityBefore + 3);

            var claim = await db.Set<Claim>().AsNoTracking().SingleAsync(c => c.ClaimId == claimId.ClaimId);
            claim.LastPlayerCommentAt.ShouldBe(PlayerPaidAt);
            claim.LastMasterCommentAt.ShouldBe(DeclinedAt);
            claim.LastVisibleMasterCommentAt.ShouldBe(DeclinedAt);
            claim.LastVisibleMasterCommentBy_Id.ShouldBe(masterId.Value);
            claim.LastUpdateDateTime.ShouldBeGreaterThanOrEqualTo(before);
            claimUpdatedAt = claim.LastUpdateDateTime;

            var history = await db.Set<Comment>().AsNoTracking()
                .Where(c => c.CommentDiscussionId == claim.CommentDiscussionId && c.ExtraAction == CommentExtraAction.RestoredFromNotifications)
                .Select(c => new { c.IsVisibleToPlayer, c.CommentText.Text.Contents })
                .ToListAsync();
            var single = history.ShouldHaveSingleItem();
            single.IsVisibleToPlayer.ShouldBeFalse();
            single.Contents.ShouldBe("Восстановлено финансовых операций из уведомлений: 2, решений по ним: 2");
        }

        // Дата последнего обновления проекта — сейчас, хотя операции исторические (ADR023).
        (await GetProjectLastUpdatedAsync(masterId, projectId)).ShouldBeGreaterThanOrEqualTo(DateOnly.FromDateTime(before));

        // 6. Повтор — ничего не вставляет, в историю не пишет, заявку не трогает
        var repeat = await Restore(masterId, claimId, operations, moderations);
        repeat.Operations.ShouldAllBe(e => e.Status == RestoredFinanceEntryStatus.AlreadyExisted);
        repeat.Operations.Select(e => e.Id).ShouldBe([lostOperationId, newOperationId]);
        repeat.Moderations.ShouldAllBe(e => e.Status == RestoredFinanceEntryStatus.AlreadyExisted);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            (await db.Set<Claim>().SingleAsync(c => c.ClaimId == claimId.ClaimId)).LastUpdateDateTime.ShouldBe(claimUpdatedAt);
            (await GetCommentsIdentityAsync(db)).ShouldBe(identityBefore + 3);
            (await db.Set<FinanceOperation>().CountAsync(f => f.ClaimId == claimId.ClaimId)).ShouldBe(5);
        }

        // 7. Конфликты: id комментария чужой заявки, id комментария этой заявки без операции, id счётчика —
        // пропущены вместе с решением по ним, счётчик не сдвинут
        int foreignCommentId, plainCommentId, identity;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            var otherDiscussionId = await GetDiscussionIdAsync(db, otherClaimId);
            foreignCommentId = await db.Set<Comment>().Where(c => c.CommentDiscussionId == otherDiscussionId).MinAsync(c => c.CommentId);
            var discussionId = await GetDiscussionIdAsync(db, claimId);
            // Комментарий подачи заявки — операции у него нет.
            plainCommentId = await db.Set<Comment>().Where(c => c.CommentDiscussionId == discussionId).MinAsync(c => c.CommentId);
            (await db.Set<FinanceOperation>().AnyAsync(f => f.CommentId == plainCommentId)).ShouldBeFalse();
            // Граница: значение счётчика выдано, но откачено — строки нет, и всё же это не потерянный id.
            _ = await BurnTwoCommentIdsAsync(db, projectId, discussionId, masterId);
            identity = await GetCommentsIdentityAsync(db);
            (await db.Set<Comment>().AnyAsync(c => c.CommentId == identity)).ShouldBeFalse();
        }
        RestoredFinanceOperation Conflicting(int id) => operations[0] with { OperationId = id, CreatedAt = PlayerPaidAt, ChangedAt = PlayerPaidAt };
        var conflicts = await Restore(masterId, claimId,
            [Conflicting(foreignCommentId), Conflicting(plainCommentId), Conflicting(identity)],
            [new(null, masterId, Approved: true, ModeratedAt, ApproveText, ParentOperationId: foreignCommentId, ParentRestoredIndex: null)]);
        conflicts.Operations.Select(e => (e.Status, e.Id)).ShouldBe(
        [
            (RestoredFinanceEntryStatus.TakenElsewhere, foreignCommentId),
            (RestoredFinanceEntryStatus.TakenElsewhere, plainCommentId),
            (RestoredFinanceEntryStatus.NotLostId, identity),
        ]);
        conflicts.Moderations.ShouldBe([new RestoredFinanceEntry(RestoredFinanceEntryStatus.ParentNotRestored, null)]);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            (await GetCommentsIdentityAsync(db)).ShouldBe(identity);
            (await db.Set<FinanceOperation>().AnyAsync(f => f.CommentId == foreignCommentId || f.CommentId == plainCommentId || f.CommentId == identity))
                .ShouldBeFalse();
        }

        // 8. На уровне репозитория: без id повтор узнаётся по автору, виду и сумме в пределах минуты
        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var repository = sp.GetRequiredService<IUnitOfWork>().GetCharacterAggregateWriteRepository();
            var near = MasterAcceptedAt.UtcDateTime.AddSeconds(40);
            Task<RestoredFinanceOperationInsert> Insert(int money) => repository.InsertRestoredFinanceOperation(new RestoredFinanceOperationRow(
                claimId, null, masterId, false, FinanceOperationType.Submit, money, cashId, FinanceOperationState.Approved,
                near, near, new DateTime(2026, 7, 2), "Другой текст"));

            (await Insert(1000)).ShouldBe(new RestoredFinanceOperationInsert(RestoredCommentInsertResult.AlreadyExists, lostOperationId));
        });

        // 8а. Решение, не связанное с операцией, — комментарий без родителя; повтор узнаёт его
        var unlinkedAt = DeclinedAt.AddHours(1);
        RestoredFinanceModeration[] unlinked =
            [new(null, masterId, Approved: true, unlinkedAt, "Без операции", ParentOperationId: null, ParentRestoredIndex: null)];
        (await Restore(masterId, claimId, [], unlinked)).Moderations.ShouldHaveSingleItem().Status
            .ShouldBe(RestoredFinanceEntryStatus.Inserted);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            var discussionId = await GetDiscussionIdAsync(db, claimId);
            var comment = await db.Set<Comment>().AsNoTracking()
                .SingleAsync(c => c.CommentDiscussionId == discussionId && c.CommentText.Text.Contents == "Без операции");
            comment.ParentCommentId.ShouldBeNull();
            comment.ExtraAction.ShouldBe(CommentExtraAction.ApproveFinance);
            comment.CreatedAt.ShouldBe(unlinkedAt.UtcDateTime);
        }
        (await Restore(masterId, claimId, [], unlinked)).Moderations.ShouldHaveSingleItem().Status
            .ShouldBe(RestoredFinanceEntryStatus.AlreadyExisted);

        // 9. Обрыв: прошлый вызов вставил операцию и упал до операции над заявкой. Повтор ничего не
        // вставляет и в историю не пишет, но дату обновления проекта поднимает.
        using (var scope = factory.Services.CreateScope())
        {
            _ = await BackdateProjectAsync(scope.ServiceProvider.GetRequiredService<MyDbContext>(), projectId);
        }
        RestoredFinanceOperation interrupted = new(null, otherPlayerId, IsCommentByPlayer: true, FinanceOperationType.Submit, 700, cashId,
            FinanceOperationState.Proposed, PlayerPaidAt, PlayerPaidAt, PlayerText);
        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var repository = sp.GetRequiredService<IUnitOfWork>().GetCharacterAggregateWriteRepository();
            (await repository.InsertRestoredFinanceOperation(new RestoredFinanceOperationRow(
                otherClaimId, null, otherPlayerId, true, FinanceOperationType.Submit, 700, cashId, FinanceOperationState.Proposed,
                PlayerPaidAt.UtcDateTime, PlayerPaidAt.UtcDateTime, new DateTime(2026, 7, 5), PlayerText)))
                .Result.ShouldBe(RestoredCommentInsertResult.Inserted);
        });
        // Сама вставленная операция поднимает дату только до своей исторической.
        (await GetProjectLastUpdatedAsync(masterId, projectId)).ShouldBe(DateOnly.FromDateTime(PlayerPaidAt.UtcDateTime));

        var resumeBefore = DateTime.UtcNow.AddSeconds(-1);
        var resumed = await Restore(masterId, otherClaimId, [interrupted], []);
        resumed.Operations.ShouldHaveSingleItem().Status.ShouldBe(RestoredFinanceEntryStatus.AlreadyExisted);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            var otherClaim = await db.Set<Claim>().AsNoTracking().SingleAsync(c => c.ClaimId == otherClaimId.ClaimId);
            (await db.Set<Comment>().AnyAsync(c => c.CommentDiscussionId == otherClaim.CommentDiscussionId
                && c.ExtraAction == CommentExtraAction.RestoredFromNotifications)).ShouldBeFalse();
            otherClaim.LastPlayerCommentAt.ShouldBe(PlayerPaidAt);
            otherClaim.LastUpdateDateTime.ShouldBeGreaterThanOrEqualTo(resumeBefore);
        }
        (await GetProjectLastUpdatedAsync(masterId, projectId)).ShouldBeGreaterThanOrEqualTo(DateOnly.FromDateTime(resumeBefore));
    }

    [Fact]
    public async Task CreateRestoredCashPaymentType_InBlockedProject_CreatesOnce()
    {
        UserIdentification masterId;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId);
        }
        await BlockProject(projectId);

        var created = await factory.Services.RunAsAsync(masterId, sp =>
            sp.GetRequiredService<IProjectFinanceSettingsService>().CreateRestoredCashPaymentType(projectId, masterId));
        var again = await factory.Services.RunAsAsync(masterId, sp =>
            sp.GetRequiredService<IProjectFinanceSettingsService>().CreateRestoredCashPaymentType(projectId, masterId));

        created.ProjectId.ShouldBe(projectId);
        created.PaymentTypeId.ShouldBeGreaterThan(0);
        again.ShouldBe(created);
        var projectInfo = await factory.Services.RunAsAsync(masterId, sp =>
            sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId));
        var paymentType = projectInfo.ProjectFinanceSettings.PaymentTypes.ShouldHaveSingleItem();
        paymentType.PaymentTypeId.ShouldBe(created);
        paymentType.TypeKind.ShouldBe(PaymentTypeKind.Cash);
        paymentType.User.UserId.ShouldBe(masterId);
    }

    private async Task<(UserIdentification MasterId, ProjectIdentification ProjectId, PaymentTypeIdentification CashId)> CreateProjectWithCash()
    {
        UserIdentification masterId;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId);
        }

        var cashId = await factory.Services.RunAsAsync(masterId, async sp =>
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
        return (masterId, projectId, cashId);
    }

    private Task AcceptFee(UserIdentification userId, ClaimIdentification claimId, PaymentTypeIdentification cashId, int money, string text)
        => factory.Services.RunAsAsync(userId, sp => sp.GetRequiredService<IFinanceService>().FeeAcceptedOperation(new FeeAcceptedOperationRequest
        {
            ClaimId = claimId.ClaimId,
            Contents = text,
            OperationDate = DateTime.UtcNow.Date,
            Money = money,
            PaymentTypeId = cashId,
        }));

    private async Task<(int First, int Second)> GetTwoOperationIds(ClaimIdentification claimId)
    {
        using var scope = factory.Services.CreateScope();
        var ids = await scope.ServiceProvider.GetRequiredService<MyDbContext>().Set<FinanceOperation>()
            .Where(f => f.ClaimId == claimId.ClaimId).OrderBy(f => f.CommentId).Select(f => f.CommentId).ToListAsync();
        ids.Count.ShouldBe(2);
        return (ids[0], ids[1]);
    }

    private Task<RestoredFinanceResult> Restore(
        UserIdentification masterId,
        ClaimIdentification claimId,
        IReadOnlyList<RestoredFinanceOperation> operations,
        IReadOnlyList<RestoredFinanceModeration> moderations)
        => factory.Services.RunAsAsync(masterId, sp =>
            sp.GetRequiredService<IClaimRestoreService>().RestoreFinanceOperations(claimId, operations, moderations));

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

    /// <summary>
    /// Сервиса блокировки пока нет — ставим флаг прямо в БД (как в <see cref="BlockedProjectScenario"/>).
    /// </summary>
    private async Task BlockProject(ProjectIdentification projectId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        _ = await db.Database.ExecuteSqlCommandAsync(
            "UPDATE dbo.Projects SET IsBlocked = 1 WHERE ProjectId = @p0",
            projectId.Value);
    }

    /// <summary>
    /// Дата последнего обновления проекта — та, что считает <c>GetProjectWithLastUpdateQuery</c>.
    /// </summary>
    private Task<DateOnly> GetProjectLastUpdatedAsync(UserIdentification masterId, ProjectIdentification projectId)
        => factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projects = await sp.GetRequiredService<IUnitOfWork>().GetProjectRepository()
                .GetProjectsBySpecification(ProjectListSpecification.AllProjectsWithMasterAccess(masterId));
            return projects.Single(p => p.ProjectId == projectId).LastUpdatedAt;
        });

    /// <summary>
    /// Все метки, из которых складывается дата последнего обновления проекта, — на дату бэкапа: так
    /// выглядит проект, восстановленный из майского бэкапа.
    /// </summary>
    private static Task<int> BackdateProjectAsync(MyDbContext db, ProjectIdentification projectId)
        => db.Database.ExecuteSqlCommandAsync(
            """
            UPDATE dbo.Comments SET CreatedTime = @p1, LastEditTime = @p1 WHERE ProjectId = @p0;
            UPDATE dbo.Characters SET UpdatedAt = @p1 WHERE ProjectId = @p0;
            UPDATE dbo.CharacterGroups SET UpdatedAt = @p1 WHERE ProjectId = @p0;
            UPDATE dbo.PlotFolders SET ModifiedDateTime = @p1 WHERE ProjectId = @p0;
            UPDATE dbo.PlotElements SET ModifiedDateTime = @p1 WHERE ProjectId = @p0;
            UPDATE dbo.Claims SET LastUpdateDateTime = @p1, LastPlayerCommentAt = @p1, LastMasterCommentAt = @p1,
                LastVisibleMasterCommentAt = @p1
            WHERE ProjectId = @p0;
            """,
            projectId.Value,
            BackupAt);

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

    private static Task<CommentRow> GetCommentAsync(MyDbContext db, int commentId)
        => db.Database.SqlQuery<CommentRow>(
                """
                SELECT c.ProjectId, c.CommentDiscussionId, c.ParentCommentId, c.AuthorUserId, c.IsCommentByPlayer,
                    c.IsVisibleToPlayer, c.ExtraAction, c.CreatedTime, c.LastEditTime, t.Text_Contents AS Text
                FROM dbo.Comments c
                JOIN dbo.CommentTexts t ON t.CommentId = c.CommentId
                WHERE c.CommentId = @p0
                """,
                commentId)
            .SingleAsync();

    private static Task<FinanceRow> GetFinanceAsync(MyDbContext db, int operationId)
        => db.Database.SqlQuery<FinanceRow>(
                """
                SELECT ProjectId, ClaimId, MoneyAmount, PaymentTypeId, Created, Changed, OperationDate, State, OperationType,
                    LinkedClaimId, RefundedOperationId, RecurrentPaymentId, ReccurrentPaymentInstanceToken
                FROM dbo.FinanceOperations
                WHERE CommentId = @p0
                """,
                operationId)
            .SingleAsync();

    /// <summary>Значимые колонки комментария — без id и дат (у эталона «сейчас», у восстановленного — из уведомления).</summary>
    private static ComparableComment Comparable(CommentRow row) => new(
        row.ProjectId,
        row.CommentDiscussionId,
        row.ParentCommentId,
        row.AuthorUserId,
        row.IsCommentByPlayer,
        row.IsVisibleToPlayer,
        row.ExtraAction,
        row.Text,
        EditedAtCreation: row.CreatedTime == row.LastEditTime);

    private sealed record ComparableComment(
        int ProjectId,
        int CommentDiscussionId,
        int? ParentCommentId,
        int AuthorUserId,
        bool IsCommentByPlayer,
        bool IsVisibleToPlayer,
        int? ExtraAction,
        string? Text,
        bool EditedAtCreation);

    /// <summary>
    /// Значимые колонки операции — без дат (у эталона «сейчас», у восстановленного — из уведомления), но с
    /// их соотношениями: дата операции — полночь, время изменения — равно времени создания или нет.
    /// </summary>
    private static object Comparable(FinanceRow row) => new
    {
        row.ProjectId,
        row.ClaimId,
        row.MoneyAmount,
        row.PaymentTypeId,
        row.State,
        row.OperationType,
        row.LinkedClaimId,
        row.RefundedOperationId,
        row.RecurrentPaymentId,
        row.ReccurrentPaymentInstanceToken,
        OperationDateIsMidnight = row.OperationDate.TimeOfDay == TimeSpan.Zero,
        ChangedAtCreation = row.Created == row.Changed,
    };

    private sealed class CommentRow
    {
        public int ProjectId { get; set; }
        public int CommentDiscussionId { get; set; }
        public int? ParentCommentId { get; set; }
        public int AuthorUserId { get; set; }
        public bool IsCommentByPlayer { get; set; }
        public bool IsVisibleToPlayer { get; set; }
        public int? ExtraAction { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime LastEditTime { get; set; }
        public string? Text { get; set; }
    }

    private sealed class FinanceRow
    {
        public int ProjectId { get; set; }
        public int ClaimId { get; set; }
        public int MoneyAmount { get; set; }
        public int? PaymentTypeId { get; set; }
        public DateTime Created { get; set; }
        public DateTime Changed { get; set; }
        public DateTime OperationDate { get; set; }
        public int State { get; set; }
        public int OperationType { get; set; }
        public int? LinkedClaimId { get; set; }
        public int? RefundedOperationId { get; set; }
        public int? RecurrentPaymentId { get; set; }
        public string? ReccurrentPaymentInstanceToken { get; set; }
    }
}
