using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.Claims;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Services.Interfaces.Drp;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;
using JoinRpg.WebPortal.Managers.Drp;
using Microsoft.Extensions.Options;

namespace JoinRpg.WebPortal.Managers.Test.Drp;

public class FinanceRestoreViewServiceTest
{
    private static readonly DateTimeOffset BackupTakenAt = new(2026, 5, 8, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset LostAt = new(2026, 10, 7, 22, 30, 49, TimeSpan.Zero);
    private const string CharacterName = "Герой";

    private readonly MockedProject mock = new();
    private readonly FakeClaimRestoreService restoreService = new();
    private readonly FakeFinanceSettingsService financeSettings = new();
    private readonly FakeNotificationRepository notifications = new();
    private readonly FakeLookupRepository lookup;
    private readonly FakeClaimsRepository claims;

    public FinanceRestoreViewServiceTest()
    {
        lookup = new FakeLookupRepository(mock);
        claims = new FakeClaimsRepository(mock);
    }

    private ProjectIdentification ProjectId => mock.ProjectInfo.ProjectId;

    private UserIdentification Master => new(mock.Master.UserId);

    private ClaimIdentification Claim(int id) => new(ProjectId, id);

    /// <summary>Время уведомления: 10:00 UTC, в Москве (часовой пояс проекта) — 13:00.</summary>
    private static DateTimeOffset Day(int day) => new(2026, 7, day, 10, 0, 0, TimeSpan.Zero);

    private FinanceRestoreViewService CreateService(int userId) => new(
        new FakeProjectMetadataRepository(mock),
        new FakeCurrentUserAccessor(userId),
        notifications,
        new FakeUserRepository(mock),
        claims,
        restoreService,
        financeSettings,
        lookup,
        Options.Create(new DrpOptions { BackupTakenAt = BackupTakenAt, LostAt = LostAt }));

    private void BlockProject()
    {
        mock.Project.IsBlocked = true;
        mock.ReInitProjectInfo();
    }

    private PaymentType AddPaymentType(PaymentTypeKind kind, User owner, bool isActive = true, string? name = null)
    {
        var paymentType = new PaymentType(kind, mock.Project.ProjectId, owner.UserId)
        {
            PaymentTypeId = mock.Project.PaymentTypes.Count + 1,
            Project = mock.Project,
            User = owner,
            Operations = [],
            IsActive = isActive,
        };
        paymentType.Name = name ?? paymentType.Name;
        mock.Project.PaymentTypes.Add(paymentType);
        mock.ReInitProjectInfo();
        return paymentType;
    }

    private void ClaimInDb(int claimId)
    {
        lookup.Claims.Add(Claim(claimId));
        claims.Headers.Add(new ClaimWithPlayer
        {
            ClaimId = Claim(claimId),
            CharacterName = CharacterName,
            Player = new UserInfoHeader(new UserIdentification(mock.Player.UserId), new UserDisplayName(mock.Player.PrefferedName!, null)),
            ExtraNicknames = null,
            ResponsibleMasterUserId = Master,
            CharacterId = new CharacterIdentification(ProjectId, 1),
        });
    }

    /// <summary>
    /// Финансовое уведомление, как его пишет <c>ClaimNotificationTextBuilder</c>: по строке на получателя.
    /// Без номера — ссылка на заявку (так было до 2026-06-06).
    /// </summary>
    private void Notify(ClaimIdentification claimId, int? commentId, DateTimeOffset at, string actionName, User initiator, int? money, params User[] recipients)
    {
        var byPlayer = initiator.UserId == mock.Player.UserId;
        var header = $"Игра: {CharacterName}, игрок {mock.Player.PrefferedName}";
        var claimLine = $"Заявка {CharacterName} игрока {mock.Player.PrefferedName} отмечено {(byPlayer ? "игроком" : $"мастером {initiator.PrefferedName}")}";
        var moneyLine = money is { } value ? $"Сумма операции: {value}\n\n" : "";
        foreach (var recipient in recipients)
        {
            var body = $"Добрый день, {recipient.PrefferedName}\n{claimLine}\n\n**{actionName}**\n\n{moneyLine}Текст";
            notifications.AddClaimNotification(new NotificationMessageForRecipient(
                new MarkdownString(body), new UserIdentification(initiator.UserId), header, new UserIdentification(recipient.UserId),
                commentId is { } id ? new ClaimCommentIdentification(claimId, id) : claimId, at));
        }
    }

    private void MasterPaid(ClaimIdentification claimId, int? commentId, DateTimeOffset at, int money)
        => Notify(claimId, commentId, at, "Оплачен взнос", mock.Master, money, mock.Player);

    private void PlayerPaid(ClaimIdentification claimId, int? commentId, DateTimeOffset at, int money)
        => Notify(claimId, commentId, at, "Оплачен взнос", mock.Player, money, mock.Master);

    private void MasterApproved(ClaimIdentification claimId, int? commentId, DateTimeOffset at)
        => Notify(claimId, commentId, at, "Финансовая операция подтверждена", mock.Master, money: null, mock.Player);

    [Fact]
    public async Task GetPlan_ProjectNotBlocked_ExplainsWithoutReadingNotifications()
    {
        var model = await CreateService(mock.Master.UserId).GetPlan(ProjectId);

        model.UnavailableReason.ShouldBe(ClaimRestoreUnavailableReason.ProjectNotBlocked);
        model.Rows.ShouldBeEmpty();
        notifications.GetClaimNotificationsForProjectCalls.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPlan_MasterWithoutMoneyPermission_NoAccess()
    {
        var master = mock.CreateMaster();
        mock.Project.ProjectAcls.Single(a => a.UserId == master.UserId).CanManageMoney = false;
        BlockProject();

        var model = await CreateService(master.UserId).GetPlan(ProjectId);

        model.UnavailableReason.ShouldBe(ClaimRestoreUnavailableReason.NoAccess);
        notifications.GetClaimNotificationsForProjectCalls.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPlan_BuildsRowsPerClaim_WithSuggestedType()
    {
        var cash = AddPaymentType(PaymentTypeKind.Cash, mock.Master);
        _ = AddPaymentType(PaymentTypeKind.Online, mock.Master);
        BlockProject();
        ClaimInDb(5);
        MasterPaid(Claim(5), 555, Day(1), 500);
        MasterPaid(Claim(5), 556, Day(2), 700);
        lookup.Operations.Add(new ClaimFinanceOperationForRestore(556, Claim(5), FinanceOperationType.Submit, FinanceOperationState.Approved, 700, Day(2)));
        MasterPaid(Claim(6), 600, Day(3), 900);

        var model = await CreateService(mock.Master.UserId).GetPlan(ProjectId);

        model.UnavailableReason.ShouldBeNull();
        notifications.GetClaimNotificationsForProjectCalls.ShouldHaveSingleItem().ShouldBe((ProjectId, LostAt));
        lookup.NotifiedCommentIds.ShouldBe([555, 556, 600], ignoreOrder: true);
        model.HasOwnCash.ShouldBeTrue();
        // Онлайн-тип в выбор не попадает.
        var paymentType = model.PaymentTypes.ShouldHaveSingleItem();
        paymentType.PaymentTypeId.ShouldBe(cash.PaymentTypeId);
        paymentType.OwnerName.ShouldContain(mock.Master.PrefferedName!);
        paymentType.IsActive.ShouldBeTrue();
        model.Rows.Select(r => r.ClaimId.ClaimId).ShouldBe([5, 6]);

        var row = model.Rows[0];
        row.ClaimExists.ShouldBeTrue();
        row.CharacterName.ShouldBe(CharacterName);
        row.PlayerName.ShouldBe(mock.Player.PrefferedName);
        row.AlreadyExisting.ShouldBe(1);
        row.Warnings.ShouldBeEmpty();
        row.Operations.Count.ShouldBe(2);
        var operation = row.Operations[0];
        operation.ShouldBe(new FinanceRestoreOperationView("id:555", 555, IsRefund: false, 500, Day(1), operation.AuthorName, IsByPlayer: false,
            FinanceRestoreOperationStateView.Approved, cash.PaymentTypeId, [], CanApply: true, NotApplicableReason: null),
            new OperationViewComparer());
        operation.AuthorName.ShouldContain(mock.Master.PrefferedName!);
        var existing = row.Operations[1];
        existing.CanApply.ShouldBeFalse();
        existing.NotApplicableReason.ShouldBe("Уже есть в базе");

        var lost = model.Rows[1];
        lost.ClaimExists.ShouldBeFalse();
        lost.CharacterName.ShouldBeNull();
        lost.Operations.ShouldBeEmpty();
        lost.Warnings.ShouldBe([FinanceRestoreWarningView.ClaimNotFound]);
    }

    [Fact]
    public async Task GetPlan_DisabledSuggestedType_IsListed_OtherDisabledIsNot()
    {
        var cash = AddPaymentType(PaymentTypeKind.Cash, mock.Master, isActive: false);
        var otherMaster = mock.CreateMaster();
        _ = AddPaymentType(PaymentTypeKind.Custom, otherMaster, isActive: false, name: "Карта");
        BlockProject();
        ClaimInDb(5);
        MasterPaid(Claim(5), 555, Day(1), 500);

        var model = await CreateService(mock.Master.UserId).GetPlan(ProjectId);

        var paymentType = model.PaymentTypes.ShouldHaveSingleItem();
        paymentType.PaymentTypeId.ShouldBe(cash.PaymentTypeId);
        paymentType.IsActive.ShouldBeFalse();
        model.Rows.ShouldHaveSingleItem().Operations.ShouldHaveSingleItem().SuggestedPaymentTypeId.ShouldBe(cash.PaymentTypeId);
        // Выключенные «наличные» — тоже свои: создать их ещё раз нельзя.
        model.HasOwnCash.ShouldBeTrue();
    }

    [Fact]
    public async Task GetPlan_OnlyOtherMastersCash_NoOwnCash()
    {
        var otherMaster = mock.CreateMaster();
        var cash = AddPaymentType(PaymentTypeKind.Cash, otherMaster);
        BlockProject();
        ClaimInDb(5);
        MasterPaid(Claim(5), 555, Day(1), 500);

        var model = await CreateService(mock.Master.UserId).GetPlan(ProjectId);

        model.HasOwnCash.ShouldBeFalse();
        model.Rows.ShouldHaveSingleItem().Operations.ShouldHaveSingleItem().SuggestedPaymentTypeId.ShouldBe(cash.PaymentTypeId);
    }

    [Fact]
    public async Task GetPlan_NoManualTypes_CreateCashForCurrentMaster_NothingSuggested()
    {
        BlockProject();
        ClaimInDb(5);
        MasterPaid(Claim(5), 555, Day(1), 500);

        var model = await CreateService(mock.Master.UserId).GetPlan(ProjectId);

        model.HasOwnCash.ShouldBeFalse();
        model.PaymentTypes.ShouldBeEmpty();
        model.Rows.ShouldHaveSingleItem().Operations.ShouldHaveSingleItem().SuggestedPaymentTypeId.ShouldBeNull();
    }

    [Fact]
    public async Task GetPlan_UnapprovedRefund_NotApplicable()
    {
        _ = AddPaymentType(PaymentTypeKind.Cash, mock.Master);
        BlockProject();
        ClaimInDb(5);
        Notify(Claim(5), 555, Day(1), "Возвращен взнос", mock.Player, -200, mock.Master);

        var model = await CreateService(mock.Master.UserId).GetPlan(ProjectId);

        var operation = model.Rows.ShouldHaveSingleItem().Operations.ShouldHaveSingleItem();
        operation.IsRefund.ShouldBeTrue();
        operation.Money.ShouldBe(200);
        operation.State.ShouldBe(FinanceRestoreOperationStateView.Proposed);
        operation.CanApply.ShouldBeFalse();
        operation.NotApplicableReason.ShouldBe("Возврат не подтверждён мастером — внесите вручную");
    }

    [Fact]
    public async Task Apply_RecomputesPlanAndSendsOnlyThisClaimWithChosenType()
    {
        var cash = AddPaymentType(PaymentTypeKind.Cash, mock.Master);
        BlockProject();
        ClaimInDb(5);
        ClaimInDb(7);
        MasterPaid(Claim(5), 555, Day(1), 500);
        MasterPaid(Claim(7), 700, Day(2), 900);
        restoreService.Result = new RestoredFinanceResult([new RestoredFinanceEntry(RestoredFinanceEntryStatus.Inserted, 555)], []);

        var result = await CreateService(mock.Master.UserId).Apply(
            new FinanceRestoreApplyRequest(Claim(5), new Dictionary<string, int> { ["id:555"] = cash.PaymentTypeId }));

        result.ShouldBe(new FinanceRestoreApplyResult(1, 0, 0, []), new ApplyResultComparer());
        // Уведомления — только этой заявки.
        notifications.GetClaimNotificationsForProjectCalls.ShouldBeEmpty();
        notifications.GetClaimNotificationsForClaimCalls.ShouldHaveSingleItem().ShouldBe((Claim(5), LostAt));
        lookup.NotifiedCommentIds.ShouldBe([555]);
        var (claimId, operations, moderations) = restoreService.Calls.ShouldHaveSingleItem();
        claimId.ShouldBe(Claim(5));
        moderations.ShouldBeEmpty();
        operations.ShouldBe([
            new RestoredFinanceOperation(555, Master, IsCommentByPlayer: false, FinanceOperationType.Submit, 500,
                new PaymentTypeIdentification(ProjectId, cash.PaymentTypeId), FinanceOperationState.Approved, Day(1), Day(1), "Текст"),
        ]);
    }

    [Fact]
    public async Task Apply_IdTakenByOtherClaimOfProject_NotSent()
    {
        var cash = AddPaymentType(PaymentTypeKind.Cash, mock.Master);
        BlockProject();
        ClaimInDb(5);
        ClaimInDb(7);
        MasterPaid(Claim(5), 555, Day(1), 500);
        lookup.Operations.Add(new ClaimFinanceOperationForRestore(555, Claim(7), FinanceOperationType.Submit, FinanceOperationState.Approved, 100, Day(1)));

        var result = await CreateService(mock.Master.UserId).Apply(
            new FinanceRestoreApplyRequest(Claim(5), new Dictionary<string, int> { ["id:555"] = cash.PaymentTypeId }));

        result.Inserted.ShouldBe(0);
        restoreService.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task Apply_OnlineType_Refuses()
    {
        _ = AddPaymentType(PaymentTypeKind.Cash, mock.Master);
        var online = AddPaymentType(PaymentTypeKind.Online, mock.Master);
        BlockProject();
        ClaimInDb(5);
        MasterPaid(Claim(5), 555, Day(1), 500);

        _ = await Should.ThrowAsync<JoinValidationException>(() => CreateService(mock.Master.UserId).Apply(
            new FinanceRestoreApplyRequest(Claim(5), new Dictionary<string, int> { ["id:555"] = online.PaymentTypeId })));

        restoreService.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task Apply_TypeNotOfThisProject_Refuses()
    {
        _ = AddPaymentType(PaymentTypeKind.Cash, mock.Master);
        BlockProject();
        ClaimInDb(5);
        MasterPaid(Claim(5), 555, Day(1), 500);

        _ = await Should.ThrowAsync<JoinValidationException>(() => CreateService(mock.Master.UserId).Apply(
            new FinanceRestoreApplyRequest(Claim(5), new Dictionary<string, int> { ["id:555"] = 999 })));

        restoreService.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task Apply_UnknownOperationKey_Refuses()
    {
        var cash = AddPaymentType(PaymentTypeKind.Cash, mock.Master);
        BlockProject();
        ClaimInDb(5);
        MasterPaid(Claim(5), 555, Day(1), 500);

        _ = await Should.ThrowAsync<JoinValidationException>(() => CreateService(mock.Master.UserId).Apply(
            new FinanceRestoreApplyRequest(Claim(5), new Dictionary<string, int> { ["id:556"] = cash.PaymentTypeId })));

        restoreService.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task Apply_OperationWithoutType_SkippedAndNamed_ItsModerationDeferred()
    {
        var cash = AddPaymentType(PaymentTypeKind.Cash, mock.Master);
        BlockProject();
        ClaimInDb(5);
        PlayerPaid(Claim(5), 600, Day(1), 300);
        MasterApproved(Claim(5), 601, Day(2));
        MasterPaid(Claim(5), 602, Day(3), 500);

        var result = await CreateService(mock.Master.UserId).Apply(
            new FinanceRestoreApplyRequest(Claim(5), new Dictionary<string, int> { ["id:602"] = cash.PaymentTypeId }));

        var (_, operations, moderations) = restoreService.Calls.ShouldHaveSingleItem();
        operations.Select(o => o.OperationId).ShouldBe([602]);
        moderations.ShouldBeEmpty();
        result.Skipped.ShouldBe([
            "оплата 300 от 01.07.2026 13:00: не выбран тип оплаты",
            "подтверждение от 02.07.2026 13:00: восстановится вместе со своей операцией, когда для неё выберут тип оплаты",
        ]);
    }

    [Fact]
    public async Task Apply_NoTypesChosen_RestoresOnlyModerations()
    {
        _ = AddPaymentType(PaymentTypeKind.Cash, mock.Master);
        BlockProject();
        ClaimInDb(5);
        // Заявленный платёж игрока есть в базе и ждёт решения; решение — только в уведомлениях.
        lookup.Operations.Add(new ClaimFinanceOperationForRestore(500, Claim(5), FinanceOperationType.Submit, FinanceOperationState.Proposed, 300, Day(1)));
        MasterApproved(Claim(5), 601, Day(2));
        MasterPaid(Claim(5), 602, Day(3), 500);

        var result = await CreateService(mock.Master.UserId).Apply(new FinanceRestoreApplyRequest(Claim(5), new Dictionary<string, int>()));

        var (_, operations, moderations) = restoreService.Calls.ShouldHaveSingleItem();
        operations.ShouldBeEmpty();
        var moderation = moderations.ShouldHaveSingleItem();
        moderation.CommentId.ShouldBe(601);
        moderation.ParentOperationId.ShouldBe(500);
        result.Skipped.ShouldBe(["оплата 500 от 03.07.2026 13:00: не выбран тип оплаты"]);
    }

    [Fact]
    public async Task Apply_UnapprovedRefund_NotSent()
    {
        var cash = AddPaymentType(PaymentTypeKind.Cash, mock.Master);
        BlockProject();
        ClaimInDb(5);
        Notify(Claim(5), 555, Day(1), "Возвращен взнос", mock.Player, -200, mock.Master);

        var result = await CreateService(mock.Master.UserId).Apply(
            new FinanceRestoreApplyRequest(Claim(5), new Dictionary<string, int> { ["id:555"] = cash.PaymentTypeId }));

        result.ShouldBe(new FinanceRestoreApplyResult(0, 0, 0, []), new ApplyResultComparer());
        restoreService.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task Apply_ClaimNotInDb_Refuses()
    {
        var cash = AddPaymentType(PaymentTypeKind.Cash, mock.Master);
        BlockProject();
        MasterPaid(Claim(6), 600, Day(1), 500);

        _ = await Should.ThrowAsync<JoinValidationException>(() => CreateService(mock.Master.UserId).Apply(
            new FinanceRestoreApplyRequest(Claim(6), new Dictionary<string, int> { ["id:600"] = cash.PaymentTypeId })));

        restoreService.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task Apply_NothingToRestore_DoesNotCallService()
    {
        _ = AddPaymentType(PaymentTypeKind.Cash, mock.Master);
        BlockProject();
        ClaimInDb(5);
        MasterPaid(Claim(5), 555, Day(1), 500);
        lookup.Operations.Add(new ClaimFinanceOperationForRestore(555, Claim(5), FinanceOperationType.Submit, FinanceOperationState.Approved, 500, Day(1)));

        var result = await CreateService(mock.Master.UserId).Apply(new FinanceRestoreApplyRequest(Claim(5), new Dictionary<string, int>()));

        result.ShouldBe(new FinanceRestoreApplyResult(0, 0, 0, []), new ApplyResultComparer());
        restoreService.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task Apply_ProjectNotBlocked_RefusesWithoutReadingNotifications()
    {
        _ = await Should.ThrowAsync<JoinValidationException>(() => CreateService(mock.Master.UserId).Apply(
            new FinanceRestoreApplyRequest(Claim(5), new Dictionary<string, int>())));

        notifications.GetClaimNotificationsForClaimCalls.ShouldBeEmpty();
    }

    [Fact]
    public async Task Apply_MasterWithoutMoneyPermission_Refuses()
    {
        var master = mock.CreateMaster();
        mock.Project.ProjectAcls.Single(a => a.UserId == master.UserId).CanManageMoney = false;
        BlockProject();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(() => CreateService(master.UserId).Apply(
            new FinanceRestoreApplyRequest(Claim(5), new Dictionary<string, int>())));

        notifications.GetClaimNotificationsForClaimCalls.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateOwnCash_CreatesForCurrentUser()
    {
        BlockProject();

        await CreateService(mock.Master.UserId).CreateOwnCash(ProjectId);

        financeSettings.CreateRestoredCashCalls.ShouldBe([(ProjectId, Master)]);
    }

    /// <summary>Модели ездят между сервером и островом по JSON (у мок-проекта id нулевой, поэтому свои id).</summary>
    [Fact]
    public void Models_SurviveJson()
    {
        var projectId = new ProjectIdentification(1);
        var operation = new FinanceRestoreOperationView("id:555", 555, false, 500, BackupTakenAt, "Мастер", false,
            FinanceRestoreOperationStateView.Proposed, 3, [3, 4], true, null);
        var moderation = new FinanceRestoreModerationView("id:556", 556, true, BackupTakenAt, "Мастер", true, "Пояснение");
        var row = new FinanceRestoreRowViewModel(new ClaimIdentification(projectId, 5), true, CharacterName, "Игрок", [operation], [moderation],
            1, 2, 3, 4, 5, [FinanceRestoreWarningView.IdConflict]);
        var plan = new FinanceRestorePlanViewModel(projectId, null, [new FinanceRestorePaymentTypeView(3, "Наличные", "Мастер", false)], [row], true);

        var restored = JsonRoundTrip.Ensure(plan);
        var restoredRow = restored.Rows.ShouldHaveSingleItem();
        restoredRow.ClaimId.ShouldBe(row.ClaimId);
        restoredRow.Operations.ShouldHaveSingleItem().CandidatePaymentTypeIds.ShouldBe([3, 4]);
        restoredRow.Moderations.ShouldHaveSingleItem().ShouldBe(moderation);
        restoredRow.Warnings.ShouldBe([FinanceRestoreWarningView.IdConflict]);
        restored.PaymentTypes.ShouldHaveSingleItem().ShouldBe(plan.PaymentTypes[0]);

        var request = new FinanceRestoreApplyRequest(row.ClaimId, new Dictionary<string, int> { ["id:555"] = 3 });
        JsonRoundTrip.Ensure(request).PaymentTypes.ShouldBe(request.PaymentTypes);
        JsonRoundTrip.Ensure(new FinanceRestoreApplyResult(1, 2, 3, ["текст"])).Skipped.ShouldBe(["текст"]);
    }

    /// <summary>Записи со списками сравниваются по ссылке на список — сравниваем по содержимому.</summary>
    private sealed class OperationViewComparer : IEqualityComparer<FinanceRestoreOperationView>
    {
        public bool Equals(FinanceRestoreOperationView? x, FinanceRestoreOperationView? y)
            => x is not null && y is not null
            && x with { CandidatePaymentTypeIds = [] } == y with { CandidatePaymentTypeIds = [] }
            && x.CandidatePaymentTypeIds.SequenceEqual(y.CandidatePaymentTypeIds);

        public int GetHashCode(FinanceRestoreOperationView obj) => obj.Key.GetHashCode();
    }

    private sealed class ApplyResultComparer : IEqualityComparer<FinanceRestoreApplyResult>
    {
        public bool Equals(FinanceRestoreApplyResult? x, FinanceRestoreApplyResult? y)
            => x is not null && y is not null
            && (x.Inserted, x.AlreadyExisted, x.Conflicts) == (y.Inserted, y.AlreadyExisted, y.Conflicts)
            && x.Skipped.SequenceEqual(y.Skipped);

        public int GetHashCode(FinanceRestoreApplyResult obj) => obj.Inserted;
    }

    private sealed class FakeLookupRepository(MockedProject mock) : IProjectRestoreLookupRepository
    {
        public List<ClaimIdentification> Claims { get; } = [];
        public List<ClaimFinanceOperationForRestore> Operations { get; } = [];
        public List<int> NotifiedCommentIds { get; } = [];

        /// <summary>Как в базе: заявки, операции и типы оплаты — всего проекта; типы — из метаданных мока.</summary>
        public Task<ClaimFinanceRestoreLookup> GetClaimFinanceForRestore(ProjectIdentification projectId, IReadOnlyCollection<int> notifiedCommentIds)
        {
            NotifiedCommentIds.AddRange(notifiedCommentIds);
            return Task.FromResult(new ClaimFinanceRestoreLookup(
                [.. Claims],
                [.. Operations],
                [],
                [],
                [.. mock.ProjectInfo.ProjectFinanceSettings.PaymentTypes.Select(p =>
                    new PaymentTypeForRestore(p.PaymentTypeId, p.TypeKind, p.User.UserId, p.Enabled, p.Name))],
                []));
        }

        public Task<ClaimCommentsRestoreLookup> GetClaimCommentsForRestore(
            ProjectIdentification projectId, IReadOnlyCollection<int> notifiedCommentIds, DateTimeOffset textsSince)
            => throw new NotSupportedException();

        public Task<ClaimCommentsRestoreLookup> GetClaimCommentsForRestore(
            ClaimIdentification claimId, IReadOnlyCollection<int> notifiedCommentIds, DateTimeOffset textsSince)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<CharacterGroupIdentification>> GetGroupsTakenByOtherProjects(IReadOnlyCollection<CharacterGroupIdentification> groupIds)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<CharacterIdentification>> GetCharactersTakenByOtherProjects(IReadOnlyCollection<CharacterIdentification> characterIds)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<CharacterGroupIdentification>> GetGroupsCreatedAfter(IReadOnlyCollection<CharacterGroupIdentification> groupIds, DateTimeOffset since)
            => throw new NotSupportedException();

        public Task<IReadOnlyDictionary<string, UserIdentification>> FindUsersByEmails(IReadOnlyCollection<string> emails)
            => throw new NotSupportedException();

        public Task<IReadOnlyDictionary<ClaimIdentification, DateTimeOffset>> GetClaimsCommentedNotByRestoreSince(ProjectIdentification projectId, DateTimeOffset since)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<ProjectIdentification>> GetBlockedProjects() => throw new NotSupportedException();
    }

    private sealed class FakeClaimRestoreService : IClaimRestoreService
    {
        public List<(ClaimIdentification ClaimId, IReadOnlyList<RestoredFinanceOperation> Operations, IReadOnlyList<RestoredFinanceModeration> Moderations)> Calls { get; } = [];

        /// <summary>null — всё вставлено.</summary>
        public RestoredFinanceResult? Result { get; set; }

        public Task<RestoredFinanceResult> RestoreFinanceOperations(ClaimIdentification claimId,
            IReadOnlyList<RestoredFinanceOperation> operations, IReadOnlyList<RestoredFinanceModeration> moderations)
        {
            Calls.Add((claimId, operations, moderations));
            return Task.FromResult(Result ?? new RestoredFinanceResult(
                [.. operations.Select(o => new RestoredFinanceEntry(RestoredFinanceEntryStatus.Inserted, o.OperationId))],
                [.. moderations.Select(m => new RestoredFinanceEntry(RestoredFinanceEntryStatus.Inserted, m.CommentId))]));
        }

        public Task<RestoredCommentsResult> RestoreClaimComments(ClaimIdentification claimId, IReadOnlyList<RestoredClaimComment> comments)
            => throw new NotSupportedException();

        public Task ApplyRestoredState(ClaimIdentification claimId, RestoredClaimTarget target) => throw new NotSupportedException();

        public Task<CharacterIdentification> RestoreLostClaim(LostClaimToRestore claim) => throw new NotSupportedException();
    }

    private sealed class FakeFinanceSettingsService : IProjectFinanceSettingsService
    {
        public List<(ProjectIdentification ProjectId, UserIdentification Owner)> CreateRestoredCashCalls { get; } = [];

        public Task<PaymentTypeIdentification> CreateRestoredCashPaymentType(ProjectIdentification projectId, UserIdentification owner)
        {
            CreateRestoredCashCalls.Add((projectId, owner));
            return Task.FromResult(new PaymentTypeIdentification(projectId, 1));
        }

        public Task CreatePaymentType(CreatePaymentTypeRequest request) => throw new NotSupportedException();

        public Task TogglePaymentActiveness(PaymentTypeIdentification paymentTypeId) => throw new NotSupportedException();

        public Task EditCustomPaymentType(PaymentTypeIdentification paymentTypeId, string name, bool isDefault) => throw new NotSupportedException();

        public Task CreateFeeSetting(CreateFeeSettingRequest request) => throw new NotSupportedException();

        public Task DeleteFeeSetting(ProjectIdentification projectId, int projectFeeSettingId) => throw new NotSupportedException();

        public Task SaveGlobalSettings(SetFinanceSettingsRequest request) => throw new NotSupportedException();
    }
}
