using System.Net;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.Web.Accommodation.Rooms;

namespace JoinRpg.Blazor.Client.ApiClients;

public class AccommodationRoomsClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider)
    : IAccommodationRoomsClient
{
    public async Task<RoomTypeRoomsViewModel> GetRooms(AccommodationTypeIdentification typeId)
        => await httpClient.GetFromJsonAsync<RoomTypeRoomsViewModel>(
            $"webapi/accommodation-rooms/GetRooms?projectId={typeId.ProjectId.Value}&roomTypeId={typeId}")
            ?? throw new Exception("Couldn't get result from server");

    public async Task<RoomTypeRoomsViewModel> AddRooms(AccommodationTypeIdentification typeId, string roomNames)
    {
        var response = await Post(typeId.ProjectId, "AddRooms", new AddRoomsRequest(typeId, roomNames));
        return await response.Content.ReadFromJsonAsync<RoomTypeRoomsViewModel>()
            ?? throw new Exception("Couldn't get result from server");
    }

    public async Task RenameRoom(AccommodationRoomIdentification roomId, string name)
        => await Post(roomId.ProjectId, "RenameRoom", new RenameRoomRequest(roomId, name));

    public async Task DeleteRoom(AccommodationRoomIdentification roomId)
        => await Post(roomId.ProjectId, "DeleteRoom", roomId);

    public async Task OccupyRoom(
        AccommodationRoomIdentification roomId,
        IReadOnlyCollection<AccommodationRequestIdentification> groupIds)
        => await Post(roomId.ProjectId, "OccupyRoom", new OccupyRoomRequest(roomId, groupIds));

    public async Task UnOccupyGroup(AccommodationRequestIdentification groupId)
        => await Post(groupId.ProjectId, "UnOccupyGroup", groupId);

    public async Task UnOccupyRoom(AccommodationRoomIdentification roomId)
        => await Post(roomId.ProjectId, "UnOccupyRoom", roomId);

    public async Task UnOccupyRoomType(AccommodationTypeIdentification typeId)
        => await Post(typeId.ProjectId, "UnOccupyRoomType", typeId);

    private async Task<HttpResponseMessage> Post<TBody>(ProjectIdentification projectId, string action, TBody body)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsJsonAsync(
            $"webapi/accommodation-rooms/{action}?projectId={projectId.Value}",
            body);

        // Причину отказа сервер отдаёт текстом, чтобы контрол мог показать её мастеру
        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound)
        {
            var reason = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(reason))
            {
                throw new AccommodationRoomsOperationRefusedException(reason);
            }
        }

        return response.EnsureSuccessStatusCode();
    }
}
