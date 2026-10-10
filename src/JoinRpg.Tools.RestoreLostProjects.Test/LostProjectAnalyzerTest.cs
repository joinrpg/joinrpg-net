namespace JoinRpg.Tools.RestoreLostProjects.Test;

public class LostProjectAnalyzerTest
{
    private const int BackupMax = 1000;
    private const int RobotId = 10;
    private const int AdminId = 20;
    private const int MasterId = 2001;
    private const int SecondMasterId = 2002;
    private const int PlayerId = 2003;

    private static readonly DateTimeOffset Day1 = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Day2 = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Day3 = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly ExistingUser[] Users =
    [
        new(RobotId, "robot@joinrpg.ru"),
        new(AdminId, "admin@example.com"),
        new(MasterId, "master@example.com"),
        new(SecondMasterId, "master2@example.com"),
        new(PlayerId, "player@example.com"),
    ];

    private static readonly Dictionary<int, string> Projects = new()
    {
        [500] = "Старый проект",
        [1500] = "Оболочка",
    };

    private static LostProjectAnalyzer CreateAnalyzer()
        => new(Projects, Users, new HashSet<int> { RobotId }, new HashSet<int> { AdminId }, BackupMax);

    private static ProjectNotification Notification(
        string entityReference,
        string header,
        int initiator = PlayerId,
        int recipient = MasterId,
        DateTimeOffset? at = null,
        bool masterInitiated = false,
        bool newClaim = false,
        string? adminBody = null)
        => new(entityReference, header, initiator, recipient, at ?? Day2, masterInitiated, newClaim, adminBody);

    private static ProjectNotification AdminNotification(int projectId, string name, int creator, string body, DateTimeOffset? at = null)
        => Notification($"Project({projectId})", $"Новый проект «{name}» — статус КогдаИгры", creator, AdminId, at ?? Day1, adminBody: body);

    private static LostProjectReport AnalyzeSingle(params ProjectNotification[] notifications)
        => CreateAnalyzer().Analyze(notifications, logs: null).ShouldHaveSingleItem();

    [Fact]
    public void SelectsReferencesOfLostProjectsOnly()
    {
        var refs = CreateAnalyzer().SelectReferences(
            ["Project(500)", "ClaimId(1001-5)", "ClaimCommentId(1500-1-2)", "Project(700)", "мусор", "ForumThreadId(400-3)"]);

        // 500 есть в бэкапе; 1001 и 1500 выше максимума; 700 и 400 не выше, но их нет в MSSQL — противоречие.
        refs.ShouldBe(["ClaimId(1001-5)", "ClaimCommentId(1500-1-2)", "Project(700)", "ForumThreadId(400-3)"]);
    }

    [Fact]
    public void AdminNotificationGivesExactNameCreatorAndKogdaIgra()
    {
        var report = AnalyzeSingle(
            AdminNotification(1001, "Дюна: Пробуждение", MasterId,
                "Добрый день, Админ!\n\nСоздан новый проект «Дюна: Пробуждение».\n\nМастер указал, что игра есть на КогдаИгре: «[Дюна](https://kogda-igra.ru/game/4321/)»."),
            Notification("ClaimId(1001-1)", "Дюна: Пробуждение: Пол, игрок Иван", PlayerId, SecondMasterId, Day2, newClaim: true));

        report.ProjectId.ShouldBe(1001);
        report.Name.ShouldBe("Дюна: Пробуждение");
        report.CreatorUserId.ShouldBe(MasterId);
        report.CreatorEmail.ShouldBe("master@example.com");
        report.CreatorMethod.ShouldBe(CreatorMethod.Exact);
        report.OtherCreatorCandidates.ShouldBeEmpty();
        report.KogdaIgra.ShouldBe(new KogdaIgraLink(true, 4321));
        report.Flags.ShouldBe(ProjectFlags.None);
        report.Notifications.ShouldBe(2);
        report.FirstActivity.ShouldBe(Day1);
        report.LastActivity.ShouldBe(Day2);
    }

