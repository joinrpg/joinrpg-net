using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.Web.Accommodation;

namespace JoinRpg.Blazor.Client.ApiClients;

internal class RoomTypeEditClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider) : IRoomTypeEditClient
{
    public async Task<RoomTypeEditViewModel> GetRoomType(AccommodationTypeIdentification roomTypeId)
        => await (await httpClient.GetAsync(
            $"webapi/room-type/get?projectId={roomTypeId.ProjectId.Value}&roomTypeId={roomTypeId.AccommodationTypeId}"))
            .ReadFromJsonOrThrowProblem<RoomTypeEditViewModel>();

    public async Task<RoomTypeEditViewModel> GetNewRoomType(ProjectIdentification projectId)
        => await (await httpClient.GetAsync($"webapi/room-type/new?projectId={projectId.Value}"))
            .ReadFromJsonOrThrowProblem<RoomTypeEditViewModel>();

    public Task CreateRoomType(ProjectIdentification projectId, RoomTypeEditViewModel model)
        => Post($"webapi/room-type/create?projectId={projectId.Value}", model);

    public Task UpdateRoomType(AccommodationTypeIdentification roomTypeId, RoomTypeEditViewModel model)
        => Post(
            $"webapi/room-type/update?projectId={roomTypeId.ProjectId.Value}&roomTypeId={roomTypeId.AccommodationTypeId}",
            model);

    public Task RenameRoomCategory(RoomCategoryIdentification roomCategoryId, RoomCategoryRenameViewModel model)
        => Post(
            $"webapi/room-type/RenameRoomCategory?projectId={roomCategoryId.ProjectId.Value}&roomCategoryId={roomCategoryId.RoomCategoryId}",
            model);

    public Task DeleteRoomType(AccommodationTypeIdentification roomTypeId)
        => Send($"webapi/room-type/delete?projectId={roomTypeId.ProjectId.Value}&roomTypeId={roomTypeId.AccommodationTypeId}", content: null);

    private Task Post<TModel>(string uri, TModel model) => Send(uri, JsonContent.Create(model));

    private async Task Send(string uri, HttpContent? content)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsync(uri, content);

        // Ошибки валидации и отказы домена форма показывает мастеру
        await response.EnsureSuccessOrThrowProblem();
    }
}
