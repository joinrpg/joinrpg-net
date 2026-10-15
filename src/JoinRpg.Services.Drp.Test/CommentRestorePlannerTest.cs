using JoinRpg.DomainTypes.Claims;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Services.Drp.ClaimComments;
using static JoinRpg.Services.Drp.Test.ClaimNotificationFactory;

namespace JoinRpg.Services.Drp.Test;

public class CommentRestorePlannerTest
{
    private static readonly DateTimeOffset BackupAt = new(2026, 5, 8, 0, 0, 0, TimeSpan.Zero);

    private static readonly ClaimIdentification OtherClaim = new(Project, 5000);

    private static readonly IReadOnlyDictionary<ClaimIdentification, UserIdentification> ClaimInDb =
        new Dictionary<ClaimIdentification, UserIdentification> { [Claim] = Player.UserId };

    private static ClaimCommentIdentification CommentRef(int id) => new(Claim, id);

    private static ExistingClaimCommentInfo InDb(int id, UserInfoHeader author, DateTimeOffset at, string? text = "текст", bool visible = true)
        => new(id, Claim, author.UserId, at, IsCommentByPlayer: author == Player, IsVisibleToPlayer: visible, text);

    private static IReadOnlyList<CommentRestorePlanRow> Plan(
        IReadOnlyCollection<ClaimNotificationEvent> events,
        IReadOnlyDictionary<ClaimIdentification, UserIdentification>? claims = null,
        IReadOnlyCollection<ExistingClaimCommentInfo>? existing = null,
        IReadOnlyCollection<int>? taken = null,
        IReadOnlyCollection<UserIdentification>? masters = null)
        => CommentRestorePlanner.Plan(new CommentRestoreSource(
            events, claims ?? ClaimInDb, masters ?? [Master.UserId, OtherMaster.UserId], existing ?? [], taken ?? [], BackupAt));

    private static IReadOnlyList<ClaimNotificationEvent> Events(params IEnumerable<NotificationMessageForRecipient>[] rows)
        => ClaimNotificationParser.Parse(rows.SelectMany(r => r));

    private static IReadOnlyList<CommentRestorePlanRow> Plan(params IEnumerable<NotificationMessageForRecipient>[] rows)
        => Plan(Events(rows));

    private static CommentToRestore SingleComment(IReadOnlyList<CommentRestorePlanRow> plan)
    {
        var row = plan.ShouldHaveSingleItem();
        row.ClaimExists.ShouldBeTrue();
        row.Warnings.ShouldBeEmpty();
        return row.ToRestore.ShouldHaveSingleItem();
    }

    private static NotificationMessageForRecipient OnlinePayment(DateTimeOffset at)
        => Row($"{ProjectTitle}: Персонаж, игрок {Player.DisplayName.DisplayName}", "Онлайн-оплата на сумму 1500.00₽ подтверждена.",
            User(1, "Оплаты"), Master, new FinanceOperationIdentification(Project, Claim.ClaimId, 55), at);

    [Fact]
    public void CopiesForRecipientsGiveOneComment()
    {
        var comment = SingleComment(Plan(
            ClaimChange(Day(1), action: null, Master, [Player, OtherMaster], text: "Ждём взнос", reference: CommentRef(555))));

        comment.ShouldBe(new CommentToRestore(555, Master.UserId, IsCommentByPlayer: false, ExtraAction: null, Day(1), "Ждём взнос",
            IsVisibleToPlayer: true, VisibilityQuestionable: false));
    }

    [Fact]
    public void CopiesOfOneCommentMergeRecipients()
    {
        // Копии одного комментария, которые парсер не склеил: игрок — только во второй.
        var toMaster = Events(ClaimChange(Day(1), action: null, Master, [OtherMaster], reference: CommentRef(555))).ShouldHaveSingleItem();
        var toPlayer = Events(ClaimChange(Day(1), action: null, Master, [Player], reference: CommentRef(555))).ShouldHaveSingleItem();

        var comment = SingleComment(Plan([toMaster, toPlayer]));

        comment.IsVisibleToPlayer.ShouldBeTrue();
    }

    [Fact]
    public void SameEventTwiceGivesOneComment()
    {
        var events = Events(ClaimChange(Day(1), action: null, Master, [Player], reference: Claim));

        SingleComment(Plan([.. events, .. events]));
    }

