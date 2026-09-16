using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl.Test.Projects;

/// <summary>
/// Поселение — часть агрегата проекта (ADR009), поэтому сервис собирается поверх настоящего
/// <c>ProjectPropsService</c> и тех же фейков, что и остальные сервисы метаданных.
/// Проверяются три вещи: операция сделала то, что обещала; сохранение ровно одно;
/// без прав не происходит ни мутации, ни сохранения.
/// </summary>
public class AccommodationServiceTest : ProjectMetadataServiceTestBase
{
    private readonly FakeEmailService emailService = new();

    private AccommodationServiceImpl CreateService(int? currentUserId = null)
        => new AccommodationServiceImpl(
            CreatePropsService(CreateCurrentUser(currentUserId)),
            new ThrowingAccommodationRepository(),
            new FakeUserRepository(mock),
            CreateCurrentUser(currentUserId),
            emailService);

    private int ProjectIntId => mock.Project.ProjectId;

    private int SaveCount => unitOfWork.SaveChangesCallCount;

    /// <summary>Читающие методы в этой миграции не участвуют — сервис не должен их звать.</summary>
    private sealed class ThrowingAccommodationRepository : IAccommodationRepository
    {
        public Task<IReadOnlyCollection<ProjectAccommodationType>> GetAccommodationForProject(int projectId) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<ClaimAccommodationInfoRow>> GetClaimAccommodationReport(int project) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<RoomTypeInfoRow>> GetRoomTypesForProject(int project) => throw new NotSupportedException();
        public Task<ProjectAccommodationType> GetRoomTypeById(int roomTypeId) => throw new NotSupportedException();
        public Task<ProjectAccommodationType?> GetRoomTypeWithDesirous(int roomTypeId) => throw new NotSupportedException();
    }

    private ProjectAccommodationType NewRoomType(string name = "Палатка") => new()
    {
        ProjectId = ProjectIntId,
        Name = name,
        Capacity = 4,
        Cost = 100,
        Description = new MarkdownDbValue(),
    };

    /// <summary>Комната с одним заселённым игроком.</summary>
    private (ProjectAccommodation Room, AccommodationRequest Request) CreateOccupiedRoom()
    {
        var roomType = mock.CreateAccommodationType();
        var claim = mock.CreateApprovedClaim(mock.Character, mock.Player);
        var request = mock.CreateAccommodationRequest(roomType, claim);
        return (mock.CreateRoom(request), request);
    }

    #region Типы поселения

    [Fact]
    public async Task CreateRoomTypeSavesOnce()
    {
        var result = await CreateService().SaveRoomTypeAsync(NewRoomType());

        _ = result.ShouldNotBeNull();
        mock.AccommodationTypes.ShouldHaveSingleItem().Name.ShouldBe("Палатка");
        SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task CantCreateRoomTypeWithoutManageAccommodationPermission()
    {
        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).SaveRoomTypeAsync(NewRoomType()));

        mock.AccommodationTypes.ShouldBeEmpty();
        SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task EditRoomTypeSavesOnce()
    {
        var roomType = mock.CreateAccommodationType();

        var result = await CreateService().SaveRoomTypeAsync(new ProjectAccommodationType
        {
            Id = roomType.Id,
            ProjectId = ProjectIntId,
            Name = "Домик",
            Capacity = 2,
            Cost = 500,
            Description = new MarkdownDbValue(),
        });

        result.ShouldBe(roomType);
        roomType.Name.ShouldBe("Домик");
        roomType.Capacity.ShouldBe(2);
        roomType.Cost.ShouldBe(500);
        SaveCount.ShouldBe(1);
    }

