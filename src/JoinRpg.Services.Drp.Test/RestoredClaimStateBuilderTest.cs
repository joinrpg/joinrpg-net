using JoinRpg.DomainTypes.Notifications;
using static JoinRpg.Services.Drp.Test.ClaimNotificationFactory;

namespace JoinRpg.Services.Drp.Test;

public class RestoredClaimStateBuilderTest
{
    [Fact]
    public void NewClaimApprovedAfterBackup()
    {
        var state = Build(
            ClaimChange(Day(1), CommentExtraAction.NewClaim, Player, [Master]),
            ClaimChange(Day(2), action: null, Master, [Player], text: "Здравствуйте"),
            ClaimChange(Day(3), CommentExtraAction.ApproveByMaster, Master, [Player], characterName: "Новое имя"));

        state.IsNewClaim.ShouldBeTrue();
        state.Status.ShouldBe(ClaimStatus.Approved);
        state.StatusChangedAt.ShouldBe(Day(3));
        state.CharacterName.ShouldBe("Новое имя");
        state.MayHaveChangedCharacter.ShouldBeTrue();
        state.Player.ShouldBe(Player.UserId);
        state.Warnings.ShouldBeEmpty();
        state.FirstActivity.ShouldBe(Day(1));
        state.LastActivity.ShouldBe(Day(3));
    }

    [Fact]
    public void NoStatusEventsKeepKnownStatus()
    {
        var state = Build(knownStatus: ClaimStatus.OnHold, ClaimChange(Day(1), action: null, Master, [Player]));

        state.IsNewClaim.ShouldBeFalse();
        state.Status.ShouldBe(ClaimStatus.OnHold);
        state.StatusChangedAt.ShouldBeNull();
        state.MayHaveChangedCharacter.ShouldBeFalse();
    }

    [Fact]
    public void NoStatusEventsAndUnknownStatus()
        => Build(ClaimChange(Day(1), action: null, Master, [Player])).Status.ShouldBeNull();

    [Fact]
    public void VisibleMasterCommentMakesUserClaimDiscussed()
    {
        var state = Build(
            ClaimChange(Day(1), CommentExtraAction.NewClaim, Player, [Master]),
            ClaimChange(Day(2), action: null, Master, [Player]));

        state.Status.ShouldBe(ClaimStatus.Discussed);
        state.StatusChangedAt.ShouldBe(Day(2));
    }

    [Fact]
    public void HiddenMasterCommentKeepsUserClaimAdded()
    {
        // Скрытый от игрока комментарий: среди получателей игрока нет.
        var state = Build(
            ClaimChange(Day(1), CommentExtraAction.NewClaim, Player, [Master]),
            ClaimChange(Day(2), action: null, Master, [OtherMaster]));

        state.Status.ShouldBe(ClaimStatus.AddedByUser);
    }

    [Fact]
    public void PlayerCommentMakesMasterClaimDiscussed()
    {
        var state = Build(
            ClaimChange(Day(1), CommentExtraAction.NewClaim, Master, [Player]),
            ClaimChange(Day(2), action: null, Player, [Master]));

        state.Status.ShouldBe(ClaimStatus.Discussed);
    }

    [Theory]
    [InlineData(CommentExtraAction.DeclineByMaster, ClaimStatus.DeclinedByMaster)]
    [InlineData(CommentExtraAction.OnHoldByMaster, ClaimStatus.OnHold)]
    [InlineData(CommentExtraAction.CheckedIn, ClaimStatus.CheckedIn)]
    [InlineData(CommentExtraAction.OutOfGame, ClaimStatus.Approved)]
    [InlineData(CommentExtraAction.PaidFee, ClaimStatus.Approved)]
    public void StatusAfterMasterAction(CommentExtraAction action, ClaimStatus expected)
        => Build(knownStatus: ClaimStatus.Approved, ClaimChange(Day(1), action, Master, [Player])).Status.ShouldBe(expected);

    [Fact]
    public void DeclineByPlayer()
        => Build(knownStatus: ClaimStatus.Approved, ClaimChange(Day(1), CommentExtraAction.DeclineByPlayer, Player, [Master]))
            .Status.ShouldBe(ClaimStatus.DeclinedByUser);

    [Fact]
    public void PlayerFromInitiatorWhenNeverGreeted()
    {
        // Игрок только писал сам — его в получателях нет.
        var state = Build(ClaimChange(Day(1), CommentExtraAction.NewClaim, Player, [Master]));

        state.Player.ShouldBe(Player.UserId);
    }

    [Fact]
    public void PlayerNotFound()
    {
        var state = Build(ClaimChange(Day(1), action: null, Master, [OtherMaster]));

        state.Player.ShouldBeNull();
        state.Warnings.ShouldBe([RestoredClaimWarning.PlayerNotFound]);
    }

