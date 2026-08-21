using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.Services.Impl.Test;

/// <summary>
/// Правила, по которым приглашение к совместному проживанию отклоняется.
/// Раньше каждое из них молча возвращало <c>null</c>, и игрок не видел причины отказа.
/// </summary>
public class AccommodationInviteRulesTest
{
    private static readonly ProjectIdentification ProjectId = new(1);

    private static AccommodationRequest Request(
        int accommodationTypeId = 10,
        int capacity = 4,
        int subjectCount = 1,
        int? accommodationId = null)
        => new()
        {
            ProjectId = ProjectId.Value,
            AccommodationTypeId = accommodationTypeId,
            AccommodationType = new ProjectAccommodationType { Id = accommodationTypeId, Capacity = capacity },
            AccommodationId = accommodationId,
            Subjects = [.. Enumerable.Range(0, subjectCount).Select(i => new Claim { ClaimId = 100 + i })],
        };

    private static void EnsureCanInvite(
        AccommodationRequest? sender,
        AccommodationRequest? receiver,
        int newDwellersCount)
        => AccommodationInviteServiceImpl.EnsureCanInvite(ProjectId, sender, receiver, newDwellersCount);

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
