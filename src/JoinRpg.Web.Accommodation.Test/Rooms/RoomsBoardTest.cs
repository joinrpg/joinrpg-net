using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.Web.Accommodation.Rooms;
using JoinRpg.Web.ProjectCommon.Claims;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Accommodation.Test.Rooms;

/// <summary>
/// Состояние контрола расселения на клиенте: правило свободного места и «Заселить всех».
/// </summary>
public class RoomsBoardTest
{
    [Fact]
    public void FreeSpaceIsCapacityMinusOccupancy()
    {
        var board = new RoomsBoard(RoomsModel.Create(
            rooms: [RoomsModel.Room(1, RoomsModel.Group(1, persons: 1))]));

        board.GetFreeSpace(board.Rooms[0]).ShouldBe(1);
    }

    [Fact]
    public void InhabitantOfSmallerTypeFillsTheWholeRoom()
    {
        // ADR018, «Правило свободного места»: «Люкс на одного» в двухместном номере делает
        // комнату полной.
        var single = RoomsModel.Group(1, persons: 1, typeCapacity: 1, typeId: 2);
        var board = new RoomsBoard(RoomsModel.Create(rooms: [RoomsModel.Room(1, single)]));

        board.GetFreeSpace(board.Rooms[0]).ShouldBe(0);
    }

    [Fact]
    public void FitsAccountsForAlreadySelectedGroups()
    {
        var first = RoomsModel.Group(1, persons: 1);
        var second = RoomsModel.Group(2, persons: 1);
        var third = RoomsModel.Group(3, persons: 1);
        var board = new RoomsBoard(RoomsModel.Create(rooms: [RoomsModel.Room(1)], unassigned: [first, second, third]));
        var room = board.Rooms[0];

        board.Fits(room, second, [first]).ShouldBeTrue();
        board.Fits(room, third, [first, second]).ShouldBeFalse();
    }

    [Fact]
    public void PlaceAllFillsRoomsGreedilyInListOrder()
    {
        var pair = RoomsModel.Group(1, persons: 2);
        var single = RoomsModel.Group(2, persons: 1);
        var anotherSingle = RoomsModel.Group(3, persons: 1);
        var board = new RoomsBoard(RoomsModel.Create(
            rooms: [RoomsModel.Room(1), RoomsModel.Room(2, RoomsModel.Group(9, persons: 1))],
            unassigned: [pair, single, anotherSingle]));

        var placements = board.PlaceAll();

        placements.Select(p => $"{p.Room.Name}: {string.Join(", ", p.Groups.Select(g => g.GroupId.AccommodationRequestId))}")
            .ShouldBe(["1: 1", "2: 2"]);
        board.Unassigned.ShouldBe([anotherSingle]);
    }

    [Fact]
    public void RollbackMoveReturnsGroupsToTheirPlaces()
    {
        var first = RoomsModel.Group(1, persons: 1);
        var second = RoomsModel.Group(2, persons: 1);
        var third = RoomsModel.Group(3, persons: 1);
        var board = new RoomsBoard(RoomsModel.Create(rooms: [RoomsModel.Room(1)], unassigned: [first, second, third]));
        var snapshot = board.Snapshot();

        var (room, groups) = board.PlaceAll().ShouldHaveSingleItem();
        board.RollbackMove(room, groups, snapshot);

        board.Unassigned.ShouldBe([first, second, third]);
        room.Groups.ShouldBeEmpty();
    }

    [Fact]
    public void KickAllOfTypeLeavesOtherTypes()
    {
        var own = RoomsModel.Group(1, persons: 1);
        var other = RoomsModel.Group(2, persons: 1, typeId: 2);
        var board = new RoomsBoard(RoomsModel.Create(rooms: [RoomsModel.Room(1, own, other)]));

        board.KickAllOfType(RoomsModel.TypeId);

        board.Rooms[0].Groups.ShouldBe([other]);
        board.Unassigned.ShouldBe([own]);
    }

    [Fact]
    public void RestoreBringsBackSnapshot()
    {
        var group = RoomsModel.Group(1, persons: 1);
        var board = new RoomsBoard(RoomsModel.Create(rooms: [RoomsModel.Room(1, group)]));
        var snapshot = board.Snapshot();

        board.Kick(board.Rooms[0], group);
        board.Restore(snapshot);

        board.Rooms[0].Groups.ShouldBe([group]);
        board.Unassigned.ShouldBeEmpty();
    }
}

/// <summary>Модели страницы «Комнаты» для тестов</summary>
internal static class RoomsModel
{
    public static readonly ProjectIdentification ProjectId = new(123);
    public static readonly AccommodationTypeIdentification TypeId = new(ProjectId, 1);

    public static AccommodationGroupViewModel Group(
        int id,
        int persons,
        int typeCapacity = 2,
        int typeId = 1,
        int feeTotal = 0,
        int feeToPay = 0)
        => new(
            new AccommodationRequestIdentification(ProjectId, id),
            new AccommodationTypeIdentification(ProjectId, typeId),
            typeCapacity,
            [.. Enumerable.Range(0, persons)
                .Select(i => new ClaimLinkViewModel(
                    new ClaimIdentification(ProjectId, (id * 100) + i),
                    new UserDisplayName($"Игрок {id}-{i}", null),
                    $"Персонаж {id}-{i}",
                    "",
                    new UserIdentification((id * 100) + i)))],
            feeTotal,
            feeToPay);

    public static AccommodationRoomViewModel Room(int id, params AccommodationGroupViewModel[] groups)
        => new(new AccommodationRoomIdentification(ProjectId, id), id.ToString(), groups);

    public static RoomTypeRoomsViewModel Create(
        IReadOnlyList<AccommodationRoomViewModel>? rooms = null,
        IReadOnlyList<AccommodationGroupViewModel>? unassigned = null,
        bool canManageRooms = true,
        bool canAssignRooms = true,
        int capacity = 2)
        => new(
            TypeId,
            "Проект",
            "Двушка",
            TypeCapacity: capacity,
            RoomCapacity: capacity,
            rooms ?? [],
            unassigned ?? [],
            canManageRooms,
            canAssignRooms);
}
