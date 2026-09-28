using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.Services.Impl.Test;

/// <summary>
/// Правила, по которым приглашение к совместному проживанию отклоняется.
/// Раньше каждое из них молча возвращало <c>null</c>, и игрок не видел причины отказа.
/// </summary>
public class AccommodationInviteRulesTest
{
    private static readonly ProjectIdentification ProjectId = new(1);

    // Заявка отправителя; приглашаемые в тестах по умолчанию живут на других заявках (100+),
    // чтобы не пересекаться с отправителем
    private static readonly ClaimIdentification SenderClaimId = new(1, 999);

    private static AccommodationRequest Request(
        int accommodationTypeId = 10,
        int capacity = 4,
        int subjectCount = 1,
        int? accommodationId = null,
        int firstSubjectClaimId = 100)
        => new()
        {
            ProjectId = ProjectId.Value,
            AccommodationTypeId = accommodationTypeId,
            AccommodationType = new ProjectAccommodationType { Id = accommodationTypeId, Capacity = capacity },
            AccommodationId = accommodationId,
            Subjects = [.. Enumerable.Range(0, subjectCount).Select(i => new Claim { ClaimId = firstSubjectClaimId + i })],
        };

    private static void EnsureCanInvite(
        AccommodationRequest? sender,
        AccommodationRequest? receiver,
        int newDwellersCount,
        int? receiverClaimId = null)
        => AccommodationInviteServiceImpl.EnsureCanInvite(
            ProjectId, SenderClaimId, sender, receiver, newDwellersCount, receiverClaimId);

    [Fact]
    public void ShouldRejectSelfInviteToOwnClaim()
    {
        var exception = Should.Throw<AccommodationInviteNotAllowedException>(
            () => EnsureCanInvite(Request(), receiver: null, newDwellersCount: 1,
                receiverClaimId: SenderClaimId.ClaimId));

        exception.Message.ShouldContain("самого себя");
    }

    [Fact]
    public void ShouldRejectSelfInviteToOwnAccommodationRequest()
    {
        // Приглашаем группу, в которую входит собственная заявка отправителя
        var exception = Should.Throw<AccommodationInviteNotAllowedException>(
            () => EnsureCanInvite(
                Request(),
                Request(subjectCount: 2, firstSubjectClaimId: SenderClaimId.ClaimId),
                newDwellersCount: 2));

        exception.Message.ShouldContain("самого себя");
    }

    [Fact]
    public void ShouldAllowInviteIntoRoomWithFreeSpace()
    {
        var act = () => EnsureCanInvite(Request(subjectCount: 1), Request(subjectCount: 1), newDwellersCount: 1);

        act.ShouldNotThrow();
    }

    [Fact]
    public void ShouldAllowInviteOfClaimWithoutAccommodationRequest()
    {
        // Приглашаемый ещё не выбрал тип проживания — заявки на проживание у него нет
        var act = () => EnsureCanInvite(Request(subjectCount: 1), receiver: null, newDwellersCount: 1);

        act.ShouldNotThrow();
    }

    [Fact]
    public void ShouldRejectWhenSenderHasNoAccommodationType()
    {
        var exception = Should.Throw<AccommodationInviteNotAllowedException>(
            () => EnsureCanInvite(sender: null, Request(), newDwellersCount: 1));

        exception.ProjectId.ShouldBe(ProjectId);
        exception.Message.ShouldContain("не выбран тип проживания");
    }

    [Fact]
    public void ShouldRejectWhenSenderIsAlreadySettledInRoom()
    {
        var exception = Should.Throw<AccommodationInviteNotAllowedException>(
            () => EnsureCanInvite(Request(accommodationId: 55), Request(), newDwellersCount: 1));

        exception.Message.ShouldContain("уже расселён");
    }

    [Fact]
    public void ShouldRejectWhenReceiverIsAlreadySettledInRoom()
    {
        var exception = Should.Throw<AccommodationInviteNotAllowedException>(
            () => EnsureCanInvite(Request(), Request(accommodationId: 55), newDwellersCount: 1));

        exception.Message.ShouldContain("уже расселён");
    }

    [Fact]
    public void ShouldRejectWhenAccommodationTypesDiffer()
    {
        var exception = Should.Throw<AccommodationInviteNotAllowedException>(
            () => EnsureCanInvite(
                Request(accommodationTypeId: 10),
                Request(accommodationTypeId: 20),
                newDwellersCount: 1));

        exception.Message.ShouldContain("такой же тип проживания");
    }

    [Fact]
    public void ShouldRejectWhenRoomHasNotEnoughSpace()
    {
        // В номере на двоих уже живёт один, приглашаем двоих — не влезут
        var exception = Should.Throw<AccommodationInviteNotAllowedException>(
            () => EnsureCanInvite(
                Request(capacity: 2, subjectCount: 1),
                Request(capacity: 2, subjectCount: 2),
                newDwellersCount: 2));

        exception.Message.ShouldContain("не хватает мест");
    }

    [Fact]
    public void ShouldAllowInviteThatExactlyFillsTheRoom()
    {
        var act = () => EnsureCanInvite(
            Request(capacity: 3, subjectCount: 1),
            Request(capacity: 3, subjectCount: 2),
            newDwellersCount: 2);

        act.ShouldNotThrow();
    }
}
