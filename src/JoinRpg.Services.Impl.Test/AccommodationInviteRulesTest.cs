using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.Services.Impl.Test;

/// <summary>
/// Правила, по которым приглашение к совместному проживанию отклоняется.
/// Раньше каждое из них молча возвращало <c>null</c>, и игрок не видел причины отказа.
/// </summary>
/// <remarks>
/// Правила решают по доменным снимкам (ADR022 §4): группы — <see cref="AccommodationGroupInfo"/> из
/// плана поселения, вместимость — из того же плана. Группы заводятся в моке как EF-строки, а снимки
/// из них строит тот же фейковый загрузчик плана, что и у остальных тестов.
/// </remarks>
public class AccommodationInviteRulesTest
{
    private readonly MockedProject mock = new();

    /// <summary>Тип проживания приглашающего; его вместимость тесты меняют до сборки плана.</summary>
    private readonly ProjectAccommodationType defaultType;

    /// <summary>Другой тип — селится из своей категории, то есть лежит в другом плане.</summary>
    private readonly ProjectAccommodationType otherType;

    /// <summary>
    /// Заявки участников групп выдаются подряд, начиная со 100, — чтобы не пересекаться с
    /// отправителем и чтобы ни одна заявка не оказалась в двух группах (план этого не допускает).
    /// </summary>
    private int nextClaimId = 100;

    public AccommodationInviteRulesTest()
    {
        mock.Project.Details.EnableAccommodation = true;
        defaultType = mock.CreateAccommodationType("Домик", capacity: 4);
        otherType = mock.CreateAccommodationType("Шатёр", capacity: 4);
    }

    // Заявка отправителя — первая в его группе.
    private ClaimIdentification SenderClaimId => new(mock.ProjectInfo.ProjectId, 999);

    /// <summary>Группа приглашающего: он сам и ещё <paramref name="subjectCount"/> − 1 соседей.</summary>
    private AccommodationRequest SenderRequest(int subjectCount = 1, bool settled = false)
        => Request(defaultType, subjectCount, settled, firstSubjectClaimId: SenderClaimId.ClaimId);

    /// <summary>Группа цели приглашения.</summary>
    private AccommodationRequest Request(
        ProjectAccommodationType? type = null,
        int subjectCount = 1,
        bool settled = false,
        int? firstSubjectClaimId = null)
    {
        var subjects = Enumerable.Range(0, subjectCount)
            .Select(i => new Claim
            {
                ClaimId = i == 0 && firstSubjectClaimId is { } first ? first : nextClaimId++,
                ProjectId = mock.Project.ProjectId,
            })
            .ToArray();

        var request = mock.CreateAccommodationRequest(type ?? defaultType, subjects);
        if (settled)
        {
            _ = mock.CreateRoom(request);
        }
        return request;
    }

    /// <summary>
    /// Снимок группы из плана поселения её собственного типа — так, как его получает сервис.
    /// </summary>
    private (RoomCategoryPlan Plan, AccommodationGroupInfo Group) Snapshot(AccommodationRequest request)
    {
        var plan = new FakeRoomCategoryPlanRepository(mock)
            .GetPlanForTypeOrDefault(new AccommodationTypeIdentification(mock.ProjectInfo.ProjectId, request.AccommodationTypeId))
            .GetAwaiter().GetResult()
            ?? throw new InvalidOperationException("Нет плана для типа группы");
        return (plan, plan.GetGroup(new AccommodationRequestIdentification(mock.ProjectInfo.ProjectId, request.Id)));
    }

    private void EnsureCanInvite(
        AccommodationRequest? sender,
        AccommodationRequest? receiver,
        int newDwellersCount,
        ClaimIdentification? receiverClaimId = null,
        int capacity = 4)
    {
        // Вместимость — из метаданных проекта (ADR015): план берёт типы из ProjectInfo, поэтому
        // метаданные пересобираются после того, как тест задал вместимость.
        defaultType.Capacity = capacity;
        mock.ReInitProjectInfo();

        AccommodationInviteServiceImpl.EnsureCanInvite(
            mock.ProjectInfo.ProjectId,
            SenderClaimId,
            sender is null ? null : Snapshot(sender),
            receiver is null ? null : Snapshot(receiver).Group,
            newDwellersCount,
            receiverClaimId);
    }