    [Theory]
    [InlineData("Мастер указал, что игра есть на КогдаИгре.", true, null)]
    [InlineData("Мастер сообщает, что игры нет на КогдаИгре.\n\nСообщение редакторам: см. https://kogda-igra.ru/game/1/", false, null)]
    [InlineData("Мастер указал, что игра есть на КогдаИгре: «[#77](https://kogda-igra.ru/game/77/)».", true, 77)]
    public void ParsesKogdaIgraLink(string body, bool onKogdaIgra, int? gameId)
        => LostProjectAnalyzer.ParseKogdaIgra("Добрый день!\n\n" + body).ShouldBe(new KogdaIgraLink(onKogdaIgra, gameId));

    [Fact]
    public void NameWithColonIsTheCommonPrefixOfDifferentHeaders()
    {
        var report = AnalyzeSingle(
            Notification("ClaimId(1001-1)", "Дюна: Пробуждение: Пол, игрок Иван"),
            Notification("ClaimId(1001-2)", "Дюна: Пробуждение: Лето, игрок Пётр"),
            Notification("ForumThreadId(1001-3)", "ProjectName(Дюна: Пробуждение): тема на форуме Сбор: где"));

        report.Name.ShouldBe("Дюна: Пробуждение");
        report.Flags.HasFlag(ProjectFlags.NameConflict).ShouldBeFalse();
        var variant = report.NameVariants.ShouldHaveSingleItem();
        variant.NotificationsByKind[HeaderKind.Claim].ShouldBe(2);
        variant.NotificationsByKind[HeaderKind.Forum].ShouldBe(1);
    }

    [Fact]
    public void ClaimsOnlyWithColonInProjectNamePickLongVariant()
    {
        var report = AnalyzeSingle(
            Notification("ClaimId(1001-1)", "Дюна: Пробуждение: Пол, игрок Иван"),
            Notification("ClaimId(1001-2)", "Дюна: Пробуждение: Лето, игрок Пётр"));

        // Оба варианта объясняют оба заголовка — это ничья, но «: » скорее в названии проекта.
        report.Name.ShouldBe("Дюна: Пробуждение");
        report.Flags.HasFlag(ProjectFlags.NameConflict).ShouldBeTrue();
        report.NameVariants.Select(v => v.Name).ShouldBe(["Дюна: Пробуждение", "Дюна"]);
    }

    [Fact]
    public void RenamedVariantsCarryLastSeen()
    {
        var report = AnalyzeSingle(
            Notification("ClaimId(1001-1)", "Дюна: Пол, игрок Иван", at: Day1),
            Notification("ClaimId(1001-2)", "Арракис: Пол, игрок Иван", at: Day3));

        report.NameVariants.Single(v => v.Name == "Дюна").LastSeen.ShouldBe(Day1);
        report.NameVariants.Single(v => v.Name == "Арракис").LastSeen.ShouldBe(Day3);
    }

    [Fact]
    public void ColonInCharacterNameDoesNotStealProjectName()
    {
        var report = AnalyzeSingle(
            Notification("ClaimId(1001-1)", "Дюна: Пол: наследник, игрок Иван"),
            Notification("ClaimId(1001-2)", "Дюна: Лето, игрок Пётр"));

        report.Name.ShouldBe("Дюна");
        report.Flags.HasFlag(ProjectFlags.NameConflict).ShouldBeFalse();
    }

    [Fact]
    public void SingleAmbiguousHeaderIsConflict()
    {
        var report = AnalyzeSingle(Notification("ClaimId(1001-1)", "Дюна: Пол: наследник, игрок Иван"));

        report.Name.ShouldBe("Дюна");
        report.Flags.HasFlag(ProjectFlags.NameConflict).ShouldBeTrue();
        report.NameVariants.Select(v => v.Name).ShouldBe(["Дюна", "Дюна: Пол"]);
    }

