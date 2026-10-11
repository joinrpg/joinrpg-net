namespace JoinRpg.Tools.RestoreLostUsers.Test;

public class LostUserPlannerTest
{
    private const int BackupMax = 1000;
    private const int RobotId = 10;

    private static readonly DateTimeOffset Day1 = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Day2 = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset LostAt = new(2026, 10, 7, 22, 30, 49, TimeSpan.Zero);
    private static readonly DateTimeOffset AfterLoss = LostAt.AddDays(1);

    private static readonly ExistingUser[] Existing =
    [
        new(RobotId, "robot@joinrpg.ru", "robot@joinrpg.ru"),
        new(500, "old@example.com", "old@example.com"),
        new(1500, "restored@example.com", "restored@example.com"),
    ];

    private static LostUserPlanner CreatePlanner() => new(Existing, new HashSet<int> { RobotId }, BackupMax, LostAt);

    private static NotificationUserActivity Activity(int id, int asRecipient = 1, int asInitiator = 0)
        => new(id, Day1, Day2, asRecipient, asInitiator);

    private static NotificationUserDetails Details(int id, string? email, string? telegram = null, params GreetingLine[] greetings)
        => new(id, email is null ? [] : [new ChannelValue($"Email({email})", Day2)], telegram, greetings);

    private static NotificationUserDetails Emails(int id, params ChannelValue[] emails) => new(id, emails, null, []);

    private static LostUserDecision PlanSingle(NotificationUserActivity activity, NotificationUserDetails? details)
    {
        var planner = CreatePlanner();
        var map = details is null
            ? new Dictionary<int, NotificationUserDetails>()
            : new Dictionary<int, NotificationUserDetails> { [details.UserId] = details };
        return planner.Plan([activity], map).ShouldHaveSingleItem();
    }

    [Fact]
    public void LostUserWithEmailIsCreated()
    {
        var decision = PlanSingle(Activity(1001), Details(1001, "new@example.com"));

        decision.Kind.ShouldBe(DecisionKind.Create);
        decision.Email.ShouldBe("new@example.com");
        decision.FirstSeen.ShouldBe(Day1);
    }

    [Fact]
    public void ExistingUserFromBackupIsIgnored()
    {
        var planner = CreatePlanner();
        planner.Plan([Activity(500)], new Dictionary<int, NotificationUserDetails>()).ShouldBeEmpty();
        planner.SelectIdsNeedingDetails([Activity(500)]).ShouldBeEmpty();
    }

    [Fact]
    public void SystemUserIsIgnored()
    {
        var planner = CreatePlanner();
        planner.Plan([Activity(RobotId, asRecipient: 0, asInitiator: 5)], new Dictionary<int, NotificationUserDetails>()).ShouldBeEmpty();
    }

    [Fact]
    public void InitiatorOnlyWithoutEmailIsSkipped()
        => PlanSingle(Activity(1002, asRecipient: 0, asInitiator: 3), null).Kind.ShouldBe(DecisionKind.NoEmail);

    [Fact]
    public void EmailTakenByAnotherUserIsSkipped()
    {
        var decision = PlanSingle(Activity(1003), Details(1003, "OLD@example.com"));

        decision.Kind.ShouldBe(DecisionKind.EmailTaken);
        decision.ConflictingUserId.ShouldBe(500);
    }

    [Fact]
    public void EmailTakenOnlyInUserNameColumnIsSkipped()
    {
        var planner = new LostUserPlanner([new ExistingUser(600, null, "name@example.com")], new HashSet<int>(), BackupMax, LostAt);

        var decision = planner.Plan([Activity(1004)], new Dictionary<int, NotificationUserDetails> { [1004] = Details(1004, "name@example.com") })
            .ShouldHaveSingleItem();

        decision.Kind.ShouldBe(DecisionKind.EmailTaken);
    }

    [Fact]
    public void MissingIdNotAboveBackupMaxIsContradiction()
        => PlanSingle(Activity(700), Details(700, "x@example.com")).Kind.ShouldBe(DecisionKind.IdNotAboveBackupMax);

    [Fact]
    public void NonPositiveIdIsInvalid()
        => PlanSingle(Activity(0), null).Kind.ShouldBe(DecisionKind.InvalidId);

    [Fact]
    public void UnparsableEmailIsReported()
        => PlanSingle(Activity(1005), Emails(1005, new ChannelValue("Email(no-at-sign)", Day2))).Kind.ShouldBe(DecisionKind.InvalidEmail);

    [Fact]
    public void DuplicateEmailsAmongCandidatesAreAllSkipped()
    {
        var planner = CreatePlanner();
        var details = new Dictionary<int, NotificationUserDetails>
        {
            [1006] = Details(1006, "same@example.com"),
            [1007] = Details(1007, "Same@Example.com"),
            [1008] = Details(1008, "other@example.com"),
        };

        var decisions = planner.Plan([Activity(1006), Activity(1007), Activity(1008)], details);

        decisions.Select(d => (d.UserId, d.Kind, d.ConflictingUserId)).ShouldBe(
        [
            (1006, DecisionKind.DuplicateEmail, 1007),
            (1007, DecisionKind.DuplicateEmail, 1006),
            (1008, DecisionKind.Create, null),
        ]);
    }

