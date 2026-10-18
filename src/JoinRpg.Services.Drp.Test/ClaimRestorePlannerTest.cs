using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Drp.MasterExports;
using static JoinRpg.Services.Drp.Test.ClaimNotificationFactory;

namespace JoinRpg.Services.Drp.Test;

public class ClaimRestorePlannerTest
{
    private static readonly DateTimeOffset BackupAt = new(2026, 5, 8, 0, 0, 0, TimeSpan.Zero);
    private static readonly UserIdentification ProjectDefaultMaster = Master.UserId;

    private static readonly ExistingCharacterInfo Hamlet = Character(1, "Гамлет");
    private static readonly ExistingCharacterInfo Ophelia = Character(2, "Офелия");

    private static ExistingCharacterInfo Character(int id, string name, bool isActive = true, bool isSlot = false)
        => new(new CharacterIdentification(Project, id), name, isActive, isSlot, OtherMaster.UserId);

    private static ExistingClaimInfo ExistingClaim(ExistingCharacterInfo character, ClaimStatus status, int? fee = null)
        => new(Claim, character.CharacterId, Player.UserId, status, Master.UserId, fee);

    private static readonly ProjectInfo AnyProject = new MockedProject().ProjectInfo;

    /// <summary>
    /// Каждый сценарий заодно прогоняется через <see cref="ProjectRestorePlanner"/> без выгрузок: его строки
    /// заявок должны совпадать с <see cref="ClaimRestorePlanner"/> (#5529).
    /// </summary>
    private static IReadOnlyList<ClaimRestorePlanRow> Plan(
        IEnumerable<ExistingClaimInfo> claims,
        IEnumerable<ExistingCharacterInfo> characters,
        params IEnumerable<NotificationMessageForRecipient>[] rows)
    {
        var source = new ClaimRestoreSource(
            ClaimNotificationParser.Parse(rows.SelectMany(r => r)),
            [.. claims],
            [.. characters],
            [new ProjectMasterName(Master.UserId, Master.DisplayName.DisplayName), new ProjectMasterName(OtherMaster.UserId, OtherMaster.DisplayName.DisplayName)],
            ProjectDefaultMaster,
            BackupAt);
        var plan = ClaimRestorePlanner.Plan(source);

        var projectPlan = ProjectRestorePlanner.Plan(new ProjectRestoreSource(
            AnyProject, source, [], [], new Dictionary<string, UserIdentification>(), LostAt: BackupAt.AddYears(1)));
        projectPlan.Claims.Select(Projection).ShouldBe(plan.Select(Projection));

        return plan;
    }

    private static string Projection(ClaimRestorePlanRow r) => string.Join(" | ",
        r.ClaimId, r.Action, r.CharacterName, r.Character, string.Join(",", r.CharacterCandidates), r.Player,
        string.Join(",", r.PlayerCandidates), r.Status, r.StatusChangedAt, r.ResponsibleMaster, r.ManualFee,
        r.FirstActivity, r.LastActivity, string.Join(",", r.Warnings), r.ApplyByDefault);

