namespace JoinRpg.Services.Drp.Test;

public class CharacterNameMismatchStatsTest
{
    private static readonly ProjectIdentification Project = new(1);
    private static readonly DateTimeOffset At = new(2026, 5, 8, 0, 0, 0, TimeSpan.Zero);

    private static ClaimNotificationEvent Event(
        int claimId, string? characterName, string? playerName, int minutes = 0, CommentExtraAction? action = null)
        => new(new ClaimIdentification(Project, claimId), new ClaimIdentification(Project, claimId), At.AddMinutes(minutes),
            new UserIdentification(1), ClaimNotificationKind.ClaimChange,
            characterName, playerName, action, ClaimEventAuthor.Master, null, null, "", []);

    private static ClaimNotificationEvent Approved(int claimId, string characterName, string playerName, int minutes = 0)
        => Event(claimId, characterName, playerName, minutes, CommentExtraAction.ApproveByMaster);

    private static IEnumerable<ClaimNotificationEvent> Claims(int matching, int mismatched)
        => [.. Enumerable.Range(1, matching).Select(i => Approved(i, $"Игрок {i}", $"Игрок {i}")),
            .. Enumerable.Range(1000, mismatched).Select(i => Approved(i, $"Персонаж {i}", $"Игрок {i}"))];

    [Fact]
    public void CountsApprovedClaimsAndMismatches()
        => CharacterNameMismatchStats.FromEvents(Claims(matching: 3, mismatched: 2))
            .ShouldBe(new CharacterNameMismatchStats(5, 2));

    [Theory]
    [InlineData(9, 1, false)]
    [InlineData(8, 1, true)]
    [InlineData(18, 2, false)]
    [InlineData(0, 1, true)]
    [InlineData(5, 0, false)]
    [InlineData(0, 0, false)]
    public void SuspiciousAboveTenPercent(int matching, int mismatched, bool suspicious)
        => CharacterNameMismatchStats.FromEvents(Claims(matching, mismatched)).IsSuspicious.ShouldBe(suspicious);

    /// <summary>У непринятой заявки персонаж (часто шаблон) носит своё имя и без поля-имени.</summary>
    [Fact]
    public void NotApprovedClaimsAreIgnored()
        => CharacterNameMismatchStats.FromEvents([
                Event(1, "Шаблон эльфа", "Игрок", action: CommentExtraAction.NewClaim),
                Event(1, "Шаблон эльфа", "Игрок", minutes: 5),
                Event(2, "Персонаж", "Игрок 2")])
            .ShouldBe(new CharacterNameMismatchStats(0, 0));

    /// <summary>Вторая роль тоже пересчитывает имя из полей.</summary>
    [Fact]
    public void SecondRoleCounts()
        => CharacterNameMismatchStats.FromEvents([Event(1, "Персонаж", "Игрок", action: CommentExtraAction.SecondRole)])
            .ShouldBe(new CharacterNameMismatchStats(1, 1));

    /// <summary>
    /// После принятия имя персонажа может устареть: перенос заявки на персонажа со своим именем,
    /// смена отображаемого имени игрока. Решает уведомление о принятии.
    /// </summary>
    [Fact]
    public void EventsAfterApprovalAreIgnored()
        => CharacterNameMismatchStats.FromEvents([
                Approved(1, "Игрок", "Игрок"),
                Event(1, "Гэндальф", "Игрок", minutes: 5, action: CommentExtraAction.MoveByMaster),
                Event(1, "Игрок", "Иван Петров", minutes: 10)])
            .ShouldBe(new CharacterNameMismatchStats(1, 0));

    [Fact]
    public void LastApprovalOfClaimDecides()
        => CharacterNameMismatchStats.FromEvents([Approved(1, "Игрок", "Игрок", minutes: 10), Approved(1, "Персонаж", "Игрок")])
            .ShouldBe(new CharacterNameMismatchStats(1, 0));

    [Fact]
    public void ApprovalsWithoutNamesAreIgnored()
        => CharacterNameMismatchStats.FromEvents([Approved(1, "Персонаж", " "), Approved(2, "Персонаж", "Игрок")])
            .ShouldBe(new CharacterNameMismatchStats(1, 1));

    [Fact]
    public void CaseAndSpacesAreNotMismatch()
        => CharacterNameMismatchStats.FromEvents([Approved(1, " иван петров", "Иван Петров ")])
            .ShouldBe(new CharacterNameMismatchStats(1, 0));
}
