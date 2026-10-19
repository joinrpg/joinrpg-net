namespace JoinRpg.Tools.RestoreLostProjects.Test;

public class ProjectHeaderParserTest
{
    private static (string, HeaderKind)[] Candidates(string header, bool mayBeMassMail = false)
        => [.. ProjectHeaderParser.GetNameCandidates(header, mayBeMassMail)];

    [Theory]
    [InlineData("Новый проект «Дюна» — статус КогдаИгры", "Дюна")]
    [InlineData("Новый проект «Дюна: Пробуждение» — статус КогдаИгры", "Дюна: Пробуждение")]
    [InlineData("Новый проект «Игра «Ведьмак»» — статус КогдаИгры", "Игра «Ведьмак»")]
    public void ParsesAdminNewProjectHeader(string header, string expected)
        => ProjectHeaderParser.TryParseAdminNewProject(header).ShouldBe(expected);

    [Theory]
    [InlineData("Дюна: тема на форуме Сбор")]
    [InlineData("Новый проект «» — статус КогдаИгры")]
    [InlineData("Новый проект «Дюна»")]
    public void OtherHeadersAreNotAdminNewProject(string header)
        => ProjectHeaderParser.TryParseAdminNewProject(header).ShouldBeNull();

    [Fact]
    public void AdminHeaderGivesNoPrefixCandidates()
        => Candidates("Новый проект «Дюна: Пробуждение» — статус КогдаИгры", mayBeMassMail: true).ShouldBeEmpty();

    [Fact]
    public void ClaimHeader()
        => Candidates("Дюна: Пол Атрейдес, игрок Иван Иванов").ShouldBe([("Дюна", HeaderKind.Claim)]);

    [Fact]
    public void ClaimHeaderWithColonInProjectNameIsAmbiguousAlone()
        => Candidates("Дюна: Пробуждение: Пол Атрейдес, игрок Иван")
            .ShouldBe([("Дюна", HeaderKind.Claim), ("Дюна: Пробуждение", HeaderKind.Claim)]);

    [Fact]
    public void ForumHeaderCurrentFormat()
        => Candidates("Дюна: тема на форуме Сбор: место и время").ShouldBe([("Дюна", HeaderKind.Forum)]);

    [Fact]
    public void ForumHeaderWithColonInProjectName()
        => Candidates("Дюна: Пробуждение: тема на форуме Сбор").ShouldBe([("Дюна: Пробуждение", HeaderKind.Forum)]);

    [Fact]
    public void ForumHeaderLegacyFormatBefore20260707()
        => Candidates("ProjectName(Дюна: Пробуждение): тема на форуме Сбор")
            .ShouldBe([("Дюна: Пробуждение", HeaderKind.Forum)]);

    [Fact]
    public void PlotHeaderLegacyFormat()
        => Candidates("ProjectName(Дюна): опубликована вводная").ShouldBe([("Дюна", HeaderKind.Plot)]);

    [Fact]
    public void PlotHeaderCurrentFormat()
        => Candidates("Дюна: опубликована вводная").ShouldBe([("Дюна", HeaderKind.Plot)]);

    [Fact]
    public void RoomHeader()
        => Candidates("Дюна: комната Палатка 12").ShouldBe([("Дюна", HeaderKind.Room)]);

    [Fact]
    public void InvitesHeader()
        => Candidates("Дюна: приглашения к проживанию").ShouldBe([("Дюна", HeaderKind.Invites)]);

    [Theory]
    [InlineData("Дюна: проект закрыт")]
    [InlineData("Дюна: проект будет закрыт из-за неактивности")]
    public void ProjectClosedHeader(string header)
        => Candidates(header, mayBeMassMail: true).ShouldBe([("Дюна", HeaderKind.ProjectClosed)]);

    [Fact]
    public void MassMailGivesEveryPrefix()
        => Candidates("Дюна: Важно: сбор в пятницу", mayBeMassMail: true)
            .ShouldBe([("Дюна", HeaderKind.MassMail), ("Дюна: Важно", HeaderKind.MassMail)]);

    [Fact]
    public void KnownTailWinsOverMassMail()
        => Candidates("Дюна: Пробуждение: опубликована вводная", mayBeMassMail: true)
            .ShouldBe([("Дюна: Пробуждение", HeaderKind.Plot)]);

    [Fact]
    public void UnknownTailIsIgnoredWhenNotMassMail()
        => Candidates("Важно: сбор в пятницу").ShouldBeEmpty();

    [Theory]
    [InlineData("2026-09-27T23:59:59Z", true, false)]
    [InlineData("2026-09-28T00:00:00Z", true, true)]
    [InlineData("2026-10-01T00:00:00Z", false, false)]
    public void MassMailOnlyAfter20260928AndOnProject(string createdAt, bool referencesProject, bool expected)
        => ProjectHeaderParser.MayBeMassMail(DateTimeOffset.Parse(createdAt, System.Globalization.CultureInfo.InvariantCulture), referencesProject)
            .ShouldBe(expected);
}
