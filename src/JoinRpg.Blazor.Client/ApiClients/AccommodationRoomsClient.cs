using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.Web.Accommodation.Rooms;

namespace JoinRpg.Blazor.Client.ApiClients;

public class AccommodationRoomsClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider)
    : IAccommodationRoomsClient
{
    public async Task<RoomTypeRoomsViewModel> GetRooms(AccommodationTypeIdentification typeId)
        => await (await httpClient.GetAsync(
            $"webapi/accommodation-rooms/GetRooms?projectId={typeId.ProjectId.Value}&roomTypeId={typeId}"))
            .ReadFromJsonOrThrowProblem<RoomTypeRoomsViewModel>();

    public async Task<RoomTypeRoomsViewModel> AddRooms(AccommodationTypeIdentification typeId, string roomNames)
        => await (await Post(typeId.ProjectId, "AddRooms", new AddRoomsRequest(typeId, roomNames)))
            .ReadFromJsonOrThrowProblem<RoomTypeRoomsViewModel>();

    public async Task RenameRoom(AccommodationRoomIdentification roomId, string name)
        => await (await Post(roomId.ProjectId, "RenameRoom", new RenameRoomRequest(roomId, name))).EnsureSuccessOrThrowProblem();

    public async Task DeleteRoom(AccommodationRoomIdentification roomId)
        => await (await Post(roomId.ProjectId, "DeleteRoom", roomId)).EnsureSuccessOrThrowProblem();

    public async Task OccupyRoom(
        AccommodationRoomIdentification roomId,
        IReadOnlyCollection<AccommodationRequestIdentification> groupIds)
        => await (await Post(roomId.ProjectId, "OccupyRoom", new OccupyRoomRequest(roomId, groupIds))).EnsureSuccessOrThrowProblem();

    public async Task UnOccupyGroup(AccommodationRequestIdentification groupId)
        => await (await Post(groupId.ProjectId, "UnOccupyGroup", groupId)).EnsureSuccessOrThrowProblem();

    public async Task UnOccupyRoom(AccommodationRoomIdentification roomId)
        => await (await Post(roomId.ProjectId, "UnOccupyRoom", roomId)).EnsureSuccessOrThrowProblem();

    public async Task UnOccupyRoomType(AccommodationTypeIdentification typeId)
        => await (await Post(typeId.ProjectId, "UnOccupyRoomType", typeId)).EnsureSuccessOrThrowProblem();

    // Тела нет: проект — в адресе.
    public async Task UnOccupyAll(ProjectIdentification projectId)
        => await (await Send(projectId, "UnOccupyAll", content: null)).EnsureSuccessOrThrowProblem();

    private Task<HttpResponseMessage> Post<TBody>(ProjectIdentification projectId, string action, TBody body)
        => Send(projectId, action, JsonContent.Create(body));

    private async Task<HttpResponseMessage> Send(ProjectIdentification projectId, string action, HttpContent? content)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        return await httpClient.PostAsync(
            $"webapi/accommodation-rooms/{action}?projectId={projectId.Value}",
            content);
    }
}