    [Fact]
    public void DuplicateGroupOfThreeIsAllSkipped()
    {
        var planner = CreatePlanner();
        var details = new Dictionary<int, NotificationUserDetails>
        {
            [1011] = Details(1011, "three@example.com"),
            [1012] = Details(1012, "THREE@example.com"),
            [1013] = Details(1013, "three@Example.com"),
        };

        var decisions = planner.Plan([Activity(1011), Activity(1012), Activity(1013)], details);

        decisions.ShouldAllBe(d => d.Kind == DecisionKind.DuplicateEmail);
        decisions.ShouldAllBe(d => d.ConflictingUserId != null && d.ConflictingUserId != d.UserId);
    }

    [Fact]
    public void IdTakenByNewRegistrantAfterLossIsConflict()
    {
        // Сайт открыли до восстановления, id 1500 достался новому регистранту, и ему тоже шли уведомления.
        // Его адрес после аварии не должен выдаваться за адрес потерянного пользователя.
        var planner = new LostUserPlanner([new ExistingUser(1500, "registrant@example.com", "registrant@example.com")], new HashSet<int>(), BackupMax, LostAt);
        var details = Emails(1500,
            new ChannelValue("Email(lost@example.com)", Day2),
            new ChannelValue("Email(registrant@example.com)", AfterLoss));

        var decision = planner.Plan([Activity(1500)], new Dictionary<int, NotificationUserDetails> { [1500] = details }).ShouldHaveSingleItem();

        decision.Kind.ShouldBe(DecisionKind.IdTakenByOtherUser);
        decision.Email.ShouldBe("lost@example.com");
        decision.ConflictingUserId.ShouldBe(1500);
    }

    [Fact]
    public void ExistingIdWithoutEmailBeforeLossIsConflict()
    {
        var planner = new LostUserPlanner([new ExistingUser(1500, "registrant@example.com", "registrant@example.com")], new HashSet<int>(), BackupMax, LostAt);

        var decision = planner.Plan([Activity(1500, asRecipient: 0, asInitiator: 2)], new Dictionary<int, NotificationUserDetails>()).ShouldHaveSingleItem();

        decision.Kind.ShouldBe(DecisionKind.IdTakenByOtherUser);
    }

    [Fact]
    public void LatestEmailBeforeLossIsUsedAndAllAreReported()
    {
        var details = Emails(1014,
            new ChannelValue("Email(first@example.com)", Day1),
            new ChannelValue("Email(second@example.com)", Day2),
            new ChannelValue("Email(after-loss@example.com)", AfterLoss));

        var decision = PlanSingle(Activity(1014), details);

        decision.Kind.ShouldBe(DecisionKind.Create);
        decision.Email.ShouldBe("second@example.com");
        decision.AllEmails.ShouldBe(["second@example.com", "first@example.com"]);
    }

    [Fact]
    public void SingleEmailIsNotRepeatedInAllEmails()
        => PlanSingle(Activity(1015), Details(1015, "one@example.com")).AllEmails.ShouldBeNull();

    [Fact]
    public void OnlyEmailAfterLossMeansNoEmail()
        => PlanSingle(Activity(1016), Emails(1016, new ChannelValue("Email(late@example.com)", AfterLoss))).Kind.ShouldBe(DecisionKind.NoEmail);

    [Fact]
    public void AlreadyRestoredUserIsNotCreatedAgain()
    {
        var planner = CreatePlanner();
        planner.SelectIdsNeedingDetails([Activity(1500)]).ShouldBe([1500]);

        PlanSingle(Activity(1500), Details(1500, "Restored@example.com")).Kind.ShouldBe(DecisionKind.AlreadyExists);
    }

    [Fact]
    public void IdAfterBackupTakenByOtherUserIsReported()
    {
        var decision = PlanSingle(Activity(1500), Details(1500, "somebody-else@example.com"));

        decision.Kind.ShouldBe(DecisionKind.IdTakenByOtherUser);
        decision.ConflictingUserId.ShouldBe(1500);
    }

    [Fact]
    public void TelegramIsTakenFromChannelValue()
    {
        // Текущий формат канала (с #4642): TelegramChatId(…)
        var decision = PlanSingle(Activity(1009), Details(1009, "tg@example.com", "TelegramChatId(123456)"));

        decision.TelegramChatId.ShouldBe(123456);
        decision.TelegramUserName.ShouldBeNull();
    }

    [Theory]
    [InlineData("TelegramChatId(123456)", 123456L, null)]
    [InlineData("TelegramChatId(-1001234567890)", -1001234567890L, null)]
    [InlineData("Telegram(123456)", 123456L, null)]
    [InlineData("TelegramId(123456)", 123456L, null)]
    [InlineData("Telegram(123456, @someone)", 123456L, "someone")] // до #4642
    [InlineData("Telegram(-1001234567890, @channel)", -1001234567890L, "channel")]
    public void ParsesTelegramChannelValue(string value, long expectedChatId, string? expectedUserName)
        => LostUserPlanner.ParseTelegram(value).ShouldBe((expectedChatId, expectedUserName));

    [Theory]
    [InlineData(null)]
    [InlineData("garbage")]
    public void UnparsableTelegramIsIgnored(string? value)
        => LostUserPlanner.ParseTelegram(value).ShouldBe((null, null));

    [Fact]
    public void NameIsTakenFromGreetings()
    {
        var decision = PlanSingle(
            Activity(1010),
            Details(1010, "name@example.com", null, new GreetingLine("Добрый день, Иван Петров!", 3, Day2)));

        decision.Name.ShouldBe("Иван Петров");
    }
}
