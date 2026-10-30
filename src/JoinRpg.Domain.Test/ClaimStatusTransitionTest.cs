using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Test;

/// <summary>
/// Запись перехода статуса в сущность заявки: статус и отметки дат (ADR014, §4). Сама таблица
/// допустимых переходов проверяется в <c>DomainTypes.Test</c>.
/// </summary>
public class ClaimStatusTransitionTest
{
    private static readonly DateTime Now = new(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EventTime = new(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);

    private readonly MockedProject mock = new();

    private Claim CreateClaim(ClaimStatus status)
    {
        var claim = mock.CreateClaim(mock.CreateCharacter("Вася"), mock.Player);
        claim.ClaimStatus = status;
        return claim;
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
    public void ChangeStatus_StampsExactlyOneExpectedDate(ClaimStatus from, ClaimStatus target)
    {
        var claim = CreateClaim(from);

        claim.ChangeStatus(target, Now);

        claim.ClaimStatus.ShouldBe(target);
        claim.MasterAcceptedDate.ShouldBe(target == ClaimStatus.Approved ? Now : null);
        claim.MasterDeclinedDate.ShouldBe(target == ClaimStatus.DeclinedByMaster ? Now : null);
        claim.PlayerDeclinedDate.ShouldBe(target == ClaimStatus.DeclinedByUser ? Now : null);
        claim.CheckInDate.ShouldBe(target == ClaimStatus.CheckedIn ? Now : null);
        claim.LastUpdateDateTime.ShouldBe(Now);
    }

    [Fact]
    public void ChangeStatus_ForbiddenTransition_WritesNothing()
    {
        var claim = CreateClaim(ClaimStatus.DeclinedByUser);
        var lastUpdate = claim.LastUpdateDateTime;

        _ = Should.Throw<ClaimWrongStatusException>(() => claim.ChangeStatus(ClaimStatus.Approved, Now));

        claim.ClaimStatus.ShouldBe(ClaimStatus.DeclinedByUser);
        claim.MasterAcceptedDate.ShouldBeNull();
        claim.LastUpdateDateTime.ShouldBe(lastUpdate);
    }

    [Fact]
    public void ChangeStatus_UnknownStatus_Throws()
        => _ = Should.Throw<ArgumentOutOfRangeException>(() => CreateClaim(ClaimStatus.AddedByUser).ChangeStatus((ClaimStatus)100500, Now));

    /// <summary>
    /// Возврат «вторая роль → утверждена» не должен затирать <c>MasterAcceptedDate</c> — он виден
    /// в отчётах.
    /// </summary>
    [Fact]
    public void ChangeStatusKeepingTimestamps_ChangesStatus_ButNotDates()
    {
        var claim = CreateClaim(ClaimStatus.CheckedIn);
        claim.MasterAcceptedDate = EventTime;
        var lastUpdate = claim.LastUpdateDateTime;

        claim.ChangeStatusKeepingTimestamps(ClaimStatus.Approved);

        claim.ClaimStatus.ShouldBe(ClaimStatus.Approved);
        claim.MasterAcceptedDate.ShouldBe(EventTime);
        claim.LastUpdateDateTime.ShouldBe(lastUpdate);
    }

    [Fact]
    public void ChangeStatusKeepingTimestamps_ChecksTransitionTable()
        => _ = Should.Throw<ClaimWrongStatusException>(
            () => CreateClaim(ClaimStatus.DeclinedByUser).ChangeStatusKeepingTimestamps(ClaimStatus.Approved));

    [Fact]
    public void EnsureCanChangeStatus_ChecksOnly_AndDoesNotWrite()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);

        claim.EnsureCanChangeStatus(ClaimStatus.Approved);

        claim.ClaimStatus.ShouldBe(ClaimStatus.AddedByUser);
        claim.MasterAcceptedDate.ShouldBeNull();
    }

    /// <summary>
    /// Восстановление повторяет случившееся: таблица переходов не проверяется, отметка — время
    /// события, а время изменения заявки — время восстановления.
    /// </summary>
    [Fact]
    public void RestoreStatus_SkipsTable_AndStampsEventTime()
    {
        var claim = CreateClaim(ClaimStatus.DeclinedByUser);

        claim.RestoreStatus(ClaimStatus.Approved, EventTime, Now);

        claim.ClaimStatus.ShouldBe(ClaimStatus.Approved);
        claim.MasterAcceptedDate.ShouldBe(EventTime);
        claim.LastUpdateDateTime.ShouldBe(Now);
    }

    [Theory]
    [InlineData(ClaimStatus.Approved, null)]
    [InlineData(ClaimStatus.DeclinedByMaster, ClaimDenialReason.Unavailable)]
    public void RestoreStatus_KeepsDenialReasonOnlyForDeclinedByMaster(ClaimStatus target, ClaimDenialReason? expected)
    {
        var claim = CreateClaim(ClaimStatus.DeclinedByMaster);
        claim.ClaimDenialStatus = ClaimDenialReason.Unavailable;

        claim.RestoreStatus(target, EventTime, Now);

        claim.ClaimDenialStatus.ShouldBe(expected);
    }

    [Fact]
    public void MarkDiscussed_MasterVisibleReply_MovesToDiscussed_AndTouchesClaim()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);

        claim.MarkDiscussed(mock.Master.UserId, isVisibleToPlayer: true, Now);

        claim.ClaimStatus.ShouldBe(ClaimStatus.Discussed);
        claim.LastUpdateDateTime.ShouldBe(Now);
    }

    [Fact]
    public void MarkDiscussed_PlayerReplyToInvitation_MovesToDiscussed()
    {
        var claim = CreateClaim(ClaimStatus.AddedByMaster);

        claim.MarkDiscussed(mock.Player.UserId, isVisibleToPlayer: false, Now);

        claim.ClaimStatus.ShouldBe(ClaimStatus.Discussed);
    }

    /// <summary>Статус не меняется, но время изменения заявки — всё равно время операции.</summary>
    [Fact]
    public void MarkDiscussed_OwnSideReply_KeepsStatus_ButTouchesClaim()
    {
        var claim = CreateClaim(ClaimStatus.AddedByUser);

        claim.MarkDiscussed(mock.Player.UserId, isVisibleToPlayer: true, Now);

        claim.ClaimStatus.ShouldBe(ClaimStatus.AddedByUser);
        claim.LastUpdateDateTime.ShouldBe(Now);
    }
}
