using System.Net;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.Web.Accommodation;

namespace JoinRpg.Blazor.Client.ApiClients;

internal class RoomTypeEditClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider) : IRoomTypeEditClient
{
    public async Task<RoomTypeEditViewModel> GetRoomType(AccommodationTypeIdentification roomTypeId)
        => await httpClient.GetFromJsonAsync<RoomTypeEditViewModel>(
            $"webapi/room-type/get?projectId={roomTypeId.ProjectId.Value}&roomTypeId={roomTypeId.AccommodationTypeId}")
            ?? throw new Exception("Couldn't get result from server");

    public Task CreateRoomType(ProjectIdentification projectId, RoomTypeEditViewModel model)
        => Post($"webapi/room-type/create?projectId={projectId.Value}", model);

    public Task UpdateRoomType(AccommodationTypeIdentification roomTypeId, RoomTypeEditViewModel model)
        => Post(
            $"webapi/room-type/update?projectId={roomTypeId.ProjectId.Value}&roomTypeId={roomTypeId.AccommodationTypeId}",
            model);

    private async Task Post(string uri, RoomTypeEditViewModel model)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsJsonAsync(uri, model);

        // Ошибки валидации приходят текстом, чтобы форма могла показать их мастеру
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
        }

        _ = response.EnsureSuccessStatusCode();
    }
}
