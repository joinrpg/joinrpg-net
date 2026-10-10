namespace JoinRpg.Tools.RestoreLostUsers.Test;

public class NewUserRowTest
{
    private static LostUserDecision Decision(DecisionKind kind = DecisionKind.Create, string? email = "lost@example.com") => new(
        UserId: 1001,
        kind,
        email,
        Name: "Иван",
        TelegramChatId: null,
        TelegramUserName: null,
        FirstSeen: new DateTimeOffset(2026, 6, 1, 15, 0, 0, TimeSpan.FromHours(3)),
        LastSeen: new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero),
        AsRecipient: 1,
        AsInitiator: 0,
        ConflictingUserId: null);

    [Fact]
    public void RegisterDateIsFirstActivityInUtc()
        => NewUserRow.From(Decision(), Guid.NewGuid()).RegisterDateUtc.ShouldBe(new DateTime(2026, 6, 1, 12, 0, 0));

    [Fact]
    public void SecurityStampIsTheGivenGuid()
    {
        // Пустой штамп, как при обычной регистрации, оживил бы cookie, выданные до потери БД.
        var stamp = Guid.NewGuid();

        var row = NewUserRow.From(Decision(), stamp);

        row.SecurityStamp.ShouldBe(stamp.ToString());
        row.SecurityStamp.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CopiesIdEmailAndName()
        => NewUserRow.From(Decision(), Guid.Empty).ShouldBe(
            new NewUserRow(1001, "lost@example.com", "Иван", new DateTime(2026, 6, 1, 12, 0, 0), Guid.Empty.ToString()));

    [Fact]
    public void RefusesDecisionOtherThanCreate()
        => Should.Throw<ArgumentException>(() => NewUserRow.From(Decision(DecisionKind.EmailTaken), Guid.NewGuid()));
}
