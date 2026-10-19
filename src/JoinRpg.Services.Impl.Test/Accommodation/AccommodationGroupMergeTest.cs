using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.Services.Impl.Test.Accommodation;

/// <summary>
/// Мастер поселяет игроков вместе — объединяет группы проживания сразу, без приглашения (#5656).
/// </summary>
public class AccommodationGroupMergeTest : AccommodationInviteTestBase
{
    [Fact]
    public async Task MergeWithClaim_MovesItIntoGroup()
    {
        var host = CreateClaimWithAccommodation("Хозяин");
        var target = CreateClaimWithAccommodation("Гость");
        var targetGroup = target.AccommodationRequest!;

        await CreateMasterService().MergeAccommodationGroups(host.GetId(), AccommodationGroupIdentification.From(target.GetId()));

        MembersOf(host.AccommodationRequest!).ShouldBe([host.ClaimId, target.ClaimId], ignoreOrder: true);
        target.AccommodationRequest.ShouldBe(host.AccommodationRequest);
        mock.AccommodationRequests.ShouldNotContain(targetGroup);
        SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task MergeWithGroup_MovesWholeGroup()
    {
        var host = CreateClaimWithAccommodation("Хозяин");
        var first = CreateClaim("Гость1");
        var second = CreateClaim("Гость2");
        var targetGroup = mock.CreateAccommodationRequest(accommodationType, first, second);

        await CreateMasterService().MergeAccommodationGroups(host.GetId(), AccommodationGroupIdentification.From(RequestId(targetGroup)));

        MembersOf(host.AccommodationRequest!)
            .ShouldBe([host.ClaimId, first.ClaimId, second.ClaimId], ignoreOrder: true);
        mock.AccommodationRequests.ShouldNotContain(targetGroup);
    }

    /// <summary>
    /// Приглашается заявка, у которой есть соседи, — переезжает вся её группа, как и при приёме
    /// приглашения.
    /// </summary>
    [Fact]
    public async Task MergeWithClaimInGroup_MovesItsNeighboursToo()
    {
        var host = CreateClaimWithAccommodation("Хозяин");
        var target = CreateClaim("Гость");
        var neighbour = CreateClaim("Сосед гостя");
        _ = mock.CreateAccommodationRequest(accommodationType, target, neighbour);

        await CreateMasterService().MergeAccommodationGroups(host.GetId(), AccommodationGroupIdentification.From(target.GetId()));

        MembersOf(host.AccommodationRequest!)
            .ShouldBe([host.ClaimId, target.ClaimId, neighbour.ClaimId], ignoreOrder: true);
    }

    [Fact]
    public async Task MergeWithClaimWithoutType_MovesIt()
    {
        var host = CreateClaimWithAccommodation("Хозяин");
        var target = CreateClaim("Гость без типа");

        await CreateMasterService().MergeAccommodationGroups(host.GetId(), AccommodationGroupIdentification.From(target.GetId()));

        MembersOf(host.AccommodationRequest!).ShouldBe([host.ClaimId, target.ClaimId], ignoreOrder: true);
    }

    /// <summary>Объединение не создаёт приглашений — в этом и смысл.</summary>
    [Fact]
    public async Task Merge_CreatesNoInvites()
    {
        var host = CreateClaimWithAccommodation("Хозяин");
        var target = CreateClaimWithAccommodation("Гость");

        await CreateMasterService().MergeAccommodationGroups(host.GetId(), AccommodationGroupIdentification.From(target.GetId()));

        mock.AccommodationInvites.ShouldBeEmpty();
        notificationService.Invites.ShouldBeEmpty();
    }

    /// <summary>
    /// Висящие приглашения закрываются так же, как при приёме: между новыми соседями — сбывшимися,
    /// входящие со стороны — отклонёнными.
    /// </summary>
    [Fact]
    public async Task Merge_ResolvesPendingInvites()
    {
        var host = CreateClaimWithAccommodation("Хозяин");
        var target = CreateClaimWithAccommodation("Гость");
        var outsider = CreateClaimWithAccommodation("Сторонний");
        var between = mock.CreateAccommodationInvite(target, host);
        var fromOutside = mock.CreateAccommodationInvite(outsider, target);

        await CreateMasterService().MergeAccommodationGroups(host.GetId(), AccommodationGroupIdentification.From(target.GetId()));

        between.IsAccepted.ShouldBe(InviteState.Accepted);
        between.ResolveDescription.ShouldBe(ResolveDescription.AcceptedAuto);
        fromOutside.IsAccepted.ShouldBe(InviteState.Declined);
        fromOutside.ResolveDescription.ShouldBe(ResolveDescription.DeclinedWithAcceptOther);
    }

    [Fact]
    public async Task Merge_NotifiesWholeResultingGroupAfterSave()
    {
        var host = CreateClaimWithAccommodation("Хозяин");
        var target = CreateClaimWithAccommodation("Гость");

        await CreateMasterService().MergeAccommodationGroups(host.GetId(), AccommodationGroupIdentification.From(target.GetId()));

        var notification = notificationService.GroupMerged.ShouldHaveSingleItem();
        notification.Members.Select(claimId => claimId.ClaimId)
            .ShouldBe([host.ClaimId, target.ClaimId], ignoreOrder: true);
        notification.Initiator.UserId.Value.ShouldBe(mock.Master.UserId);
        NotificationWentAfterSave();
    }

    /// <summary>
    /// Игрок утверждённой заявки может менять своё проживание, но объединять группы — нет: операция
    /// переселяет чужие заявки. Ему остаётся приглашение.
    /// </summary>
    [Fact]
    public async Task Merge_ByPlayer_Throws()
    {
        var host = CreateClaimWithAccommodation("Хозяин");
        var target = CreateClaimWithAccommodation("Гость");

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).MergeAccommodationGroups(
                host.GetId(), AccommodationGroupIdentification.From(target.GetId())));