    [Fact]
    public void RenamedProjectIsConflictWithBothNames()
    {
        var report = AnalyzeSingle(
            Notification("ClaimId(1001-1)", "Дюна: Пол, игрок Иван", at: Day1),
            Notification("ClaimId(1001-2)", "Дюна: Лето, игрок Пётр", at: Day1),
            Notification("ClaimId(1001-3)", "Арракис: Пол, игрок Иван", at: Day3));

        report.Name.ShouldBe("Дюна");
        report.Flags.HasFlag(ProjectFlags.NameConflict).ShouldBeTrue();
        report.NameVariants.Select(v => v.Name).ShouldBe(["Дюна", "Арракис"]);
    }

    [Fact]
    public void AdminNameDisagreeingWithHeadersIsConflict()
    {
        var report = AnalyzeSingle(
            AdminNotification(1001, "Дюна", MasterId, "Мастер указал, что игра есть на КогдаИгре."),
            Notification("ClaimId(1001-1)", "Арракис: Пол, игрок Иван"));

        report.Name.ShouldBe("Дюна");
        report.Flags.HasFlag(ProjectFlags.NameConflict).ShouldBeTrue();
    }

    [Fact]
    public void MassMailBeforeFormatChangeIsNotANameSource()
    {
        var report = AnalyzeSingle(
            Notification("Project(1001)", "Важно: сбор", MasterId, PlayerId, Day2),
            Notification("ClaimId(1001-1)", "Дюна: Пол, игрок Иван"));

        report.Name.ShouldBe("Дюна");
        report.Flags.HasFlag(ProjectFlags.NameConflict).ShouldBeFalse();
    }

    [Fact]
    public void PresumedCreatorIsEarliestMaster()
    {
        var report = AnalyzeSingle(
            // Мастерское действие второго мастера — позже.
            Notification("ClaimCommentId(1001-1-5)", "Дюна: Пол, игрок Иван", SecondMasterId, PlayerId, Day3, masterInitiated: true),
            // Новая заявка: получатель — мастер.
            Notification("ClaimCommentId(1001-1-1)", "Дюна: Пол, игрок Иван", PlayerId, MasterId, Day2, newClaim: true),
            // Комментарий игрока — не свидетельство.
            Notification("ClaimCommentId(1001-1-2)", "Дюна: Пол, игрок Иван", PlayerId, SecondMasterId, Day1),
            // Админ действует в проекте раньше всех, робот тоже — не считаются.
            Notification("ClaimCommentId(1001-1-3)", "Дюна: Пол, игрок Иван", AdminId, PlayerId, Day1, masterInitiated: true),
            Notification("Project(1001)", "Дюна: проект будет закрыт из-за неактивности", RobotId, MasterId, Day1));

        report.CreatorUserId.ShouldBe(MasterId);
        report.CreatorMethod.ShouldBe(CreatorMethod.Presumed);
        report.OtherCreatorCandidates.ShouldBe([new CreatorCandidate(SecondMasterId, 1, Day3)]);
    }

    [Fact]
    public void PlotAndProjectMailInitiatorsAreMasters()
    {
        var candidates = CreateAnalyzer().GetCreatorCandidates(
        [
            Notification("PlotElementId(1001-1-2)", "Дюна: опубликована вводная", SecondMasterId, PlayerId, Day1),
            Notification("Project(1001)", "Дюна: Сбор", MasterId, PlayerId, Day2),
        ]);

        candidates.ShouldBe([new CreatorCandidate(SecondMasterId, 1, Day1), new CreatorCandidate(MasterId, 1, Day2)]);
    }

    [Fact]
    public void NoMasterEvidenceMeansCreatorNotFound()
    {
        var report = AnalyzeSingle(Notification("ClaimCommentId(1001-1-2)", "Дюна: Пол, игрок Иван", PlayerId, MasterId, Day1));

        report.CreatorUserId.ShouldBeNull();
        report.CreatorMethod.ShouldBe(CreatorMethod.NotFound);
        report.Flags.HasFlag(ProjectFlags.CreatorNotFound).ShouldBeTrue();
    }

