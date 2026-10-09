using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.Services.Impl.Test.Accommodation;

/// <summary>
/// Тесты управления комнатами (<c>AddRooms</c>/<c>RenameRoom</c>/<c>DeleteRoom</c>) поверх
/// агрегата плана поселения (ADR018, PR 4). Обвязка — <see cref="AccommodationServiceTestBase"/>.
/// </summary>
/// <remarks>
/// Тесты «без права», «комната чужого проекта» и «архивный проект» воспроизводят дефекты 1, 2 и 4
/// из ADR018: до переноса <c>EditRoom</c>/<c>DeleteRoom</c> не проверяли ни прав, ни проекта
/// (<c>projectId</c> был необязательным), а активность проекта не проверял ни один метод.
/// </remarks>
public class AccommodationRoomsServiceTest : AccommodationServiceTestBase
{
    private readonly ProjectAccommodationType roomType;

    public AccommodationRoomsServiceTest()
    {
        roomType = mock.CreateAccommodationType("Палатка", capacity: 4);
        mock.ReInitProjectInfo();
    }

    private RoomCategoryIdentification CategoryId
        => new(ProjectId, roomType.RoomCategoryId);

    [Fact]
    public async Task AddRooms_CreatesRoomsInCategory()
    {
        var created = await CreateService().AddRooms(CategoryId, ["101", "102"]);

        created.Count.ShouldBe(2);
        mock.Rooms.Select(room => room.Name).ShouldBe(["101", "102"]);
        mock.Rooms.ShouldAllBe(room => room.RoomCategoryId == roomType.RoomCategoryId);
        mock.Rooms.ShouldAllBe(room => room.RoomCategory == roomType.RoomCategory);
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

        await CreateService().RenameRoom(room.GetId(), "102");

        room.Name.ShouldBe("102");
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    /// <summary>Дефект 1 ADR018: <c>EditRoom</c> не проверял прав вообще.</summary>
    [Fact]
    public async Task RenameRoom_WithoutPermission_Throws()
    {
        var room = mock.CreateEmptyRoom(roomType, "101");

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).RenameRoom(room.GetId(), "102"));

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
            () => CreateService().RenameRoom(room.GetId(), "102"));

        room.Name.ShouldBe("101");
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task DeleteRoom_RemovesEmptyRoom()
    {
        var room = mock.CreateEmptyRoom(roomType, "101");

        await CreateService().DeleteRoom(room.GetId());

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
            () => CreateService().DeleteRoom(room.GetId()));

        mock.Rooms.ShouldHaveSingleItem();
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>Дефект 1 ADR018: <c>DeleteRoom</c> не проверял прав вообще.</summary>
    [Fact]
    public async Task DeleteRoom_WithoutPermission_Throws()
    {
        var room = mock.CreateEmptyRoom(roomType, "101");

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).DeleteRoom(room.GetId()));

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
            () => CreateService().DeleteRoom(room.GetId()));

        mock.Rooms.ShouldHaveSingleItem();
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }
}