    [Fact]
    public void CommentWithoutIdGetsNewId()
    {
        var comment = SingleComment(Plan(
            ClaimChange(Day(1), action: null, Master, [Player], text: "Старый формат", reference: Claim)));

        comment.CommentId.ShouldBeNull();
        comment.Text.ShouldBe("Старый формат");
    }

    [Fact]
    public void MultilineMarkdownTextIsKept()
    {
        const string text = "Первая строка\n\n> цитата «ёлочки» и \"кавычки\"\n\n**жирный**, _курсив_ и `код`\n- пункт";

        SingleComment(Plan(ClaimChange(Day(1), action: null, Master, [Player], text: text))).Text.ShouldBe(text);
    }

    [Theory]
    [InlineData(CommentExtraAction.NewClaim)]
    [InlineData(CommentExtraAction.DeclineByPlayer)]
    public void PlayerActionIsVisible(CommentExtraAction action)
    {
        var comment = SingleComment(Plan(ClaimChange(Day(1), action, Player, [Master])));

        comment.Author.ShouldBe(Player.UserId);
        comment.IsCommentByPlayer.ShouldBeTrue();
        comment.ExtraAction.ShouldBe(action);
        comment.IsVisibleToPlayer.ShouldBeTrue();
        comment.VisibilityQuestionable.ShouldBeFalse();
    }

    [Fact]
    public void ClaimCreatedByMasterIsMasterComment()
    {
        var comment = SingleComment(Plan(ClaimChange(Day(1), CommentExtraAction.NewClaim, Master, [Player])));

        comment.Author.ShouldBe(Master.UserId);
        comment.IsCommentByPlayer.ShouldBeFalse();
        comment.ExtraAction.ShouldBe(CommentExtraAction.NewClaim);
        comment.IsVisibleToPlayer.ShouldBeTrue();
    }

    [Fact]
    public void ActionWithoutTextIsRestoredWithAction()
    {
        var comment = SingleComment(Plan(ClaimChange(Day(1), CommentExtraAction.ApproveByMaster, Master, [Player], text: "")));

        comment.ExtraAction.ShouldBe(CommentExtraAction.ApproveByMaster);
        comment.Text.ShouldBe("");
    }

    [Fact]
    public void MasterCommentWithoutPlayerAmongRecipientsIsHidden()
    {
        var comment = SingleComment(Plan(ClaimChange(Day(1), action: null, Master, [OtherMaster])));

        comment.IsVisibleToPlayer.ShouldBeFalse();
        comment.VisibilityQuestionable.ShouldBeFalse();
    }

    [Fact]
    public void HiddenMasterActionKeepsAction()
    {
        var comment = SingleComment(Plan(ClaimChange(Day(1), CommentExtraAction.ChangeResponsible, Master, [OtherMaster],
            text: $"{Master.DisplayName.DisplayName} → {OtherMaster.DisplayName.DisplayName}")));

        comment.ExtraAction.ShouldBe(CommentExtraAction.ChangeResponsible);
        comment.IsVisibleToPlayer.ShouldBeFalse();
    }

    [Fact]
    public void OnlyFirstVisibleMasterCommentAfterPlayerIsQuestionable()
    {
        var row = Plan(
            ClaimChange(Day(1), action: null, Master, [Player], text: "до игрока"),
            ClaimChange(Day(2), action: null, Player, [Master], text: "вопрос"),
            ClaimChange(Day(3), action: null, Master, [Player], text: "ответ"),
            ClaimChange(Day(4), action: null, Master, [Player], text: "ещё"),
            ClaimChange(Day(5), action: null, Master, [OtherMaster], text: "скрытый"),
            ClaimChange(Day(6), action: null, Player, [Master], text: "второй вопрос"),
            ClaimChange(Day(7), action: null, Master, [OtherMaster], text: "скрытый после вопроса"),
            ClaimChange(Day(8), action: null, Master, [Player], text: "второй ответ"))
            .ShouldHaveSingleItem();

        row.ToRestore.Select(c => (c.Text, c.IsVisibleToPlayer, c.VisibilityQuestionable)).ShouldBe([
            ("до игрока", true, false),
            ("вопрос", true, false),
            ("ответ", true, true),
            ("ещё", true, false),
            ("скрытый", false, false),
            ("второй вопрос", true, false),
            ("скрытый после вопроса", false, false),
            ("второй ответ", true, true),
        ]);
    }

