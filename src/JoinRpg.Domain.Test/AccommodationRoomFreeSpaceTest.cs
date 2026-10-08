using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;

namespace JoinRpg.Domain.Test;

/// <summary>
/// Свободное место в комнате для контура приглашений (<see cref="AccommodationExtensions"/>).
/// Комната принадлежит категории, а не типу проживания (ADR020), поэтому предел комнаты задают
/// типы живущих в ней групп — правило свободного места ADR018.
/// </summary>
public class AccommodationRoomFreeSpaceTest
{
    private readonly MockedProject mock = new();

    private AccommodationRequest Group(ProjectAccommodationType type, int persons)
    {
        var claims = new Claim[persons];
        for (var i = 0; i < persons; i++)
        {
            claims[i] = mock.CreateClaim(mock.CreateCharacter($"Персонаж {mock.AccommodationRequests.Count}-{i}"), mock.Player);
        }
        return mock.CreateAccommodationRequest(type, claims);
    }

    private static void PlaceInto(AccommodationRequest group, ProjectAccommodation room)
    {
        room.Inhabitants.Add(group);
        group.Accommodation = room;
        group.AccommodationId = room.Id;
    }

    [Fact]
    public void SingleType_CapacityMinusOccupancy()
    {
        var lux = mock.CreateAccommodationType("Люкс", capacity: 3);
        mock.ReInitProjectInfo();
        var group = Group(lux, persons: 1);
        _ = mock.CreateRoom(group);

        group.GetRoomFreeSpace(mock.ProjectInfo).ShouldBe(2);
    }

    [Fact]
    public void Unassigned_CapacityOfOwnTypeMinusGroupSize()
    {
        var lux = mock.CreateAccommodationType("Люкс", capacity: 3);
        mock.ReInitProjectInfo();
        var group = Group(lux, persons: 2);

        group.GetRoomFreeSpace(mock.ProjectInfo).ShouldBe(1);
    }

    [Fact]
    public void RoomWithSiblingTypeGroup_IsLimitedByItsCapacity()
    {
        // «Люкс на двоих», въехавший в трёхместный «Люкс», делает комнату двухместной — и для
        // группы «Люкса» тоже. По вместимости типа комнаты осталось бы одно место.
        var lux = mock.CreateAccommodationType("Люкс", capacity: 3);
        var pair = mock.CreateAccommodationType("Люкс на двоих", capacity: 2);
        mock.ReInitProjectInfo();
        var room = mock.CreateEmptyRoom(lux, "101");
        var luxGroup = Group(lux, persons: 1);
        PlaceInto(luxGroup, room);
        PlaceInto(Group(pair, persons: 1), room);

        luxGroup.GetRoomFreeSpace(mock.ProjectInfo).ShouldBe(0);
    }
}