    [Fact]
    public void PlayerAmbiguousWhenNamesakeIsGreeted()
    {
        var namesake = User(30, Player.DisplayName.DisplayName);
        var state = Build(ClaimChange(Day(1), action: null, Master, [Player, namesake]));

        state.Player.ShouldBeNull();
        state.PlayerCandidates.ShouldBe([Player.UserId, namesake.UserId], ignoreOrder: true);
        state.Warnings.ShouldBe([RestoredClaimWarning.PlayerAmbiguous]);
    }

    [Fact]
    public void LastResponsibleAndFee()
    {
        var state = Build(
            ClaimChange(Day(1), CommentExtraAction.ChangeResponsible, Master, [Player], text: "Мастер Мастеров → Другой Мастер"),
            ClaimChange(Day(2), CommentExtraAction.FeeChanged, Master, [Player], text: "1500"),
            ClaimChange(Day(3), CommentExtraAction.ChangeResponsible, OtherMaster, [Player], text: "Другой Мастер → Третий → Четвёртый"),
            ClaimChange(Day(4), CommentExtraAction.FeeChanged, Master, [Player], text: "2000"));

        state.ResponsibleMasterName.ShouldBe("Четвёртый");
        state.ResponsibleChangedAt.ShouldBe(Day(3));
        state.ManualFee.ShouldBe(2000);
    }

    [Theory]
    [InlineData(7, ClaimStatus.Discussed)] // до #4518: «подана» и сразу «обсуждается»
    [InlineData(9, ClaimStatus.AddedByMaster)] // после — «предложена»
    public void RestoreByMasterDependsOnWhen(int month, ClaimStatus expected)
    {
        var at = new DateTimeOffset(2026, month, 15, 12, 0, 0, TimeSpan.Zero);

        Build(knownStatus: ClaimStatus.DeclinedByMaster, ClaimChange(at, CommentExtraAction.RestoreByMaster, Master, [Player]))
            .Status.ShouldBe(expected);
    }

    [Theory]
    [InlineData(9, 1, ClaimStatus.Discussed)] // до #5123 модерация шла обычным комментарием
    [InlineData(10, 2, ClaimStatus.AddedByUser)]
    public void FinanceModerationDependsOnWhen(int month, int day, ClaimStatus expected)
    {
        var at = new DateTimeOffset(2026, month, day, 12, 0, 0, TimeSpan.Zero);

        Build(knownStatus: ClaimStatus.AddedByUser, ClaimChange(at, CommentExtraAction.ApproveFinance, Master, [Player]))
            .Status.ShouldBe(expected);
    }

    [Fact]
    public void InvitationAcceptedMeansDiscussedEvenWithUnknownStatus()
        => Build(ClaimChange(Day(1), CommentExtraAction.InvitationAcceptedByPlayer, Player, [Master])).Status.ShouldBe(ClaimStatus.Discussed);

    [Fact]
    public void PlayerNameWithTrailingDotIsMatchedByGreeting()
    {
        var player = User(12, "Петров А.");
        var state = Build(ClaimChange(Day(1), action: null, Master, [player], player: player));

        state.Player.ShouldBe(player.UserId);
    }

    [Fact]
    public void PreferentialFeeRequestedByMasterDoesNotMakeHimPlayer()
    {
        // Запрос льготного взноса подписан «игроком», кто бы его ни делал.
        var state = Build(ClaimChange(Day(1), CommentExtraAction.RequestPreferential, Master, [OtherMaster]));

        state.Player.ShouldBeNull();
        state.Warnings.ShouldBe([RestoredClaimWarning.PlayerNotFound]);
    }

    [Fact]
    public void SecondRoleHasZeroFee()
    {
        var state = Build(ClaimChange(Day(1), CommentExtraAction.SecondRole, Master, [Player]));

        state.IsNewClaim.ShouldBeTrue();
        state.Status.ShouldBe(ClaimStatus.Approved);
        state.ManualFee.ShouldBe(0);
    }

    [Fact]
    public void OnlinePaymentCountsAsActivityOnly()
    {
        var header = $"{ProjectTitle}: Персонаж, игрок {Player.DisplayName.DisplayName}";
        var payment = Row(header, "Онлайн-оплата на сумму 1500.00₽ подтверждена.", User(1, "Оплаты"), Master, new FinanceOperationIdentification(Project, Claim.ClaimId, 55), Day(5));

        var state = Build(knownStatus: ClaimStatus.Approved, ClaimChange(Day(1), action: null, Master, [Player]), [payment]);

        state.Status.ShouldBe(ClaimStatus.Approved);
        state.LastActivity.ShouldBe(Day(5));
    }

    private static RestoredClaimState Build(params IEnumerable<NotificationMessageForRecipient>[] rows)
        => Build(knownStatus: null, rows);

    private static RestoredClaimState Build(ClaimStatus? knownStatus, params IEnumerable<NotificationMessageForRecipient>[] rows)
        => RestoredClaimStateBuilder.Build(Claim, ClaimNotificationParser.Parse(rows.SelectMany(r => r)), knownStatus);
}
