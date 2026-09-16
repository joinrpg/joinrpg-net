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

    /// <summary>Все пары «откуда → куда»: страж того, что таблица переходов перенесена без потерь.</summary>
    public static TheoryData<ClaimStatus, ClaimStatus> AllTransitions()
    {
        var data = new TheoryData<ClaimStatus, ClaimStatus>();
        foreach (var from in AllStatuses)
        {
            foreach (var target in AllStatuses)
            {
                data.Add(from, target);
            }
        }
        return data;
    }

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

    [Theory]
    [MemberData(nameof(AllTransitions))]
    public async Task ChangeStatus_AllowsExactlyWhatTransitionTableAllows(ClaimStatus from, ClaimStatus target)
    {
        var claim = CreateClaim(from);

        if (from.CanChangeTo(target))
        {
            await ChangeStatus(claim, target);
            claim.ClaimStatus.ShouldBe(target);
        }
        else
        {
            _ = await Should.ThrowAsync<ClaimWrongStatusException>(() => ChangeStatus(claim, target));
            claim.ClaimStatus.ShouldBe(from);
            SaveChangesCallCount.ShouldBe(0);
        }
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
            ctx => ctx.EnsureCanChangeStatus(ctx.Claim, ClaimStatus.Approved));

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
