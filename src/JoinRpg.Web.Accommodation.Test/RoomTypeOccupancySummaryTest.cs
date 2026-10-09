using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.Web.Models.Accommodation;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Accommodation.Test;

/// <summary>
/// Сводка строки типа на странице «Поселение» по плану категории (ADR020): комнатные счётчики —
/// пула, занятость и заявки — самого типа.
/// </summary>
public class RoomTypeOccupancySummaryTest
{
    private readonly MockedProject mock = new();

    [Fact]
    public void SharedPool_RoomCountersArePool_DemandIsPerType()
    {
        var luxEntity = mock.CreateAccommodationType("Люкс", capacity: 2);
        var singleEntity = mock.CreateAccommodationType("Люкс на одного", capacity: 1, roomCategory: luxEntity.RoomCategory);
        mock.ReInitProjectInfo();
        var projectInfo = mock.ProjectInfo;
        var projectId = projectInfo.ProjectId;
        var lux = projectInfo.AccommodationSettings.GetTypeById(new(projectId, luxEntity.Id));
        var single = projectInfo.AccommodationSettings.GetTypeById(new(projectId, singleEntity.Id));

        // Комната 1 — пара «Люкса», комната 2 — одиночка «на одного» (комната полна), комната 3 пуста.
        // Ещё одна группа «на одного» ждёт расселения.
        var pair = Group(1, lux, roomId: 1, persons: 2);
        var alone = Group(2, single, roomId: 2, persons: 1);
        var waiting = Group(3, single, roomId: null, persons: 1);
        var plan = new RoomCategoryPlan(
            lux.RoomCategoryId,
            projectInfo,
            projectInfo.AccommodationSettings.GetTypesOfCategory(lux.RoomCategoryId),
            [Room(1, pair), Room(2, alone), Room(3)],
            [pair, alone, waiting]);

        var forSingle = RoomTypeOccupancySummary.FromPlan(plan, single.Id, paidCount: 0);

        forSingle.Capacity.ShouldBe(2);
        forSingle.RoomsCount.ShouldBe(3);
        forSingle.TotalCapacity.ShouldBe(6);
        forSingle.PoolOccupied.ShouldBe(3);
        forSingle.FullyFreeRoomsCount.ShouldBe(1);
        forSingle.FullyOccupiedRoomsCount.ShouldBe(2);
        forSingle.FreeCapacity.ShouldBe(2);
        forSingle.FullyOccupiedSeats.ShouldBe(3);

        // Формулы строки сходятся: «вместимость × пустые + частично = свободно»,
        // «в полных + в частичных = занято» — и при комнате, заполненной типом на одного.
        (forSingle.Capacity * forSingle.FullyFreeRoomsCount + forSingle.PartialFreeSeats).ShouldBe(forSingle.FreeCapacity);
        (forSingle.FullyOccupiedSeats + forSingle.PartialOccupiedSeats).ShouldBe(forSingle.PoolOccupied);
        forSingle.PartialOccupiedSeats.ShouldBe(0);

        forSingle.Occupied.ShouldBe(1);
        forSingle.ApprovedClaims.ShouldBe(2);
        forSingle.PendingRequests.ShouldBe(1);

        RoomTypeOccupancySummary.FromPlan(plan, lux.Id, paidCount: 0).PendingRequests.ShouldBe(0);

        RoomInfo Room(int id, params AccommodationGroupInfo[] inhabitants)
            => new(new AccommodationRoomIdentification(projectId, id), $"{id}", inhabitants);
    }

    private AccommodationGroupInfo Group(int id, DomainTypes.ProjectMetadata.Accommodation.AccommodationTypeInfo type, int? roomId, int persons)
    {
        var projectId = mock.ProjectInfo.ProjectId;
        return new(
            new AccommodationRequestIdentification(projectId, id),
            type.Id,
            roomId is null ? null : new AccommodationRoomIdentification(projectId, roomId.Value),
            [.. Enumerable.Range(1, persons).Select(i => new ClaimIdentification(projectId, id * 100 + i))]);
    }
}
