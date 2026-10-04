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
            AccommodationTargetIdentification.From(RequestId(group)));

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
