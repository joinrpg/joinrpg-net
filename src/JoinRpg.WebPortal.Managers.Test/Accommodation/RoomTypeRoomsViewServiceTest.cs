using JoinRpg.Data.Interfaces.Accommodation;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.ProjectMetadata.Accommodation;
using JoinRpg.Services.Interfaces;
using JoinRpg.WebPortal.Managers.Accommodation;

namespace JoinRpg.WebPortal.Managers.Test.Accommodation;

/// <summary>
/// Серверная сторона контрола расселения: модель страницы «Комнаты» из плана поселения и
/// разбор поля «добавить комнаты».
/// </summary>
public class RoomTypeRoomsViewServiceTest
{
    private readonly MockedProject mock = new();
    private readonly FakeAccommodationService accommodationService = new();
    private readonly FakePlanRepository planRepository = new();

    private ProjectIdentification ProjectId => mock.ProjectInfo.ProjectId;

    private RoomTypeRoomsViewService CreateService(int userId)
        => new(
            planRepository,
            new FakeCharacterInfoRepository(mock),
            new FakeProjectMetadataRepository(mock),
            accommodationService,
            new FakeCurrentUserAccessor(userId));

    private AccommodationTypeInfo CreateType(int capacity = 2)
    {
        var type = mock.CreateAccommodationType("Двушка", capacity: capacity);
        mock.ReInitProjectInfo();
        return mock.ProjectInfo.AccommodationSettings.GetTypeById(new(ProjectId, type.Id));
    }

    /// <summary>Группа из одного жильца — заявки игрока на новом персонаже</summary>
    private AccommodationGroupInfo CreateGroup(AccommodationTypeInfo type, int groupId, AccommodationRoomIdentification? roomId)
    {
        var character = mock.CreateCharacter($"Персонаж {groupId}");
        var claim = mock.CreateApprovedClaim(character, mock.Player);
        return new AccommodationGroupInfo(
            new AccommodationRequestIdentification(ProjectId, groupId),
            type.Id,
            roomId,
            [new ClaimIdentification(ProjectId, claim.ClaimId)]);
    }

    private RoomCategoryPlan CreatePlan(
        AccommodationTypeInfo type,
        IReadOnlyCollection<RoomInfo> rooms,
        IReadOnlyCollection<AccommodationGroupInfo> groups)
        => new(type.RoomCategoryId, mock.ProjectInfo, [type], rooms, groups);

    [Fact]
    public async Task GetRooms_SplitsGroupsIntoRoomsAndUnassigned()
    {
        var type = CreateType();
        var roomId = new AccommodationRoomIdentification(ProjectId, 7);
        var placed = CreateGroup(type, 1, roomId);
        var waiting = CreateGroup(type, 2, roomId: null);
        planRepository.Plan = CreatePlan(type, [new RoomInfo(roomId, "7", [placed])], [placed, waiting]);

        var model = JsonRoundTrip.Ensure(await CreateService(mock.Master.UserId).GetRooms(type.Id));

        var room = model.Rooms.ShouldHaveSingleItem();
        room.RoomId.ShouldBe(roomId);
        room.Groups.ShouldHaveSingleItem().GroupId.ShouldBe(placed.Id);
        model.UnassignedGroups.ShouldHaveSingleItem().GroupId.ShouldBe(waiting.Id);
        model.UnassignedGroups[0].Residents.ShouldHaveSingleItem().PlayerName.DisplayName.ShouldBe(mock.PlayerInfo.DisplayName.DisplayName);
        model.RoomCapacity.ShouldBe(2);
        model.TypeCapacity.ShouldBe(2);
        model.TypeName.ShouldBe("Двушка");
    }

    [Fact]
    public async Task GetRooms_MasterGetsBothRights()
    {
        var type = CreateType();
        planRepository.Plan = CreatePlan(type, [], []);

        var model = await CreateService(mock.Master.UserId).GetRooms(type.Id);

        model.CanManageRooms.ShouldBeTrue();
        model.CanAssignRooms.ShouldBeTrue();
    }

    [Fact]
    public async Task GetRooms_NotMasterGetsNoRights()
    {
        var type = CreateType();
        planRepository.Plan = CreatePlan(type, [], []);

        var model = await CreateService(mock.Player.UserId).GetRooms(type.Id);

        model.CanManageRooms.ShouldBeFalse();
        model.CanAssignRooms.ShouldBeFalse();
    }

    [Fact]
    public async Task GetRoomsOrDefault_UnknownTypeIsNull()
    {
        var type = CreateType();
        planRepository.Plan = null;

        (await CreateService(mock.Master.UserId).GetRoomsOrDefault(type.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task AddRooms_ParsesRangesAndAddsToTypeCategory()
    {
        var type = CreateType();
        planRepository.Plan = CreatePlan(type, [], []);

        _ = await CreateService(mock.Master.UserId).AddRooms(type.Id, "1, 3-5, Люкс");

        accommodationService.AddedTo.ShouldBe(type.RoomCategoryId);
        accommodationService.AddedNames.ShouldBe(["1", "3", "4", "5", "Люкс"]);
    }

    [Fact]
    public async Task AddRooms_EmptyInputIsRejected()
    {
        var type = CreateType();

        _ = await Should.ThrowAsync<FieldRequiredException>(
            () => CreateService(mock.Master.UserId).AddRooms(type.Id, " , "));
        accommodationService.AddedNames.ShouldBeNull();
    }

    [Fact]
    public async Task AddRooms_UnknownTypeIsNotFound()
    {
        _ = CreateType();

        _ = await Should.ThrowAsync<AccommodationTypeNotFoundException>(
            () => CreateService(mock.Master.UserId).AddRooms(new AccommodationTypeIdentification(ProjectId, 999), "1"));
    }

    private sealed class FakePlanRepository : IRoomCategoryPlanRepository
    {
        public RoomCategoryPlan? Plan { get; set; }

        public Task<RoomCategoryPlan?> GetPlanForTypeOrDefault(AccommodationTypeIdentification typeId)
            => Task.FromResult(Plan);

        public Task<IReadOnlyCollection<RoomCategoryPlan>> GetAllPlans(ProjectIdentification projectId)
            => throw new NotSupportedException();
    }

    private sealed class FakeAccommodationService : IAccommodationService
    {
        public RoomCategoryIdentification? AddedTo { get; private set; }
        public IReadOnlyCollection<string>? AddedNames { get; private set; }

        public Task<IReadOnlyCollection<AccommodationRoomIdentification>> AddRooms(
            RoomCategoryIdentification categoryId,
            IReadOnlyCollection<string> roomNames)
        {
            AddedTo = categoryId;
            AddedNames = roomNames;
            return Task.FromResult<IReadOnlyCollection<AccommodationRoomIdentification>>([]);
        }

        public Task RenameRoom(AccommodationRoomIdentification roomId, string name) => throw new NotSupportedException();
        public Task DeleteRoom(AccommodationRoomIdentification roomId) => throw new NotSupportedException();
        public Task OccupyRoom(AccommodationRoomIdentification roomId, IReadOnlyCollection<AccommodationRequestIdentification> groupIds) => throw new NotSupportedException();
        public Task UnOccupyGroup(AccommodationRequestIdentification groupId) => throw new NotSupportedException();
        public Task UnOccupyRoom(AccommodationRoomIdentification roomId) => throw new NotSupportedException();
        public Task UnOccupyRoomType(AccommodationTypeIdentification typeId) => throw new NotSupportedException();
        public Task UnOccupyAllRooms(ProjectIdentification projectId) => throw new NotSupportedException();
    }
}
