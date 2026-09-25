using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Services.Impl.Claims;
using JoinRpg.Services.Impl.Projects;

namespace JoinRpg.Services.Impl.Test.Claims;

/// <summary>
/// Тесты единого перехода статуса заявки (ADR014, §4).
/// </summary>
public class ClaimStatusTransitionTest : ClaimServiceTestBase
{
    private static readonly ClaimStatus[] AllStatuses = Enum.GetValues<ClaimStatus>();

    private Claim CreateClaim(ClaimStatus status)
    {
        var character = mock.CreateCharacter("Вася");
        var claim = mock.CreateClaim(character, mock.Player);
        claim.ClaimStatus = status;
        mock.ReInitProjectInfo();
        return claim;
    }

    private Task ChangeStatus(Claim claim, ClaimStatus target)
        => CreatePropsService().ChangeClaim(
            claim.GetId(),
            ClaimAccessRequirement.AnyMaster,
            ProjectActiveRequirement.MustBeActive,
            target,
            ctx => ctx.ChangeStatus(ctx.Claim, ctx.Request));

    private Task ChangeStatusKeepingTimestamps(Claim claim, ClaimStatus target)
        => CreatePropsService().ChangeClaim(
            claim.GetId(),
            ClaimAccessRequirement.AnyMaster,
            ProjectActiveRequirement.MustBeActive,
            target,
            ctx => ctx.ChangeStatusKeepingTimestamps(ctx.Claim, ctx.Request));

    /// <summary>
    /// Разрешённые переходы, выписанные явно — это спецификация, а не пересказ реализации.
    /// Сверять с <c>ClaimExtensions.CanChangeTo</c> в тесте бессмысленно: такая проверка
    /// подтверждает лишь то, что <c>ChangeStatus</c> её вызывает, и молча согласится с любой
    /// ошибкой в самой таблице. Здесь же таблица зафиксирована, и её изменение обязано быть
    /// осознанным — тест упадёт.
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
        // создании заявки, переходом — никогда. Выписаны, потому что миграция переносит таблицу
        // один в один и тест обязан описывать её как есть; удалять правило — отдельное решение.
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
    public async Task ChangeStatus_AllowsTransition(ClaimStatus from, ClaimStatus to)
    {
        var claim = CreateClaim(from);

        await ChangeStatus(claim, to);

        claim.ClaimStatus.ShouldBe(to);
    }

    [Theory]
    [MemberData(nameof(Forbidden))]
    public async Task ChangeStatus_RejectsTransition_AndSavesNothing(ClaimStatus from, ClaimStatus to)
    {
        var claim = CreateClaim(from);

        _ = await Should.ThrowAsync<ClaimWrongStatusException>(() => ChangeStatus(claim, to));

        claim.ClaimStatus.ShouldBe(from);
        SaveChangesCallCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(ClaimStatus.AddedByUser, ClaimStatus.Approved)]
    [InlineData(ClaimStatus.AddedByUser, ClaimStatus.DeclinedByMaster)]
    [InlineData(ClaimStatus.AddedByUser, ClaimStatus.DeclinedByUser)]
    [InlineData(ClaimStatus.Approved, ClaimStatus.CheckedIn)]
    [InlineData(ClaimStatus.AddedByUser, ClaimStatus.OnHold)]
    [InlineData(ClaimStatus.AddedByUser, ClaimStatus.Discussed)]
    [InlineData(ClaimStatus.OnHold, ClaimStatus.AddedByUser)]
    [InlineData(ClaimStatus.OnHold, ClaimStatus.AddedByMaster)]
    public async Task ChangeStatus_StampsExactlyOneExpectedDate(ClaimStatus from, ClaimStatus target)
    {
        var claim = CreateClaim(from);

        await ChangeStatus(claim, target);

        claim.MasterAcceptedDate.HasValue.ShouldBe(target == ClaimStatus.Approved);
        claim.MasterDeclinedDate.HasValue.ShouldBe(target == ClaimStatus.DeclinedByMaster);
        claim.PlayerDeclinedDate.HasValue.ShouldBe(target == ClaimStatus.DeclinedByUser);
        claim.CheckInDate.HasValue.ShouldBe(target == ClaimStatus.CheckedIn);
        claim.LastUpdateDateTime.ShouldNotBe(default);
    }

    /// <summary>
    /// Возврат «вторая роль → утверждена» не должен затирать <c>MasterAcceptedDate</c> — он виден
    /// в отчётах.
    /// </summary>
    [Fact]
    public async Task ChangeStatusKeepingTimestamps_ChangesStatus_ButNotDates()
    {
        var claim = CreateClaim(ClaimStatus.CheckedIn);
        var acceptedDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        claim.MasterAcceptedDate = acceptedDate;

        await ChangeStatusKeepingTimestamps(claim, ClaimStatus.Approved);

        claim.ClaimStatus.ShouldBe(ClaimStatus.Approved);
        claim.MasterAcceptedDate.ShouldBe(acceptedDate);
    }

    [Fact]
    public async Task ChangeStatusKeepingTimestamps_ChecksTransitionTable()
    {
        var claim = CreateClaim(ClaimStatus.DeclinedByUser);

        _ = await Should.ThrowAsync<ClaimWrongStatusException>(
            () => ChangeStatusKeepingTimestamps(claim, ClaimStatus.Approved));
    }

    [Fact]
    public async Task ChangeStatus_UnknownStatus_Throws()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);

        _ = await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () => ChangeStatus(claim, (ClaimStatus)100500));
    }

    [Fact]
    public async Task EnsureCanChangeStatus_ChecksOnly_AndDoesNotWrite()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);

        await CreatePropsService().ChangeClaim(
            claim.GetId(),
            ClaimAccessRequirement.AnyMaster,
            ProjectActiveRequirement.MustBeActive,
            0,
            ctx => ctx.Claim.EnsureCanChangeStatus(ClaimStatus.Approved));

        claim.ClaimStatus.ShouldBe(ClaimStatus.AddedByUser);
        claim.MasterAcceptedDate.ShouldBeNull();
    }

    private Task MarkDiscussed(Claim claim, int currentUserId, bool isVisibleToPlayer)
        => CreatePropsService(currentUserId).ChangeClaim(
            claim.GetId(),
            ClaimAccessRequirement.MasterOrPlayer,
            ProjectActiveRequirement.MustBeActive,
            isVisibleToPlayer,
            ctx => ctx.MarkDiscussed(ctx.Request));

    [Fact]
    public async Task MarkDiscussed_AddedByUser_VisibleMasterComment_MovesToDiscussed()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);

        await MarkDiscussed(claim, mock.Master.UserId, isVisibleToPlayer: true);

        claim.ClaimStatus.ShouldBe(ClaimStatus.Discussed);
    }

    [Fact]
    public async Task MarkDiscussed_AddedByUser_HiddenComment_KeepsStatus()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);

        await MarkDiscussed(claim, mock.Master.UserId, isVisibleToPlayer: false);

        claim.ClaimStatus.ShouldBe(ClaimStatus.AddedByUser);
    }

    [Fact]
    public async Task MarkDiscussed_AddedByMaster_PlayerHimself_MovesToDiscussed()
    {
        var claim = CreateClaim(ClaimStatus.AddedByMaster);

        await MarkDiscussed(claim, mock.Player.UserId, isVisibleToPlayer: true);

        claim.ClaimStatus.ShouldBe(ClaimStatus.Discussed);
    }
}
