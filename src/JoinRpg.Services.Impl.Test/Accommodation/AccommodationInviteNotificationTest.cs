using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.Services.Impl.Accommodation;

namespace JoinRpg.Services.Impl.Test.Accommodation;

/// <summary>
/// Уведомления контура приглашений к совместному проживанию: с каким видом события и кому именно
/// сервис ставит уведомление — и что ставит строго ПОСЛЕ сохранения (ADR018, §11), иначе игрок
/// получил бы письмо о том, чего в базе не случилось.
/// </summary>
public class AccommodationInviteNotificationTest : AccommodationInviteTestBase
{
    [Fact]
    public async Task CreateInviteToClaim_NotifiesInvitedClaim()
    {
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var receiver = CreateClaim("Приглашаемый");

        await CreateService().CreateAccommodationInvite(
            sender.GetId(),
            RequestId(sender),
            AccommodationGroupIdentification.From(receiver.GetId()));

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
            AccommodationGroupIdentification.From(RequestId(group)));

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

        await CreateService().AcceptAccommodationInvite(InviteId(invite));

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

        await CreateService().CancelAccommodationInvite(InviteId(invite));

        Notification().Kind.ShouldBe(InviteChangeKind.Cancelled);
        RecipientClaimIds().ShouldBe([sender.ClaimId, receiver.ClaimId], ignoreOrder: true);
        NotificationWentAfterSave();
    }

    [Fact]
    public async Task DeclineInvite_NotifiesBothSides()
    {
        // Отказ уведомляет так же, как отзыв: тело у обеих операций общее, но различие корней
        // делает их разными операциями, поэтому проверяется каждая.
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var receiver = CreateClaim("Приглашаемый");
        var invite = mock.CreateAccommodationInvite(sender, receiver);

        await CreateService().DeclineAccommodationInvite(InviteId(invite));

        Notification().Kind.ShouldBe(InviteChangeKind.Cancelled);
        RecipientClaimIds().ShouldBe([sender.ClaimId, receiver.ClaimId], ignoreOrder: true);
        NotificationWentAfterSave();
    }

    // Снятие всех приглашений заявки больше не живёт в этом сервисе: его делает claim-контур
    // (приватный ClaimServiceImpl.DeclineAllClaimInvites поверх ctx.LoadInvitesForClaim), и покрыт
    // он там же — DeclineByMaster_WithInvites_*, DeclineByPlayer_WithInvites_DeclinesThem,
    // DeclineByMaster_WithoutInvites_SendsNoInviteNotification в ClaimServiceImplTest.
}
