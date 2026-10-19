using static JoinRpg.Services.Drp.Test.ClaimNotificationFactory;

namespace JoinRpg.Services.Drp.Test;

public class ClaimNotificationParserTest
{
    public static TheoryData<CommentExtraAction> AllActions() => [.. Enum.GetValues<CommentExtraAction>()];

    [Theory]
    [MemberData(nameof(AllActions))]
    public void RecognizesEveryActionByMaster(CommentExtraAction action)
    {
        var e = Parse(ClaimChange(Day(1), action, Master, [Player, OtherMaster], text: "пояснение")).ShouldHaveSingleItem();

        e.Kind.ShouldBe(ClaimNotificationKind.ClaimChange);
        e.Action.ShouldBe(action);
        e.Author.ShouldBe(ClaimEventAuthor.Master);
        e.MasterName.ShouldBe(Master.DisplayName.DisplayName);
        e.CharacterName.ShouldBe("Персонаж");
        e.PlayerName.ShouldBe(Player.DisplayName.DisplayName);
        e.Text.ShouldBe("пояснение");
    }

    [Fact]
    public void RecognizesActionByPlayer()
    {
        var e = Parse(ClaimChange(Day(1), CommentExtraAction.NewClaim, Player, [Master])).ShouldHaveSingleItem();

        e.Action.ShouldBe(CommentExtraAction.NewClaim);
        e.Author.ShouldBe(ClaimEventAuthor.Player);
        e.MasterName.ShouldBeNull();
    }

    [Fact]
    public void PlainComment()
    {
        var e = Parse(ClaimChange(Day(1), action: null, Player, [Master], text: "Вопрос\nв две строки")).ShouldHaveSingleItem();

        e.Kind.ShouldBe(ClaimNotificationKind.ClaimChange);
        e.Action.ShouldBeNull();
        e.Author.ShouldBe(ClaimEventAuthor.Player);
        e.Text.ShouldBe("Вопрос\nв две строки");
    }

    [Fact]
    public void CommentStartingWithBoldActionNameIsStillComment()
    {
        var e = Parse(ClaimChange(Day(1), action: null, Master, [Player], text: "**Заявка одобрена мастером**\n\nшутка")).ShouldHaveSingleItem();

        e.Action.ShouldBeNull();
        e.Text.ShouldBe("**Заявка одобрена мастером**\n\nшутка");
    }

    [Fact]
    public void MoneyOperation()
    {
        var e = Parse(ClaimChange(Day(1), CommentExtraAction.PaidFee, Master, [Player], text: "наличными", money: 1500)).ShouldHaveSingleItem();

        e.Action.ShouldBe(CommentExtraAction.PaidFee);
        e.Money.ShouldBe(1500);
        e.Text.ShouldBe("наличными");
    }

    [Fact]
    public void MergesRecipientsOfOneEvent()
    {
        var e = Parse(ClaimChange(Day(1), CommentExtraAction.ApproveByMaster, Master, [Player, OtherMaster])).ShouldHaveSingleItem();

        e.Recipients.ShouldBe([
            new ClaimNotificationRecipient(Player.UserId, Player.DisplayName.DisplayName),
            new ClaimNotificationRecipient(OtherMaster.UserId, OtherMaster.DisplayName.DisplayName),
        ], ignoreOrder: true);
    }

    [Fact]
    public void MergesRecipientsOfOldEventsReferencingClaim()
    {
        // До 2026-06-06 все события ссылались на заявку: склеиваем по времени, инициатору и тексту.
        var rows = ClaimChange(Day(1), action: null, Master, [Player, OtherMaster], text: "первый", reference: Claim)
            .Concat(ClaimChange(Day(1).AddSeconds(1), action: null, Master, [Player, OtherMaster], text: "второй", reference: Claim));

        Parse(rows).Select(e => (e.Text, e.Recipients.Count)).ShouldBe([("первый", 2), ("второй", 2)]);
    }

    [Fact]
    public void NamesWithSeparatorsInside()
    {
        var player = User(11, "Иван, игрок со стажем");
        var e = Parse(ClaimChange(Day(1), CommentExtraAction.NewClaim, player, [Master], characterName: "Граф: Монте игрока Кристо", player: player))
            .ShouldHaveSingleItem();

        e.CharacterName.ShouldBe("Граф: Монте игрока Кристо");
        e.PlayerName.ShouldBe("Иван, игрок со стажем");
        e.Author.ShouldBe(ClaimEventAuthor.Player);
    }