        MembersOf(target.AccommodationRequest!).ShouldHaveSingleItem().ShouldBe(target.ClaimId);
        SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Ответственный мастер без права расселять игроков объединять группы не может — за заявки цели
    /// он не отвечает.
    /// </summary>
    [Fact]
    public async Task Merge_ByResponsibleMasterWithoutPermission_Throws()
    {
        var host = CreateClaimWithAccommodation("Хозяин");
        var target = CreateClaimWithAccommodation("Гость");
        host.ResponsibleMasterUserId.ShouldBe(mock.Master.UserId);
        mock.Project.ProjectAcls.Single(acl => acl.UserId == mock.Master.UserId).CanSetPlayersAccommodations = false;
        mock.ReInitProjectInfo();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Master.UserId).MergeAccommodationGroups(
                host.GetId(), AccommodationGroupIdentification.From(target.GetId())));

        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Merge_IntoFullRoom_ThrowsAndChangesNothing()
    {
        var smallType = mock.CreateAccommodationType("Одноместка", capacity: 1);
        mock.ReInitProjectInfo();
        var host = CreateClaim("Хозяин");
        var hostGroup = mock.CreateAccommodationRequest(smallType, host);
        var target = CreateClaim("Гость");
        var targetGroup = mock.CreateAccommodationRequest(smallType, target);

        var exception = await Should.ThrowAsync<AccommodationInviteNotAllowedException>(
            () => CreateMasterService().MergeAccommodationGroups(
                host.GetId(), AccommodationGroupIdentification.From(target.GetId())));

        exception.Message.ShouldContain("не хватает мест");
        NoInviteWording(exception);
        MembersOf(hostGroup).ShouldHaveSingleItem().ShouldBe(host.ClaimId);
        MembersOf(targetGroup).ShouldHaveSingleItem().ShouldBe(target.ClaimId);
        SaveChangesCallCount.ShouldBe(0);
        notificationService.GroupMerged.ShouldBeEmpty();
    }

    [Fact]
    public async Task Merge_WithGroupOfOtherType_Throws()
    {
        var otherType = mock.CreateAccommodationType("Шатёр", capacity: 4);
        mock.ReInitProjectInfo();
        var host = CreateClaimWithAccommodation("Хозяин");
        var target = CreateClaim("Гость");
        _ = mock.CreateAccommodationRequest(otherType, target);

        var exception = await Should.ThrowAsync<AccommodationInviteNotAllowedException>(
            () => CreateMasterService().MergeAccommodationGroups(
                host.GetId(), AccommodationGroupIdentification.From(target.GetId())));

        exception.Message.ShouldContain("такой же тип проживания");
        NoInviteWording(exception);
        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Merge_WithOwnGroup_Throws()
    {
        var host = CreateClaim("Хозяин");
        var neighbour = CreateClaim("Сосед");
        var group = mock.CreateAccommodationRequest(accommodationType, host, neighbour);

        var exception = await Should.ThrowAsync<AccommodationInviteNotAllowedException>(
            () => CreateMasterService().MergeAccommodationGroups(
                host.GetId(), AccommodationGroupIdentification.From(RequestId(group))));

        exception.Message.ShouldContain("уже живёт");
        NoInviteWording(exception);
        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Merge_WithSelf_Throws()
    {
        var host = CreateClaimWithAccommodation("Хозяин");

        var exception = await Should.ThrowAsync<AccommodationInviteNotAllowedException>(
            () => CreateMasterService().MergeAccommodationGroups(
                host.GetId(), AccommodationGroupIdentification.From(host.GetId())));

        exception.Message.ShouldContain("уже живёт");
        NoInviteWording(exception);
        SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>Сосед по своей группе уже живёт вместе — объединять нечего.</summary>
    [Fact]
    public async Task Merge_WithOwnNeighbour_Throws()
    {
        var host = CreateClaim("Хозяин");
        var neighbour = CreateClaim("Сосед");
        _ = mock.CreateAccommodationRequest(accommodationType, host, neighbour);

        var exception = await Should.ThrowAsync<AccommodationInviteNotAllowedException>(
            () => CreateMasterService().MergeAccommodationGroups(
                host.GetId(), AccommodationGroupIdentification.From(neighbour.GetId())));

        exception.Message.ShouldContain("уже живёт");
        NoInviteWording(exception);
        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Merge_HostWithoutType_Throws()
    {
        var host = CreateClaim("Хозяин без типа");
        var target = CreateClaimWithAccommodation("Гость");

        var exception = await Should.ThrowAsync<AccommodationInviteNotAllowedException>(
            () => CreateMasterService().MergeAccommodationGroups(
                host.GetId(), AccommodationGroupIdentification.From(target.GetId())));

        exception.Message.ShouldContain("не выбран тип");
        NoInviteWording(exception);
        SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Расселённых по комнатам не объединяем: это отдельная операция на странице комнат.
    /// </summary>
    [Fact]
    public async Task Merge_WithTargetSettledInRoom_Throws()
    {
        var host = CreateClaimWithAccommodation("Хозяин");
        var target = CreateClaimWithAccommodation("Гость");
        _ = mock.CreateRoom(target.AccommodationRequest!, "Домик №1");

        var exception = await Should.ThrowAsync<AccommodationInviteNotAllowedException>(
            () => CreateMasterService().MergeAccommodationGroups(
                host.GetId(), AccommodationGroupIdentification.From(target.GetId())));

        exception.Message.ShouldContain("расселён");
        NoInviteWording(exception);
        MembersOf(target.AccommodationRequest!).ShouldHaveSingleItem().ShouldBe(target.ClaimId);
        SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// В уведомлении — весь итоговый состав: и прежние соседи хозяина, и соседи цели, переехавшие с
    /// ней.
    /// </summary>
    [Fact]
    public async Task Merge_NotifiesNeighboursOfBothSides()
    {
        var host = CreateClaim("Хозяин");
        var hostNeighbour = CreateClaim("Сосед хозяина");
        _ = mock.CreateAccommodationRequest(accommodationType, host, hostNeighbour);
        var target = CreateClaim("Гость");
        var targetNeighbour = CreateClaim("Сосед гостя");
        _ = mock.CreateAccommodationRequest(accommodationType, target, targetNeighbour);

        await CreateMasterService().MergeAccommodationGroups(host.GetId(), AccommodationGroupIdentification.From(target.GetId()));

        notificationService.GroupMerged.ShouldHaveSingleItem().Members.Select(claimId => claimId.ClaimId)
            .ShouldBe([host.ClaimId, hostNeighbour.ClaimId, target.ClaimId, targetNeighbour.ClaimId], ignoreOrder: true);
    }

    [Fact]
    public async Task Merge_InArchivedProject_Throws()
    {
        var host = CreateClaimWithAccommodation("Хозяин");
        var target = CreateClaimWithAccommodation("Гость");
        ArchiveProject();

        _ = await Should.ThrowAsync<ProjectDeactivatedException>(
            () => CreateMasterService().MergeAccommodationGroups(
                host.GetId(), AccommodationGroupIdentification.From(target.GetId())));

        SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Мастер не приглашает, а поселяет вместе — и отказ ему не должен говорить о приглашении (#5656).
    /// </summary>
    private static void NoInviteWording(Exception exception)
        => exception.Message.ShouldNotContain("приглаш", Case.Insensitive);

    private IReadOnlyCollection<int> MembersOf(AccommodationRequest group)
        => [.. mock.Project.Claims
            .Where(claim => claim.AccommodationRequest_Id == group.Id)
            .Select(claim => claim.ClaimId)];
}