    /// <summary>
    /// Тип поселения чужого проекта не находится загрузчиком хэндла — как и раньше, это null,
    /// а не исключение.
    /// </summary>
    [Fact]
    public async Task EditingUnknownRoomTypeReturnsNull()
    {
        var result = await CreateService().SaveRoomTypeAsync(new ProjectAccommodationType
        {
            Id = 100500,
            ProjectId = ProjectIntId,
            Name = "Чужой",
            Description = new MarkdownDbValue(),
        });

        result.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveRoomTypeSavesOnce()
    {
        var roomType = mock.CreateAccommodationType();

        await CreateService().RemoveRoomType(ProjectIntId, roomType.Id);

        mock.AccommodationTypes.ShouldBeEmpty();
        SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task CantRemoveRoomTypeWithoutManageAccommodationPermission()
    {
        var roomType = mock.CreateAccommodationType();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).RemoveRoomType(ProjectIntId, roomType.Id));

        _ = mock.AccommodationTypes.ShouldHaveSingleItem();
        SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task CantRemoveRoomTypeWithOccupiedRoom()
    {
        var (room, _) = CreateOccupiedRoom();

        _ = await Should.ThrowAsync<RoomIsOccupiedException>(
            () => CreateService().RemoveRoomType(ProjectIntId, room.AccommodationTypeId));

        SaveCount.ShouldBe(0);
    }

    #endregion

    #region Комнаты

    [Fact]
    public async Task AddRoomsUnderstandsRangesAndSavesOnce()
    {
        var roomType = mock.CreateAccommodationType();

        var result = await CreateService().AddRooms(ProjectIntId, roomType.Id, "1-3, 7");

        result.Select(r => r.Name).ShouldBe(["1", "2", "3", "7"]);
        mock.Rooms.Count.ShouldBe(4);
        SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task CantAddRoomsWithoutManageAccommodationPermission()
    {
        var roomType = mock.CreateAccommodationType();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).AddRooms(ProjectIntId, roomType.Id, "1-3"));

        mock.Rooms.ShouldBeEmpty();
        SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task EditRoomSavesOnce()
    {
        var roomType = mock.CreateAccommodationType();
        var room = mock.CreateEmptyRoom(roomType);

        await CreateService().EditRoom(ProjectIntId, roomType.Id, room.Id, "Новое имя");

        room.Name.ShouldBe("Новое имя");
        SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task CantEditRoomWithoutManageAccommodationPermission()
    {
        var roomType = mock.CreateAccommodationType();
        var room = mock.CreateEmptyRoom(roomType, "Старое имя");

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).EditRoom(ProjectIntId, roomType.Id, room.Id, "Новое имя"));

        room.Name.ShouldBe("Старое имя");
        SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task DeleteRoomSavesOnce()
    {
        var roomType = mock.CreateAccommodationType();
        var room = mock.CreateEmptyRoom(roomType);

        await CreateService().DeleteRoom(ProjectIntId, roomType.Id, room.Id);

        mock.Rooms.ShouldBeEmpty();
        SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task CantDeleteRoomWithoutManageAccommodationPermission()
    {
        var roomType = mock.CreateAccommodationType();
        var room = mock.CreateEmptyRoom(roomType);

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).DeleteRoom(ProjectIntId, roomType.Id, room.Id));

        _ = mock.Rooms.ShouldHaveSingleItem();
        SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task CantDeleteOccupiedRoom()
    {
        var (room, _) = CreateOccupiedRoom();

        _ = await Should.ThrowAsync<RoomIsOccupiedException>(
            () => CreateService().DeleteRoom(ProjectIntId, room.AccommodationTypeId, room.Id));

        SaveCount.ShouldBe(0);
    }

    #endregion

    #region Заселение

    [Fact]
    public async Task OccupyRoomSavesOnceAndSendsEmail()
    {
        var roomType = mock.CreateAccommodationType();
        var claim = mock.CreateApprovedClaim(mock.Character, mock.Player);
        var request = mock.CreateAccommodationRequest(roomType, claim);
        var room = mock.CreateEmptyRoom(roomType);

        await CreateService().OccupyRoom(new OccupyRequest
        {
            ProjectId = ProjectIntId,
            RoomId = room.Id,
            AccommodationRequestIds = [request.Id],
        });

        request.AccommodationId.ShouldBe(room.Id);
        SaveCount.ShouldBe(1);
        _ = emailService.Sent.ShouldHaveSingleItem().ShouldBeOfType<OccupyRoomEmail>();
    }

    [Fact]
    public async Task CantOccupyRoomWithoutSetPlayersAccommodationsPermission()
    {
        var roomType = mock.CreateAccommodationType();
        var claim = mock.CreateApprovedClaim(mock.Character, mock.Player);
        var request = mock.CreateAccommodationRequest(roomType, claim);
        var room = mock.CreateEmptyRoom(roomType);

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).OccupyRoom(new OccupyRequest
            {
                ProjectId = ProjectIntId,
                RoomId = room.Id,
                AccommodationRequestIds = [request.Id],
            }));