    [Fact]
    public void MasterNameEndingWithByPlayer()
    {
        var master = User(22, "Вася игроком");
        var e = Parse(ClaimChange(Day(1), action: null, master, [Player], text: "привет")).ShouldHaveSingleItem();

        e.Kind.ShouldBe(ClaimNotificationKind.ClaimChange);
        e.Author.ShouldBe(ClaimEventAuthor.Master);
        e.MasterName.ShouldBe("Вася игроком");
    }

    [Fact]
    public void GreetingKeepsPunctuationOfName()
    {
        var player = User(12, "Ёжик!");
        var e = Parse(ClaimChange(Day(1), action: null, Master, [player], player: player)).ShouldHaveSingleItem();

        e.Recipients.ShouldHaveSingleItem().GreetingName.ShouldBe("Ёжик!");
        e.PlayerName.ShouldBe("Ёжик!");
    }

    [Fact]
    public void LegacyBodyWithClaimLinkAndCrLf()
    {
        // До 2026-06-07 в конце тела была ссылка на заявку.
        var row = ClaimChange(Day(1), CommentExtraAction.DeclineByMaster, Master, [Player], text: "мест нет").Single();
        var lfBody = row.Body.Value.Replace("\r\n", "\n"); // исходник билдера на Windows может быть с CRLF
        var legacy = row with { Body = new MarkdownString((lfBody + "\nСтраница заявки: https://joinrpg.ru/7/claim/1234/edit\n").Replace("\n", "\r\n")) };

        var e = Parse([legacy]).ShouldHaveSingleItem();

        e.Action.ShouldBe(CommentExtraAction.DeclineByMaster);
        e.Text.ShouldBe("мест нет");
    }

    [Fact]
    public void OnlinePayment()
    {
        var header = $"{ProjectTitle}: Персонаж, игрок {Player.DisplayName.DisplayName}";
        var row = Row(header, "Онлайн-оплата на сумму 1500.00₽ подтверждена.", User(1, "Оплаты"), Master, new FinanceOperationIdentification(Project, Claim.ClaimId, 55), Day(1));

        var e = Parse([row]).ShouldHaveSingleItem();

        e.Kind.ShouldBe(ClaimNotificationKind.OnlinePayment);
        e.ClaimId.ShouldBe(Claim);
        e.Text.ShouldBe("Онлайн-оплата на сумму 1500.00₽ подтверждена.");
    }

    [Fact]
    public void AccommodationInvite()
    {
        // Приглашения к проживанию тоже ссылаются на заявку, но устроены иначе.
        var e = Parse([ClaimNotificationFactory.AccommodationInvite(Day(1))]).ShouldHaveSingleItem();

        e.Kind.ShouldBe(ClaimNotificationKind.AccommodationInvite);
        e.CharacterName.ShouldBeNull();
        e.Recipients.ShouldHaveSingleItem().GreetingName.ShouldBe("Игрок Игроков");
    }

    [Fact]
    public void OtherClaimNotification()
    {
        var row = Row($"{ProjectTitle}: что-то новое", "Добрый день, Игрок Игроков\n\nНепонятный текст", Master, Player, Claim, Day(1));

        var e = Parse([row]).ShouldHaveSingleItem();

        e.Kind.ShouldBe(ClaimNotificationKind.Other);
        e.CharacterName.ShouldBeNull();
    }

    [Fact]
    public void SkipsNotificationsNotAboutClaims()
    {
        var row = Row(ProjectTitle, "Добрый день, Игрок Игроков\n\nРассылка", Master, Player, Project, Day(1));

        Parse([row]).ShouldBeEmpty();
    }

    [Fact]
    public void OrdersEventsByTime()
    {
        var rows = ClaimChange(Day(2), CommentExtraAction.ApproveByMaster, Master, [Player])
            .Concat(ClaimChange(Day(1), CommentExtraAction.NewClaim, Player, [Master]));

        Parse(rows).Select(e => e.Action).ShouldBe([CommentExtraAction.NewClaim, CommentExtraAction.ApproveByMaster]);
    }

    private static IReadOnlyList<ClaimNotificationEvent> Parse(IEnumerable<DomainTypes.Notifications.NotificationMessageForRecipient> rows)
        => ClaimNotificationParser.Parse(rows);
}