    [Fact]
    public void PlayerCommentInDbMakesVisibleMasterCommentQuestionable()
    {
        var comment = Plan(Events(ClaimChange(Day(1), action: null, Master, [Player])),
            existing: [InDb(50, Player, BackupAt.AddDays(-3))])
            .ShouldHaveSingleItem().ToRestore.ShouldHaveSingleItem();

        comment.VisibilityQuestionable.ShouldBeTrue();
    }

    [Fact]
    public void VisibleMasterCommentInDbAnswersPlayer()
    {
        var comment = Plan(Events(ClaimChange(Day(1), action: null, Master, [Player])),
            existing: [InDb(50, Player, BackupAt.AddDays(-3)), InDb(51, Master, BackupAt.AddDays(-2), "ответ")])
            .ShouldHaveSingleItem().ToRestore.ShouldHaveSingleItem();

        comment.VisibilityQuestionable.ShouldBeFalse();
    }

    [Fact]
    public void MasterAsPlayerMakesVisibleMasterCommentsQuestionable()
    {
        var comment = Plan(Events(ClaimChange(Day(1), action: null, Master, [Player])), masters: [Master.UserId, Player.UserId])
            .ShouldHaveSingleItem().ToRestore.ShouldHaveSingleItem();

        comment.VisibilityQuestionable.ShouldBeTrue();
    }

    [Fact]
    public void FinanceEventsAreOnlyCounted()
    {
        var row = Plan(
            [OnlinePayment(Day(1))],
            ClaimChange(Day(2), CommentExtraAction.PaidFee, Master, [Player], money: 1500),
            ClaimChange(Day(3), CommentExtraAction.FeeChanged, Master, [Player], text: "2000"),
            ClaimChange(Day(4), CommentExtraAction.RequestPreferential, Player, [Master]),
            ClaimChange(Day(5), action: null, Master, [Player], text: "обычный"))
            .ShouldHaveSingleItem();

        row.SkippedFinance.ShouldBe(4);
        row.ToRestore.ShouldHaveSingleItem().Text.ShouldBe("обычный");
    }

    [Fact]
    public void OnlinePaymentAloneGivesRow()
    {
        var row = Plan([OnlinePayment(Day(1))]).ShouldHaveSingleItem();

        row.SkippedFinance.ShouldBe(1);
        row.ToRestore.ShouldBeEmpty();
    }

    [Fact]
    public void UnparsedEventsAreWarned()
    {
        var invitation = Row($"{ProjectTitle}: приглашение на поселение", "Добрый день, Игрок Игроков\n\nВас пригласили жить вместе",
            Master, Player, CommentRef(556), Day(1));

        var row = Plan([invitation]).ShouldHaveSingleItem();

        row.Unparsed.ShouldBe(1);
        row.ToRestore.ShouldBeEmpty();
        row.Warnings.ShouldBe([CommentRestoreWarning.UnparsedEvents]);
    }

    [Fact]
    public void EventsBeforeBackupAreIgnored()
        => Plan(ClaimChange(BackupAt.AddTicks(-1), action: null, Master, [Player])).ShouldBeEmpty();

    [Fact]
    public void EventAtBackupMomentIsRestored()
        => SingleComment(Plan(ClaimChange(BackupAt, action: null, Master, [Player]))).CreatedAt.ShouldBe(BackupAt);

    [Fact]
    public void CommentWithIdAlreadyInDbIsSkipped()
    {
        var row = Plan(Events(ClaimChange(Day(1), action: null, Master, [Player], reference: CommentRef(555))),
            existing: [InDb(555, Master, Day(1), "другой текст")])
            .ShouldHaveSingleItem();

        row.ToRestore.ShouldBeEmpty();
        row.SkippedExisting.ShouldBe(1);
    }