        request.AccommodationId.ShouldBeNull();
        SaveCount.ShouldBe(0);
        emailService.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task CantOccupyRoomBeyondCapacity()
    {
        var roomType = mock.CreateAccommodationType(capacity: 1);
        var request = mock.CreateAccommodationRequest(
            roomType,
            mock.CreateApprovedClaim(mock.Character, mock.Player),
            mock.CreateClaim(mock.CreateCharacter("Второй персонаж"), mock.Master));
        var room = mock.CreateEmptyRoom(roomType);

        _ = await Should.ThrowAsync<JoinRpgInsufficientRoomSpaceException>(
            () => CreateService().OccupyRoom(new OccupyRequest
            {
                ProjectId = ProjectIntId,
                RoomId = room.Id,
                AccommodationRequestIds = [request.Id],
            }));

        SaveCount.ShouldBe(0);
        emailService.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task UnOccupyRoomSavesOnceAndSendsEmail()
    {
        var (_, request) = CreateOccupiedRoom();

        await CreateService().UnOccupyRoom(new UnOccupyRequest
        {
            ProjectId = ProjectIntId,
            AccommodationRequestId = request.Id,
        });

        request.AccommodationId.ShouldBeNull();
        SaveCount.ShouldBe(1);
        _ = emailService.Sent.ShouldHaveSingleItem().ShouldBeOfType<UnOccupyRoomEmail>();
    }

    [Fact]
    public async Task CantUnOccupyRoomWithoutSetPlayersAccommodationsPermission()
    {
        var (room, request) = CreateOccupiedRoom();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).UnOccupyRoom(new UnOccupyRequest
            {
                ProjectId = ProjectIntId,
                AccommodationRequestId = request.Id,
            }));

        request.AccommodationId.ShouldBe(room.Id);
        SaveCount.ShouldBe(0);
        emailService.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task UnOccupyRoomAllSavesOnce()
    {
        var (room, request) = CreateOccupiedRoom();

        await CreateService().UnOccupyRoomAll(new UnOccupyAllRequest
        {
            ProjectId = ProjectIntId,
            RoomId = room.Id,
        });

        request.AccommodationId.ShouldBeNull();
        SaveCount.ShouldBe(1);
        _ = emailService.Sent.ShouldHaveSingleItem().ShouldBeOfType<UnOccupyRoomEmail>();
    }

    [Fact]
    public async Task CantUnOccupyRoomAllWithoutSetPlayersAccommodationsPermission()
    {
        var (room, request) = CreateOccupiedRoom();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).UnOccupyRoomAll(new UnOccupyAllRequest
            {
                ProjectId = ProjectIntId,
                RoomId = room.Id,
            }));

        request.AccommodationId.ShouldBe(room.Id);
        SaveCount.ShouldBe(0);
    }

    /// <summary>
    /// Изменение поведения: раньше выселение каждой комнаты сохранялось отдельно, теперь вся
    /// операция — одно сохранение, а писем по-прежнему по одному на комнату.
    /// </summary>
    [Fact]
    public async Task UnOccupyAllSavesOnceForAllRooms()
    {
        var (_, firstRequest) = CreateOccupiedRoom();
        var (_, secondRequest) = CreateOccupiedRoom();

        await CreateService().UnOccupyAll(ProjectIntId);

        firstRequest.AccommodationId.ShouldBeNull();
        secondRequest.AccommodationId.ShouldBeNull();
        SaveCount.ShouldBe(1);
        emailService.Sent.Count.ShouldBe(2);
    }

    [Fact]
    public async Task CantUnOccupyAllWithoutSetPlayersAccommodationsPermission()
    {
        var (room, request) = CreateOccupiedRoom();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).UnOccupyAll(ProjectIntId));

        request.AccommodationId.ShouldBe(room.Id);
        SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task UnOccupyRoomTypeTouchesOnlyItsOwnType()
    {
        var (_, requestToUnoccupy) = CreateOccupiedRoom();
        var (otherRoom, otherRequest) = CreateOccupiedRoom();

        await CreateService().UnOccupyRoomType(ProjectIntId, requestToUnoccupy.AccommodationTypeId);

        requestToUnoccupy.AccommodationId.ShouldBeNull();
        otherRequest.AccommodationId.ShouldBe(otherRoom.Id);
        SaveCount.ShouldBe(1);
        _ = emailService.Sent.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task CantUnOccupyRoomTypeWithoutSetPlayersAccommodationsPermission()
    {
        var (room, request) = CreateOccupiedRoom();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).UnOccupyRoomType(ProjectIntId, room.AccommodationTypeId));

        request.AccommodationId.ShouldBe(room.Id);
        SaveCount.ShouldBe(0);
    }

    #endregion

    /// <summary>
    /// Изменение поведения: раньше проверки активности проекта в сервисе не было вовсе.
    /// Подготовка поселения — операция про будущую игру, поэтому в архивном проекте запрещена.
    /// </summary>
    [Fact]
    public async Task CantChangeAccommodationInArchivedProject()
    {
        mock.Project.Active = false;
        mock.Project.IsAcceptingClaims = false;
        mock.ReInitProjectInfo();

        _ = await Should.ThrowAsync<ProjectDeactivatedException>(
            () => CreateService().SaveRoomTypeAsync(NewRoomType()));

        SaveCount.ShouldBe(0);
    }
}
