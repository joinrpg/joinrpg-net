using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Portal.Controllers.WebApi;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.Accommodation.Rooms;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Test.Controllers;

/// <summary>
/// Ручки контрола расселения: сверка проекта и раскладка доменных исключений по кодам ответа
/// с причиной, которую остров покажет мастеру.
/// </summary>
public class AccommodationRoomsControllerTest
{
    private static readonly ProjectIdentification ProjectId = new(1611);
    private static readonly ProjectIdentification AlienProjectId = new(1612);

    private static AccommodationRoomIdentification Room(ProjectIdentification projectId) => new(projectId, 5);

    private static AccommodationRequestIdentification Group(ProjectIdentification projectId) => new(projectId, 7);

    [Fact]
    public async Task OccupyRoom_AlienRoomIsRejectedBeforeService()
    {
        var client = new FakeClient();

        var result = await new AccommodationRoomsController(client)
            .OccupyRoom(ProjectId, new OccupyRoomRequest(Room(AlienProjectId), [Group(ProjectId)]));

        _ = result.ShouldBeOfType<BadRequestObjectResult>();
        client.Called.ShouldBeFalse();
    }

    [Fact]
    public async Task OccupyRoom_AlienGroupIsRejectedBeforeService()
    {
        var client = new FakeClient();

        var result = await new AccommodationRoomsController(client)
            .OccupyRoom(ProjectId, new OccupyRoomRequest(Room(ProjectId), [Group(AlienProjectId)]));

        _ = result.ShouldBeOfType<BadRequestObjectResult>();
        client.Called.ShouldBeFalse();
    }

    [Fact]
    public async Task OccupyRoom_NoGroupsIsBadRequest()
    {
        var client = new FakeClient();

        var result = await new AccommodationRoomsController(client)
            .OccupyRoom(ProjectId, new OccupyRoomRequest(Room(ProjectId), []));

        _ = result.ShouldBeOfType<BadRequestObjectResult>();
        client.Called.ShouldBeFalse();
    }

    [Fact]
    public async Task OccupyRoom_InsufficientSpaceIsBadRequestWithReason()
    {
        var client = new FakeClient { Failure = new JoinRpgInsufficientRoomSpaceException(Room(ProjectId)) };

        var result = await new AccommodationRoomsController(client)
            .OccupyRoom(ProjectId, new OccupyRoomRequest(Room(ProjectId), [Group(ProjectId)]));

        result.ShouldBeOfType<BadRequestObjectResult>().Value.ShouldBe("В комнате не хватает мест");
    }

    [Fact]
    public async Task OccupyRoom_UnknownRoomIsNotFound()
    {
        var client = new FakeClient { Failure = new AccommodationRoomNotFoundException(Room(ProjectId)) };

        var result = await new AccommodationRoomsController(client)
            .OccupyRoom(ProjectId, new OccupyRoomRequest(Room(ProjectId), [Group(ProjectId)]));

        _ = result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteRoom_OccupiedIsBadRequestWithReason()
    {
        var client = new FakeClient { Failure = new RoomIsOccupiedException(Room(ProjectId)) };

        var result = await new AccommodationRoomsController(client).DeleteRoom(ProjectId, Room(ProjectId));

        result.ShouldBeOfType<BadRequestObjectResult>().Value.ShouldBe("В комнате живут игроки — сначала выселите их");
    }

    [Fact]
    public async Task UnOccupyGroup_IsPassedToService()
    {
        var client = new FakeClient();

        var result = await new AccommodationRoomsController(client).UnOccupyGroup(ProjectId, Group(ProjectId));

        _ = result.ShouldBeOfType<OkResult>();
        client.Called.ShouldBeTrue();
    }

    /// <summary>
    /// Управление комнатами и расселение — разные права (ADR018, §10). Сервис проверяет их сам,
    /// но атрибут не пускает до сервиса заведомо чужой запрос и даёт мастеру понятный отказ.
    /// </summary>
    [Theory]
    [InlineData(nameof(AccommodationRoomsController.AddRooms), Permission.CanManageAccommodation)]
    [InlineData(nameof(AccommodationRoomsController.RenameRoom), Permission.CanManageAccommodation)]
    [InlineData(nameof(AccommodationRoomsController.DeleteRoom), Permission.CanManageAccommodation)]
    [InlineData(nameof(AccommodationRoomsController.GetRooms), Permission.CanSetPlayersAccommodations)]
    [InlineData(nameof(AccommodationRoomsController.OccupyRoom), Permission.CanSetPlayersAccommodations)]
    [InlineData(nameof(AccommodationRoomsController.UnOccupyGroup), Permission.CanSetPlayersAccommodations)]
    [InlineData(nameof(AccommodationRoomsController.UnOccupyRoom), Permission.CanSetPlayersAccommodations)]
    [InlineData(nameof(AccommodationRoomsController.UnOccupyRoomType), Permission.CanSetPlayersAccommodations)]
    public void ActionRequiresPermission(string action, Permission permission)
    {
        var method = typeof(AccommodationRoomsController).GetMethod(action)
            ?? throw new InvalidOperationException($"Нет экшена {action}");

        var attribute = method.GetCustomAttributes(typeof(RequireMaster), inherit: true)
            .Cast<RequireMaster>()
            .ShouldHaveSingleItem();
        attribute.Policy.ShouldBe(new RequireMaster(permission).Policy);
    }

    [Fact]
    public void EveryActionIsCoveredByPermissionTest()
    {
        var actions = typeof(AccommodationRoomsController)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .Order(StringComparer.Ordinal)
            .ToList();

        actions.ShouldBe(
            [
                nameof(AccommodationRoomsController.AddRooms),
                nameof(AccommodationRoomsController.DeleteRoom),
                nameof(AccommodationRoomsController.GetRooms),
                nameof(AccommodationRoomsController.OccupyRoom),
                nameof(AccommodationRoomsController.RenameRoom),
                nameof(AccommodationRoomsController.UnOccupyGroup),
                nameof(AccommodationRoomsController.UnOccupyRoom),
                nameof(AccommodationRoomsController.UnOccupyRoomType),
            ],
            customMessage: "Новый экшен — добавьте его в ActionRequiresPermission");
    }

    private sealed class FakeClient : IAccommodationRoomsClient
    {
        public bool Called { get; private set; }

        public Exception? Failure { get; init; }

        private Task Call()
        {
            Called = true;
            return Failure is null ? Task.CompletedTask : Task.FromException(Failure);
        }

        public Task<RoomTypeRoomsViewModel> GetRooms(AccommodationTypeIdentification typeId) => throw new NotSupportedException();
        public Task<RoomTypeRoomsViewModel> AddRooms(AccommodationTypeIdentification typeId, string roomNames) => throw new NotSupportedException();
        public Task RenameRoom(AccommodationRoomIdentification roomId, string name) => Call();
        public Task DeleteRoom(AccommodationRoomIdentification roomId) => Call();
        public Task OccupyRoom(AccommodationRoomIdentification roomId, IReadOnlyCollection<AccommodationRequestIdentification> groupIds) => Call();
        public Task UnOccupyGroup(AccommodationRequestIdentification groupId) => Call();
        public Task UnOccupyRoom(AccommodationRoomIdentification roomId) => Call();
        public Task UnOccupyRoomType(AccommodationTypeIdentification typeId) => Call();
    }
}
