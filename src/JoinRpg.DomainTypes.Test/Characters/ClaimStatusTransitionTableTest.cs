using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.DomainTypes.Test.Characters;

/// <summary>
/// Таблица переходов статуса заявки и «статус после ответа» (ADR014, §4).
/// </summary>
public class ClaimStatusTransitionTableTest
{
    private static readonly ClaimStatus[] AllStatuses = Enum.GetValues<ClaimStatus>();

    /// <summary>
    /// Разрешённые переходы, выписанные явно — это спецификация, а не пересказ реализации. Здесь
    /// таблица зафиксирована, и её изменение обязано быть осознанным — тест упадёт.
    /// </summary>
    private static readonly (ClaimStatus From, ClaimStatus To)[] AllowedTransitions =
    [
        // Утвердить можно поданную, обсуждаемую и — при выходе на вторую роль — зарегистрированную.
        (ClaimStatus.AddedByUser, ClaimStatus.Approved),
        (ClaimStatus.Discussed, ClaimStatus.Approved),
        (ClaimStatus.CheckedIn, ClaimStatus.Approved),

        // В лист ожидания — из любой «живой» необработанной.
        (ClaimStatus.AddedByUser, ClaimStatus.OnHold),
        (ClaimStatus.Discussed, ClaimStatus.OnHold),
        (ClaimStatus.AddedByMaster, ClaimStatus.OnHold),

        // Восстановление отклонённой или снятой с ожидания: RestoreByMaster переводит
        // в AddedByMaster.
        (ClaimStatus.DeclinedByUser, ClaimStatus.AddedByMaster),
        (ClaimStatus.DeclinedByMaster, ClaimStatus.AddedByMaster),
        (ClaimStatus.OnHold, ClaimStatus.AddedByMaster),

        // А эти три таблица разрешает, но выполнить их некому: AddedByUser ставится только при
        // создании заявки, переходом — никогда. Выписаны, потому что тест обязан описывать таблицу
        // как есть; удалять правило — отдельное решение.
        (ClaimStatus.DeclinedByUser, ClaimStatus.AddedByUser),
        (ClaimStatus.DeclinedByMaster, ClaimStatus.AddedByUser),
        (ClaimStatus.OnHold, ClaimStatus.AddedByUser),

        // Отклонить (игроком или мастером) можно всё, кроме уже отклонённой и зарегистрированной.
        (ClaimStatus.AddedByUser, ClaimStatus.DeclinedByUser),
        (ClaimStatus.Discussed, ClaimStatus.DeclinedByUser),
        (ClaimStatus.AddedByMaster, ClaimStatus.DeclinedByUser),
        (ClaimStatus.Approved, ClaimStatus.DeclinedByUser),
        (ClaimStatus.OnHold, ClaimStatus.DeclinedByUser),
        (ClaimStatus.AddedByUser, ClaimStatus.DeclinedByMaster),
        (ClaimStatus.Discussed, ClaimStatus.DeclinedByMaster),
        (ClaimStatus.AddedByMaster, ClaimStatus.DeclinedByMaster),
        (ClaimStatus.Approved, ClaimStatus.DeclinedByMaster),
        (ClaimStatus.OnHold, ClaimStatus.DeclinedByMaster),

        // Обсуждение: комментарий переводит поданную заявку в «обсуждается».
        (ClaimStatus.AddedByUser, ClaimStatus.Discussed),
        (ClaimStatus.Discussed, ClaimStatus.Discussed),
        (ClaimStatus.AddedByMaster, ClaimStatus.Discussed),

        // Зарегистрировать можно только утверждённую.
        (ClaimStatus.Approved, ClaimStatus.CheckedIn),
    ];

    public static TheoryData<ClaimStatus, ClaimStatus> Allowed()
    {
        var data = new TheoryData<ClaimStatus, ClaimStatus>();
        foreach (var (from, to) in AllowedTransitions)
        {
            data.Add(from, to);
        }
        return data;
    }

    /// <summary>Все остальные пары — то есть дополнение спецификации до полного квадрата.</summary>
    public static TheoryData<ClaimStatus, ClaimStatus> Forbidden()
    {
        var allowed = AllowedTransitions.ToHashSet();
        var data = new TheoryData<ClaimStatus, ClaimStatus>();
        foreach (var from in AllStatuses)
        {
            foreach (var to in AllStatuses)
            {
                if (!allowed.Contains((from, to)))
                {
                    data.Add(from, to);
                }
            }
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(Allowed))]
    public void CanChangeTo_AllowsTransition(ClaimStatus from, ClaimStatus to) => from.CanChangeTo(to).ShouldBeTrue();

    [Theory]
    [MemberData(nameof(Forbidden))]
    public void CanChangeTo_RejectsTransition(ClaimStatus from, ClaimStatus to) => from.CanChangeTo(to).ShouldBeFalse();

    [Fact]
    public void CanChangeTo_UnknownTarget_Throws()
        => _ = Should.Throw<ArgumentOutOfRangeException>(() => ClaimStatus.AddedByUser.CanChangeTo((ClaimStatus)100500));

    /// <summary>
    /// Ответ, который выводит заявку в «обсуждается», выписан явно; всё остальное статус не меняет.
    /// </summary>
    private static readonly (ClaimStatus Status, bool ByPlayer, bool IsVisibleToPlayer)[] RepliesThatStartDiscussion =
    [
        // Мастер пригласил — ответ игрока, видимость тут не при чём.
        (ClaimStatus.AddedByMaster, true, true),
        (ClaimStatus.AddedByMaster, true, false),
        // Игрок подал — ответ мастера, но только видимый игроку.
        (ClaimStatus.AddedByUser, false, true),
    ];

    public static TheoryData<ClaimStatus, bool, bool> AllReplies()
    {
        var data = new TheoryData<ClaimStatus, bool, bool>();
        foreach (var status in AllStatuses)
        {
            foreach (var byPlayer in new[] { true, false })
            {
                foreach (var isVisibleToPlayer in new[] { true, false })
                {
                    data.Add(status, byPlayer, isVisibleToPlayer);
                }
            }
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(AllReplies))]
    public void AfterReply_MovesToDiscussedOnlyOnOppositeSideReply(ClaimStatus status, bool byPlayer, bool isVisibleToPlayer)
    {
        var expected = RepliesThatStartDiscussion.Contains((status, byPlayer, isVisibleToPlayer))
            ? ClaimStatus.Discussed
            : status;

        status.AfterReply(byPlayer, isVisibleToPlayer).ShouldBe(expected);
    }
}
