using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Impl.Accommodation;
using JoinRpg.Services.Impl.Test.Projects;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Services.Impl.Test.Accommodation;

/// <summary>
/// Тесты управления комнатами (<c>AddRooms</c>/<c>RenameRoom</c>/<c>DeleteRoom</c>) поверх
/// агрегата плана поселения (ADR018, PR 4). Сервис собран на реальном
/// <see cref="AccommodationPropsService"/> и фейковом write-хэндле.
/// </summary>
/// <remarks>
/// Тесты «без права», «комната чужого проекта» и «архивный проект» воспроизводят дефекты 1, 2 и 4
/// из ADR018: до переноса <c>EditRoom</c>/<c>DeleteRoom</c> не проверяли ни прав, ни проекта
/// (<c>projectId</c> был необязательным), а активность проекта не проверял ни один метод.
/// </remarks>
public class AccommodationRoomsServiceTest : ProjectMetadataServiceTestBase
{
    private readonly ProjectAccommodationType roomType;

    public AccommodationRoomsServiceTest()
    {
        mock.Project.Details.EnableAccommodation = true;
        roomType = mock.CreateAccommodationType("Палатка", capacity: 4);
        mock.ReInitProjectInfo();
    }

    private RoomCategoryIdentification CategoryId
        => new(ProjectId, roomType.Id);

    private AccommodationServiceImpl CreateService(int? currentUserId = null, bool isAdmin = false)
    {
        var currentUser = CreateCurrentUser(currentUserId, isAdmin);
        return new AccommodationServiceImpl(
            unitOfWork,
            new FakeEmailService(),
            currentUser,
            new AccommodationPropsService(
                unitOfWork, currentUser, metadataRepository, NullLogger<AccommodationPropsService>.Instance));
    }

    /// <summary>Переводит проект в архив — операции над ним запрещены.</summary>
    private void ArchiveProject()
    {
        mock.Project.Active = false;
        mock.Project.IsAcceptingClaims = false;
        mock.ReInitProjectInfo();
    }

    private AccommodationRoomIdentification RoomId(ProjectAccommodation room)
        => new(ProjectId, room.Id);

    /// <summary>Идентификатор той же комнаты, но в чужом проекте.</summary>
    private AccommodationRoomIdentification AlienRoomId(ProjectAccommodation room)
        => new(new ProjectIdentification(ProjectId.Value + 1), room.Id);

    [Fact]
    public async Task AddRooms_CreatesRoomsInCategory()
    {
        var created = await CreateService().AddRooms(CategoryId, ["101", "102"]);

        created.Count.ShouldBe(2);
        mock.Rooms.Select(room => room.Name).ShouldBe(["101", "102"]);
        mock.Rooms.ShouldAllBe(room => room.AccommodationTypeId == roomType.Id);
        mock.Rooms.ShouldAllBe(room => room.ProjectAccommodationType == roomType);
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    /// <summary>Дефект 1 ADR018: до переноса добавление комнат не проверяло прав вообще.</summary>
    [Fact]
    public async Task AddRooms_WithoutPermission_Throws()
    {
        // Игрок (mock.Player) не входит в ACL проекта
        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).AddRooms(CategoryId, ["101"]));

        mock.Rooms.ShouldBeEmpty();
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>Дефект 4 ADR018: активность проекта не проверял ни один метод.</summary>
    [Fact]
    public async Task AddRooms_InArchivedProject_Throws()
    {
        ArchiveProject();

        _ = await Should.ThrowAsync<ProjectDeactivatedException>(
            () => CreateService().AddRooms(CategoryId, ["101"]));

        mock.Rooms.ShouldBeEmpty();
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task RenameRoom_ChangesName()
    {
        var room = mock.CreateEmptyRoom(roomType, "101");

        await CreateService().RenameRoom(RoomId(room), "102");

        room.Name.ShouldBe("102");
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    /// <summary>Дефект 1 ADR018: <c>EditRoom</c> не проверял прав вообще.</summary>
    [Fact]
    public async Task RenameRoom_WithoutPermission_Throws()
    {
        var room = mock.CreateEmptyRoom(roomType, "101");

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).RenameRoom(RoomId(room), "102"));

        room.Name.ShouldBe("101");
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Дефект 2 ADR018: <c>projectId</c> у <c>EditRoom</c> был необязательным, и контракт разрешал
    /// переименовать комнату чужого проекта.
    /// </summary>
    [Fact]
    public async Task RenameRoom_RoomFromAnotherProject_Throws()
    {
        var room = mock.CreateEmptyRoom(roomType, "101");

        _ = await Should.ThrowAsync<AccommodationRoomNotFoundException>(
            () => CreateService().RenameRoom(AlienRoomId(room), "102"));

        room.Name.ShouldBe("101");
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>Дефект 4 ADR018.</summary>
    [Fact]
    public async Task RenameRoom_InArchivedProject_Throws()
    {
        var room = mock.CreateEmptyRoom(roomType, "101");
        ArchiveProject();

        _ = await Should.ThrowAsync<ProjectDeactivatedException>(
            () => CreateService().RenameRoom(RoomId(room), "102"));

        room.Name.ShouldBe("101");
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task DeleteRoom_RemovesEmptyRoom()
    {
        var room = mock.CreateEmptyRoom(roomType, "101");

        await CreateService().DeleteRoom(RoomId(room));

        mock.Rooms.ShouldBeEmpty();
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task DeleteRoom_OccupiedRoom_Throws()
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        var group = mock.CreateAccommodationRequest(roomType, claim);
        var room = mock.CreateRoom(group, "101");

        _ = await Should.ThrowAsync<RoomIsOccupiedException>(
            () => CreateService().DeleteRoom(RoomId(room)));

        mock.Rooms.ShouldHaveSingleItem();
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>Дефект 1 ADR018: <c>DeleteRoom</c> не проверял прав вообще.</summary>
    [Fact]
    public async Task DeleteRoom_WithoutPermission_Throws()
    {
        var room = mock.CreateEmptyRoom(roomType, "101");

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).DeleteRoom(RoomId(room)));

        mock.Rooms.ShouldHaveSingleItem();
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Дефект 2 ADR018: <c>projectId</c> у <c>DeleteRoom</c> был необязательным, и контракт
    /// разрешал удалить комнату чужого проекта.
    /// </summary>
    [Fact]
    public async Task DeleteRoom_RoomFromAnotherProject_Throws()
    {
        var room = mock.CreateEmptyRoom(roomType, "101");

        _ = await Should.ThrowAsync<AccommodationRoomNotFoundException>(
            () => CreateService().DeleteRoom(AlienRoomId(room)));

        mock.Rooms.ShouldHaveSingleItem();
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>Дефект 4 ADR018.</summary>
    [Fact]
    public async Task DeleteRoom_InArchivedProject_Throws()
    {
        var room = mock.CreateEmptyRoom(roomType, "101");
        ArchiveProject();

        _ = await Should.ThrowAsync<ProjectDeactivatedException>(
            () => CreateService().DeleteRoom(RoomId(room)));

        mock.Rooms.ShouldHaveSingleItem();
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }
}
