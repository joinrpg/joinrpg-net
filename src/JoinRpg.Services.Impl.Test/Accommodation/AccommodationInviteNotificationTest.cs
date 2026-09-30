using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.Services.Impl.Accommodation;

namespace JoinRpg.Services.Impl.Test.Accommodation;

/// <summary>
/// Уведомления контура приглашений к совместному проживанию: с каким видом события и кому именно
/// сервис ставит уведомление — и что ставит строго ПОСЛЕ сохранения (ADR018, §11), иначе игрок
/// получил бы письмо о том, чего в базе не случилось.
/// </summary>
/// <remarks>
/// Сервис ещё не переехал на write-репозитории и ходит в <c>UnitOfWork.GetDbSet</c>, поэтому
/// наборы подключены к <see cref="FakeUnitOfWork"/> явно (см. <c>UseDbSet</c>).
/// </remarks>
public class AccommodationInviteNotificationTest
{
    private readonly MockedProject mock = new();
    private readonly FakeUnitOfWork unitOfWork;
    private readonly FakeAccommodationNotificationService notificationService = new();
    private readonly ProjectAccommodationType accommodationType;

    /// <summary>Сохранения и уведомления в порядке, в котором они случились.</summary>
    private readonly List<string> journal = [];

    public AccommodationInviteNotificationTest()
    {
        mock.Project.Details.EnableAccommodation = true;
        accommodationType = mock.CreateAccommodationType("Домик", capacity: 4);
        mock.ReInitProjectInfo();

        unitOfWork = new FakeUnitOfWork(mock);
        unitOfWork.UseDbSet(mock.Project.Claims);
        unitOfWork.UseDbSet(mock.AccommodationRequests);
        unitOfWork.UseDbSet(mock.AccommodationInvites);

        unitOfWork.OnSaveChanges = _ => journal.Add("save");
        notificationService.OnNotification = () => journal.Add("notification");
    }

    private AccommodationInviteServiceImpl CreateService()
        => new(unitOfWork, notificationService, new FakeCurrentUserAccessor(mock.Master.UserId));

    /// <summary>Заявка игрока с выбранным типом проживания (своя заявка на проживание).</summary>
    private Claim CreateClaimWithAccommodation(string characterName)
    {
        var claim = CreateClaim(characterName);
        _ = mock.CreateAccommodationRequest(accommodationType, claim);
        return claim;
    }

    private Claim CreateClaim(string characterName)
        => mock.CreateApprovedClaim(mock.CreateCharacter(characterName), mock.Player);

    private AccommodationRequestIdentification RequestId(Claim claim)
        => new(mock.ProjectInfo.ProjectId, claim.AccommodationRequest!.Id);

    private AccommodationInviteIdentification InviteId(AccommodationInvite invite)
        => new(mock.ProjectInfo.ProjectId, invite.Id);

    /// <summary>Единственное ушедшее уведомление о приглашении.</summary>
    private AccommodationInviteNotification Notification()
        => notificationService.Invites.ShouldHaveSingleItem();

    private IReadOnlyCollection<int> RecipientClaimIds()
        => [.. Notification().RecipientClaims.Select(claimId => claimId.ClaimId)];

    /// <summary>
    /// Уведомление обязано уйти после всех сохранений: до <c>SaveChanges</c> операции в базе ещё
    /// нет, а откатить отправленное письмо невозможно.
    /// </summary>
    private void NotificationWentAfterSave()
    {
        journal.ShouldContain("save");
        journal.IndexOf("notification").ShouldBeGreaterThan(journal.LastIndexOf("save"));
    }

    [Fact]
    public async Task CreateInviteToClaim_NotifiesInvitedClaim()
    {
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var receiver = CreateClaim("Приглашаемый");

        await CreateService().CreateAccommodationInvite(
            sender.GetId(),
            RequestId(sender),
            AccommodationTargetIdentification.From(receiver.GetId()));

        Notification().Kind.ShouldBe(InviteChangeKind.Created);
        RecipientClaimIds().ShouldBe([receiver.ClaimId]);
        NotificationWentAfterSave();
    }

    [Fact]
    public async Task CreateInviteToAccommodationRequest_NotifiesWholeGroup()
    {
        // Приглашаем не заявку, а группу соседей: уведомление должно уйти всем её участникам.
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var firstNeighbour = CreateClaim("Сосед1");
        var secondNeighbour = CreateClaim("Сосед2");
        var group = mock.CreateAccommodationRequest(accommodationType, firstNeighbour, secondNeighbour);

        await CreateService().CreateAccommodationInvite(
            sender.GetId(),
            RequestId(sender),
            AccommodationTargetIdentification.From(new AccommodationRequestIdentification(
                mock.ProjectInfo.ProjectId, group.Id)));

        Notification().Kind.ShouldBe(InviteChangeKind.Created);
        RecipientClaimIds().ShouldBe([firstNeighbour.ClaimId, secondNeighbour.ClaimId], ignoreOrder: true);
        NotificationWentAfterSave();
    }

    [Fact]
    public async Task AcceptInvite_NotifiesInvitingClaim()
    {
        // Принял приглашённый, поэтому узнать о результате должна сторона приглашающего.
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var receiver = CreateClaim("Приглашаемый");
        var invite = mock.CreateAccommodationInvite(sender, receiver);

        _ = await CreateService().AcceptAccommodationInvite(InviteId(invite));

        Notification().Kind.ShouldBe(InviteChangeKind.Accepted);
        RecipientClaimIds().ShouldBe([sender.ClaimId]);
        NotificationWentAfterSave();
    }

    [Fact]
    public async Task CancelInvite_NotifiesBothSides()
    {
        // Приглашение отменено — уведомление уходит обеим сторонам: и приглашавшему, и приглашённому.
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var receiver = CreateClaim("Приглашаемый");
        var invite = mock.CreateAccommodationInvite(sender, receiver);

        _ = await CreateService().CancelOrDeclineAccommodationInvite(InviteId(invite), InviteState.Canceled);

        Notification().Kind.ShouldBe(InviteChangeKind.Cancelled);
        RecipientClaimIds().ShouldBe([sender.ClaimId, receiver.ClaimId], ignoreOrder: true);
        NotificationWentAfterSave();
    }

    [Fact]
    public async Task DeclineAllClaimInvites_NotifiesOtherSidesOnly()
    {
        // Заявка отозвана — её собственные приглашения снимаются. Самой заявке писать незачем:
        // операция случилась с ней, а не с ней в качестве адресата.
        var declined = CreateClaimWithAccommodation("Отзываемая");
        var invited = CreateClaim("Приглашённый ею");
        var inviting = CreateClaim("Пригласивший её");
        _ = mock.CreateAccommodationInvite(declined, invited);
        _ = mock.CreateAccommodationInvite(inviting, declined);

        await CreateService().DeclineAllClaimInvites(declined.GetId());

        Notification().Kind.ShouldBe(InviteChangeKind.Cancelled);
        RecipientClaimIds().ShouldBe([invited.ClaimId, inviting.ClaimId], ignoreOrder: true);
        RecipientClaimIds().ShouldNotContain(declined.ClaimId);
        NotificationWentAfterSave();
    }

    [Fact]
    public async Task DeclineAllClaimInvites_NoInvites_NothingSent()
    {
        // Приглашений не было — уведомлять некого, и пустое уведомление ставить не надо.
        var claim = CreateClaimWithAccommodation("Отзываемая");

        await CreateService().DeclineAllClaimInvites(claim.GetId());

        notificationService.Invites.ShouldBeEmpty();
    }
}