    [Fact]
    public void LostClaimOnExistingCharacter()
    {
        var row = Plan([], [Hamlet, Ophelia],
            ClaimChange(Day(1), CommentExtraAction.NewClaim, Player, [Master], characterName: "Гамлет"),
            ClaimChange(Day(2), CommentExtraAction.ApproveByMaster, Master, [Player], characterName: "Гамлет"))
            .ShouldHaveSingleItem();

        row.Action.ShouldBe(ClaimRestoreAction.Create);
        row.ClaimId.ShouldBe(Claim);
        row.Character.ShouldBe(Hamlet.CharacterId);
        row.Player.ShouldBe(Player.UserId);
        row.Status.ShouldBe(ClaimStatus.Approved);
        // Смены ответственного не было — по правилам для этого персонажа.
        row.ResponsibleMaster.ShouldBe(Hamlet.DefaultResponsibleMaster);
        row.FirstActivity.ShouldBe(Day(1));
        row.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void LostClaimOnNewCharacter()
    {
        var row = Plan([], [Hamlet, Character(3, "Лаэрт", isActive: false)],
            ClaimChange(Day(1), CommentExtraAction.NewClaim, Player, [Master], characterName: "Лаэрт"))
            .ShouldHaveSingleItem();

        row.Action.ShouldBe(ClaimRestoreAction.Create);
        row.Character.ShouldBeNull();
        row.CharacterName.ShouldBe("Лаэрт");
        row.Status.ShouldBe(ClaimStatus.AddedByUser);
        row.ResponsibleMaster.ShouldBe(ProjectDefaultMaster);
    }

    [Fact]
    public void LostClaimOnAmbiguousCharacter()
    {
        var row = Plan([], [Hamlet, Character(4, "Гамлет")],
            ClaimChange(Day(1), CommentExtraAction.NewClaim, Player, [Master], characterName: "Гамлет"))
            .ShouldHaveSingleItem();

        row.Character.ShouldBeNull();
        row.Warnings.ShouldContain(ClaimRestoreWarning.CharacterAmbiguous);
    }

    [Fact]
    public void LostClaimWithoutStatusEvents()
    {
        var row = Plan([], [Hamlet],
            ClaimChange(Day(1), action: null, Master, [Player], characterName: "Гамлет"))
            .ShouldHaveSingleItem();

        row.Status.ShouldBeNull();
        row.Warnings.ShouldContain(ClaimRestoreWarning.StatusUnknown);
    }

    /// <summary>
    /// Задокументированное различие: <see cref="ClaimRestorePlanner.Plan(ClaimRestoreSource)"/> не знает момента
    /// аварии и правку после восстановления не распознаёт, а <see cref="ProjectRestorePlanner"/> — распознаёт.
    /// </summary>
    [Fact]
    public void OnlyProjectPlannerRecognizesChangeAfterLoss()
    {
        var lostAt = BackupAt.AddYears(1);
        var source = new ClaimRestoreSource(
            ClaimNotificationParser.Parse(ClaimChange(Day(1), CommentExtraAction.ApproveByMaster, Master, [Player], characterName: "Гамлет")),
            [ExistingClaim(Hamlet, ClaimStatus.OnHold) with { LastUpdated = lostAt.AddDays(1), LastChangedNotByRestore = lostAt.AddDays(1) }],
            [Hamlet],
            [new ProjectMasterName(Master.UserId, Master.DisplayName.DisplayName)],
            ProjectDefaultMaster,
            BackupAt);

        var claimRow = ClaimRestorePlanner.Plan(source).ShouldHaveSingleItem();
        claimRow.Action.ShouldBe(ClaimRestoreAction.Change);
        claimRow.ApplyByDefault.ShouldBeTrue();
        claimRow.Warnings.ShouldNotContain(ClaimRestoreWarning.ChangedAfterRestore);

        var projectRow = ProjectRestorePlanner.Plan(new ProjectRestoreSource(
            AnyProject, source, [], [], new Dictionary<string, UserIdentification>(), lostAt)).Claims.ShouldHaveSingleItem();
        projectRow.Action.ShouldBe(ClaimRestoreAction.Change);
        projectRow.Status.ShouldBe(claimRow.Status);
        projectRow.AttributesNotByDefault.ShouldBe([ClaimRestoreAttributeKind.Status]);
        projectRow.ApplyByDefault.ShouldBeFalse();
        projectRow.Warnings.ShouldContain(ClaimRestoreWarning.ChangedAfterRestore);
    }

    [Fact]
    public void ExistingClaimChangedAfterBackup()
    {
        var row = Plan([ExistingClaim(Hamlet, ClaimStatus.Discussed)], [Hamlet],
            ClaimChange(Day(1), CommentExtraAction.ApproveByMaster, Master, [Player], characterName: "Гамлет"),
            ClaimChange(Day(2), CommentExtraAction.ChangeResponsible, Master, [Player], characterName: "Гамлет",
                text: $"{Master.DisplayName.DisplayName} → {OtherMaster.DisplayName.DisplayName}"),
            ClaimChange(Day(3), CommentExtraAction.FeeChanged, Master, [Player], characterName: "Гамлет", text: "1500"))
            .ShouldHaveSingleItem();

        row.Action.ShouldBe(ClaimRestoreAction.Change);
        row.Character.ShouldBeNull();
        row.Status.ShouldBe(ClaimStatus.Approved);
        row.StatusChangedAt.ShouldBe(Day(1));
        row.ResponsibleMaster.ShouldBe(OtherMaster.UserId);
        row.ManualFee.ShouldBe(1500);
        row.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void ExistingClaimAlreadyRestored()
    {
        var row = Plan([ExistingClaim(Hamlet, ClaimStatus.Approved, fee: 1500)], [Hamlet],
            ClaimChange(Day(1), CommentExtraAction.ApproveByMaster, Master, [Player], characterName: "Гамлет"),
            ClaimChange(Day(3), CommentExtraAction.FeeChanged, Master, [Player], characterName: "Гамлет", text: "1500"))
            .ShouldHaveSingleItem();

        row.Action.ShouldBe(ClaimRestoreAction.Nothing);
        row.Status.ShouldBeNull();
        row.ManualFee.ShouldBeNull();
    }

    [Fact]
    public void ExistingClaimWithOnlyEventsBeforeBackupIsSkipped()
    {
        var beforeBackup = BackupAt.AddDays(-10);

        Plan([ExistingClaim(Hamlet, ClaimStatus.Approved)], [Hamlet],
            ClaimChange(beforeBackup, CommentExtraAction.ApproveByMaster, Master, [Player], characterName: "Гамлет"))
            .ShouldBeEmpty();
    }

    [Fact]
    public void ExistingClaimMovedToAnotherCharacter()
    {
        var row = Plan([ExistingClaim(Hamlet, ClaimStatus.Approved)], [Hamlet, Ophelia],
            ClaimChange(Day(1), CommentExtraAction.MoveByMaster, Master, [Player], characterName: "Офелия"))
            .ShouldHaveSingleItem();

        row.Action.ShouldBe(ClaimRestoreAction.Change);
        row.Character.ShouldBe(Ophelia.CharacterId);
    }

    [Fact]
    public void ExistingClaimMovedToUnknownCharacter()
    {
        var row = Plan([ExistingClaim(Hamlet, ClaimStatus.Approved)], [Hamlet],
            ClaimChange(Day(1), CommentExtraAction.MoveByMaster, Master, [Player], characterName: "Призрак"))
            .ShouldHaveSingleItem();

        row.Character.ShouldBeNull();
        row.Warnings.ShouldBe([ClaimRestoreWarning.MovedCharacterNotFound]);
    }

    [Fact]
    public void RenamedCharacterIsOnlyWarned()
    {
        var row = Plan([ExistingClaim(Hamlet, ClaimStatus.Approved)], [Hamlet],
            ClaimChange(Day(1), action: null, Master, [Player], characterName: "Гамлет, принц датский"))
            .ShouldHaveSingleItem();

        row.Action.ShouldBe(ClaimRestoreAction.Nothing);
        row.Warnings.ShouldBe([ClaimRestoreWarning.CharacterRenamed]);
    }

    [Fact]
    public void UnknownResponsibleIsWarned()
    {
        var row = Plan([ExistingClaim(Hamlet, ClaimStatus.Approved)], [Hamlet],
            ClaimChange(Day(1), CommentExtraAction.ChangeResponsible, Master, [Player], characterName: "Гамлет", text: "Мастер Мастеров → Ушедший Мастер"))
            .ShouldHaveSingleItem();

        row.ResponsibleMaster.ShouldBeNull();
        row.Warnings.ShouldBe([ClaimRestoreWarning.ResponsibleNotFound]);
    }

    [Fact]
    public void ReleasingApprovedClaimsGoFirst()
    {
        // id прежней заявки больше — порядок по id дал бы обратное.
        var other = new ClaimIdentification(Project, 5000);
        var rows = Plan([new ExistingClaimInfo(other, Hamlet.CharacterId, Player.UserId, ClaimStatus.Approved, Master.UserId, null)], [Hamlet],
            // Новая заявка (id 1234) одобрена на Гамлета, а прежняя отклонена — сначала отклонить.
            ClaimChange(Day(1), CommentExtraAction.NewClaim, Player, [Master], characterName: "Гамлет"),
            ClaimChange(Day(2), CommentExtraAction.ApproveByMaster, Master, [Player], characterName: "Гамлет"),
            ClaimChange(Day(2), CommentExtraAction.DeclineByMaster, Master, [Player], characterName: "Гамлет", claimId: other));

        rows.Select(r => (r.ClaimId, r.Status)).ShouldBe([(other, ClaimStatus.DeclinedByMaster), (Claim, ClaimStatus.Approved)]);
    }

    [Fact]
    public void MovedApprovedClaimGoesAfterReleasing()
    {
        // Принятую заявку (1234) перемещают на Офелию, а принятая на Офелии (5000) отклонена: сначала отклонить,
        // хотя у перемещаемой статус не меняется.
        var onOphelia = new ClaimIdentification(Project, 5000);
        var rows = Plan(
            [ExistingClaim(Hamlet, ClaimStatus.Approved), new ExistingClaimInfo(onOphelia, Ophelia.CharacterId, Player.UserId, ClaimStatus.Approved, Master.UserId, null)],
            [Hamlet, Ophelia],
            ClaimChange(Day(1), CommentExtraAction.MoveByMaster, Master, [Player], characterName: "Офелия"),
            ClaimChange(Day(1), CommentExtraAction.DeclineByMaster, Master, [Player], characterName: "Офелия", claimId: onOphelia));

        rows.Select(r => r.ClaimId).ShouldBe([onOphelia, Claim]);
    }

    [Fact]
    public void LostClaimApprovedFromSlotCreatesNewCharacter()
    {
        // Одобрение слота создало персонажа с именем слота — его и нет в базе.
        var slot = Character(5, "Стражник", isSlot: true);
        var row = Plan([], [slot],
            ClaimChange(Day(1), CommentExtraAction.NewClaim, Player, [Master], characterName: "Стражник"),
            ClaimChange(Day(2), CommentExtraAction.ApproveByMaster, Master, [Player], characterName: "Стражник"))
            .ShouldHaveSingleItem();

        row.Character.ShouldBeNull();
        row.CharacterName.ShouldBe("Стражник");
        row.Warnings.ShouldContain(ClaimRestoreWarning.ApprovedOnSlot);
    }

    [Fact]
    public void LostClaimInDiscussionOnSlotStaysOnSlot()
    {
        var slot = Character(5, "Стражник", isSlot: true);
        var row = Plan([], [slot],
            ClaimChange(Day(1), CommentExtraAction.NewClaim, Player, [Master], characterName: "Стражник"))
            .ShouldHaveSingleItem();

        row.Character.ShouldBe(slot.CharacterId);
        row.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void ExistingClaimOnSlotApprovedIsWarned()
    {
        var slot = Character(5, "Стражник", isSlot: true);
        var row = Plan([ExistingClaim(slot, ClaimStatus.Discussed)], [slot],
            ClaimChange(Day(1), CommentExtraAction.ApproveByMaster, Master, [Player], characterName: "Стражник"))
            .ShouldHaveSingleItem();

        row.Warnings.ShouldBe([ClaimRestoreWarning.ApprovedOnSlot]);
    }

    [Fact]
    public void AmbiguousCharacterGivesCandidates()
    {
        var hamlet2 = Character(4, "Гамлет");
        var row = Plan([], [Hamlet, hamlet2],
            ClaimChange(Day(1), CommentExtraAction.NewClaim, Player, [Master], characterName: "Гамлет"))
            .ShouldHaveSingleItem();

        row.Character.ShouldBeNull();
        row.CharacterCandidates.ShouldBe([Hamlet.CharacterId, hamlet2.CharacterId], ignoreOrder: true);
    }

    [Fact]
    public void NamesAreComparedTrimmed()
    {
        var spaced = Character(6, "Гамлет ");
        var row = Plan([ExistingClaim(spaced, ClaimStatus.Discussed)], [spaced],
            ClaimChange(Day(1), CommentExtraAction.ApproveByMaster, Master, [Player], characterName: "Гамлет"))
            .ShouldHaveSingleItem();

        row.Character.ShouldBeNull();
        row.Warnings.ShouldBeEmpty();
        row.Status.ShouldBe(ClaimStatus.Approved);
    }

    [Fact]
    public void LostClaimWithoutSubmissionIsWarned()
    {
        var row = Plan([], [Hamlet],
            ClaimChange(Day(1), CommentExtraAction.ApproveByMaster, Master, [Player], characterName: "Гамлет"))
            .ShouldHaveSingleItem();

        row.Action.ShouldBe(ClaimRestoreAction.Create);
        row.Warnings.ShouldContain(ClaimRestoreWarning.LostClaimNotSubmittedAfterBackup);
    }

    [Fact]
    public void LostClaimWithoutCharacterNameIsWarned()
    {
        var header = $"{ProjectTitle}: Персонаж, игрок {Player.DisplayName.DisplayName}";
        var payment = Row(header, "Онлайн-оплата на сумму 1500.00₽ подтверждена.", User(1, "Оплаты"), Master,
            new FinanceOperationIdentification(Project, Claim.ClaimId, 55), Day(1));

        var row = Plan([], [Hamlet], [payment]).ShouldHaveSingleItem();

        row.CharacterName.ShouldBeNull();
        row.Warnings.ShouldContain(ClaimRestoreWarning.CharacterNotFound);
    }

    [Fact]
    public void LostClaimWithUnknownResponsibleFallsBackToDefault()
    {
        var row = Plan([], [Hamlet],
            ClaimChange(Day(1), CommentExtraAction.NewClaim, Player, [Master], characterName: "Гамлет"),
            ClaimChange(Day(2), CommentExtraAction.ChangeResponsible, Master, [Player], characterName: "Гамлет", text: "А → Неизвестный"))
            .ShouldHaveSingleItem();

        row.ResponsibleMaster.ShouldBe(Hamlet.DefaultResponsibleMaster);
        row.Warnings.ShouldContain(ClaimRestoreWarning.ResponsibleNotFound);
    }

    [Fact]
    public void ExistingClaimReplaysOnlyEventsAfterBackupFromDbStatus()
    {
        // До бэкапа заявку отклоняли, но в базе она уже принята — это учтено; после бэкапа только оплата.
        var row = Plan([ExistingClaim(Hamlet, ClaimStatus.Approved)], [Hamlet],
            ClaimChange(BackupAt.AddDays(-5), CommentExtraAction.DeclineByMaster, Master, [Player], characterName: "Гамлет"),
            ClaimChange(Day(1), CommentExtraAction.PaidFee, Master, [Player], characterName: "Гамлет", money: 100))
            .ShouldHaveSingleItem();

        row.Action.ShouldBe(ClaimRestoreAction.Nothing);
    }
}