    [Fact]
    public void CommentWithoutIdMatchesDbByAuthorTimeAndText()
    {
        var row = Plan(Events(
                ClaimChange(Day(1), action: null, Master, [Player], text: "Ждём взнос", reference: Claim),
                ClaimChange(Day(1).AddMinutes(2), action: null, Master, [Player], text: "Ждём взнос", reference: Claim),
                ClaimChange(Day(1), action: null, OtherMaster, [Player], text: "Ждём взнос", reference: Claim)),
            existing: [InDb(50, Master, Day(1).AddSeconds(40), " Ждём взнос\n")])
            .ShouldHaveSingleItem();

        row.SkippedExisting.ShouldBe(1);
        row.ToRestore.Select(c => (c.Author, c.CreatedAt)).ShouldBe([
            (OtherMaster.UserId, Day(1)),
            (Master.UserId, Day(1).AddMinutes(2)),
        ]);
    }

    [Fact]
    public void LineBreaksAreNormalizedWhenMatchingDb()
    {
        var row = Plan(Events(ClaimChange(Day(1), action: null, Master, [Player], text: "a\nb", reference: Claim)),
            existing: [InDb(50, Master, Day(1), "a\r\nb")])
            .ShouldHaveSingleItem();

        row.SkippedExisting.ShouldBe(1);
        row.ToRestore.ShouldBeEmpty();
    }

    [Fact]
    public void CommentWithoutLoadedTextMatchesNothing()
    {
        // Текст не загружен (null) — сопоставление без id не падает и не считает такой комментарий тем же.
        var row = Plan(Events(ClaimChange(Day(1), action: null, Master, [Player], text: "Ждём взнос", reference: Claim)),
            existing: [InDb(50, Master, Day(1), text: null)])
            .ShouldHaveSingleItem();

        row.SkippedExisting.ShouldBe(0);
        row.ToRestore.ShouldHaveSingleItem().Text.ShouldBe("Ждём взнос");
    }

    [Fact]
    public void TakenIdIsConflict()
    {
        var row = Plan(Events(ClaimChange(Day(1), action: null, Master, [Player], reference: CommentRef(555))), taken: [555])
            .ShouldHaveSingleItem();

        row.ToRestore.ShouldBeEmpty();
        row.Conflicts.ShouldBe([555]);
        row.Warnings.ShouldBe([CommentRestoreWarning.CommentIdConflict]);
    }

    [Fact]
    public void IdOfAnotherClaimCommentIsConflict()
    {
        var claims = new Dictionary<ClaimIdentification, UserIdentification> { [Claim] = Player.UserId, [OtherClaim] = Player.UserId };
        var otherComment = new ExistingClaimCommentInfo(555, OtherClaim, Master.UserId, Day(1), IsCommentByPlayer: false, IsVisibleToPlayer: true, "текст");

        var row = Plan(Events(ClaimChange(Day(1), action: null, Master, [Player], reference: CommentRef(555))), claims, existing: [otherComment])
            .ShouldHaveSingleItem();

        row.ClaimId.ShouldBe(Claim);
        row.Conflicts.ShouldBe([555]);
    }

    [Fact]
    public void MissingClaimIsWarned()
    {
        var row = Plan(Events(ClaimChange(Day(1), action: null, Master, [Player])), claims: new Dictionary<ClaimIdentification, UserIdentification>())
            .ShouldHaveSingleItem();

        row.ClaimExists.ShouldBeFalse();
        row.ToRestore.ShouldBeEmpty();
        row.Warnings.ShouldBe([CommentRestoreWarning.ClaimNotFound]);
    }

    [Fact]
    public void RowsByClaimIdCommentsByTime()
    {
        var claims = new Dictionary<ClaimIdentification, UserIdentification> { [Claim] = Player.UserId, [OtherClaim] = Player.UserId };
        // У заявки 5000 события раньше — порядок по времени дал бы её первой.
        var rows = Plan(Events(
                ClaimChange(Day(4), action: null, Master, [Player], text: "1234-2"),
                ClaimChange(Day(1), action: null, Master, [Player], text: "5000-1", claimId: OtherClaim),
                ClaimChange(Day(3), action: null, Master, [Player], text: "1234-1"),
                ClaimChange(Day(2), action: null, Master, [Player], text: "5000-2", claimId: OtherClaim)),
            claims);

        rows.Select(r => (r.ClaimId, string.Join(",", r.ToRestore.Select(c => c.Text)))).ShouldBe([
            (Claim, "1234-1,1234-2"),
            (OtherClaim, "5000-1,5000-2"),
        ]);
    }
}