    [Fact]
    public void CreatorMissingInMssqlIsFlagged()
    {
        var report = AnalyzeSingle(AdminNotification(1001, "Дюна", 9999, "Мастер указал, что игра есть на КогдаИгре."));

        report.CreatorUserId.ShouldBe(9999);
        report.CreatorEmail.ShouldBeNull();
        report.Flags.HasFlag(ProjectFlags.CreatorMissingInMssql).ShouldBeTrue();
    }

    [Fact]
    public void IdNotAboveBackupMaxWithoutProjectIsContradiction()
    {
        var report = AnalyzeSingle(Notification("ClaimId(700-1)", "Пропавший: Пол, игрок Иван"));

        report.Flags.HasFlag(ProjectFlags.IdNotAboveBackupMax).ShouldBeTrue();
    }

    [Fact]
    public void ProjectAboveBackupMaxAlreadyInMssqlIsFlagged()
    {
        var report = AnalyzeSingle(Notification("ClaimId(1500-1)", "Новый: Пол, игрок Иван"));

        report.MssqlName.ShouldBe("Оболочка");
        report.Flags.HasFlag(ProjectFlags.AlreadyInMssql).ShouldBeTrue();
        report.Flags.HasFlag(ProjectFlags.IdNotAboveBackupMax).ShouldBeFalse();
    }

    [Fact]
    public void ProjectFromBackupIsNotReported()
        => CreateAnalyzer().Analyze([Notification("ClaimId(500-1)", "Старый проект: Пол, игрок Иван")], logs: null).ShouldBeEmpty();

    [Fact]
    public void LogsAddProjectsAndUsers()
    {
        LogEntry[] logs =
        [
            new(1001, "master@example.com", "/1001/character/5/edit", null, Day1),
            new(null, "MASTER@example.com", "/1001/claim/7", null, Day3),
            new(1002, "unknown@example.com", null, null, Day2),
            new(null, "player@example.com", "/500/claim/1", null, Day2),
            new(null, null, "/account/login", null, Day2),
        ];

        var reports = CreateAnalyzer().Analyze(
            [AdminNotification(1001, "Дюна", MasterId, "Мастер указал, что игра есть на КогдаИгре.", Day2)],
            logs);

        reports.Select(r => r.ProjectId).ShouldBe([1001, 1002]);

        var dune = reports[0];
        dune.LogRequests.ShouldBe(2);
        dune.LogUsers.ShouldBe([$"{MasterId} (2)"]);
        dune.CreatorInLogs.ShouldBe(true);
        dune.FirstActivity.ShouldBe(Day1);
        dune.LastActivity.ShouldBe(Day3);

        var onlyInLogs = reports[1];
        onlyInLogs.Notifications.ShouldBe(0);
        onlyInLogs.LogUsers.ShouldBe(["unknown@example.com (1, нет в MSSQL)"]);
        onlyInLogs.Flags.HasFlag(ProjectFlags.NameNotFound).ShouldBeTrue();
        onlyInLogs.Flags.HasFlag(ProjectFlags.CreatorNotFound).ShouldBeTrue();
        onlyInLogs.CreatorInLogs.ShouldBeNull();
    }

    [Fact]
    public void CreatorInLogsIsUnknownWithoutLogs()
        => AnalyzeSingle(AdminNotification(1001, "Дюна", MasterId, "Мастер указал, что игра есть на КогдаИгре."))
            .CreatorInLogs.ShouldBeNull();

    [Theory]
    [InlineData("/1001/character/5", 1001)]
    [InlineData("1001", 1001)]
    [InlineData("/1001?x=1", 1001)]
    [InlineData("/account/login", null)]
    [InlineData("/0/claim", null)]
    [InlineData("", null)]
    public void ProjectIdFromRequestPath(string path, int? expected)
        => LostProjectAnalyzer.TryGetProjectIdFromPath(path).ShouldBe(expected);
}