    [Fact]
    public void ShouldRejectSelfInviteToOwnClaim()
    {
        var exception = Should.Throw<AccommodationInviteNotAllowedException>(
            () => EnsureCanInvite(SenderRequest(), receiver: null, newDwellersCount: 1,
                receiverClaimId: SenderClaimId));

        exception.Message.ShouldContain("самого себя");
    }

    [Fact]
    public void ShouldRejectSelfInviteToOwnAccommodationRequest()
    {
        // Приглашаем группу, в которую входит собственная заявка отправителя, — то есть его же группу
        var ownGroup = SenderRequest(subjectCount: 2);

        var exception = Should.Throw<AccommodationInviteNotAllowedException>(
            () => EnsureCanInvite(ownGroup, ownGroup, newDwellersCount: 2));

        exception.Message.ShouldContain("самого себя");
    }

    [Fact]
    public void ShouldAllowInviteIntoRoomWithFreeSpace()
    {
        var act = () => EnsureCanInvite(SenderRequest(subjectCount: 1), Request(subjectCount: 1), newDwellersCount: 1);

        act.ShouldNotThrow();
    }

    [Fact]
    public void ShouldAllowInviteOfClaimWithoutAccommodationRequest()
    {
        // Приглашаемый ещё не выбрал тип проживания — заявки на проживание у него нет
        var act = () => EnsureCanInvite(SenderRequest(subjectCount: 1), receiver: null, newDwellersCount: 1);

        act.ShouldNotThrow();
    }

    [Fact]
    public void ShouldRejectWhenSenderHasNoAccommodationType()
    {
        var exception = Should.Throw<AccommodationInviteNotAllowedException>(
            () => EnsureCanInvite(sender: null, Request(), newDwellersCount: 1));

        exception.ProjectId.ShouldBe(mock.ProjectInfo.ProjectId);
        exception.Message.ShouldContain("не выбран тип проживания");
    }

    [Fact]
    public void ShouldRejectWhenSenderIsAlreadySettledInRoom()
    {
        var exception = Should.Throw<AccommodationInviteNotAllowedException>(
            () => EnsureCanInvite(SenderRequest(settled: true), Request(), newDwellersCount: 1));

        exception.Message.ShouldContain("уже расселён");
    }

    [Fact]
    public void ShouldRejectWhenReceiverIsAlreadySettledInRoom()
    {
        var exception = Should.Throw<AccommodationInviteNotAllowedException>(
            () => EnsureCanInvite(SenderRequest(), Request(settled: true), newDwellersCount: 1));

        exception.Message.ShouldContain("уже расселён");
    }

    [Fact]
    public void ShouldRejectWhenAccommodationTypesDiffer()
    {
        var exception = Should.Throw<AccommodationInviteNotAllowedException>(
            () => EnsureCanInvite(
                SenderRequest(),
                Request(otherType),
                newDwellersCount: 1));

        exception.Message.ShouldContain("такой же тип проживания");
    }

    [Fact]
    public void ShouldRejectWhenRoomHasNotEnoughSpace()
    {
        // В номере на двоих уже живёт один, приглашаем двоих — не влезут
        var exception = Should.Throw<AccommodationInviteNotAllowedException>(
            () => EnsureCanInvite(
                SenderRequest(subjectCount: 1),
                Request(subjectCount: 2),
                newDwellersCount: 2,
                capacity: 2));

        exception.Message.ShouldContain("не хватает мест");
    }

    [Fact]
    public void ShouldAllowInviteThatExactlyFillsTheRoom()
    {
        var act = () => EnsureCanInvite(
            SenderRequest(subjectCount: 1),
            Request(subjectCount: 2),
            newDwellersCount: 2,
            capacity: 3);

        act.ShouldNotThrow();
    }
}
