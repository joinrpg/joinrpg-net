using System.Net;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.Web.Accommodation.Rooms;

namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>
/// Ручки контрола расселения на странице «Комнаты» и чтение результата с самой страницы — общие для
/// сценариев поселения.
/// </summary>
internal static class AccommodationRoomsApi
{
    /// <summary>Адрес ручки — ровно такой, какой собирает HTTP-клиент острова.</summary>
    public static string Url(ProjectIdentification projectId, string action)
        => $"webapi/accommodation-rooms/{action}?projectId={projectId.Value}";

    public static Task<HttpResponseMessage> OccupyAsync(
        this HttpClient client,
        string? antiforgeryToken,
        ProjectIdentification projectId,
        AccommodationRoomIdentification roomId,
        params AccommodationRequestIdentification[] groupIds)
        => client.PostJsonWithTokenHeaderAsync(
            Url(projectId, "OccupyRoom"),
            new OccupyRoomRequest(roomId, groupIds),
            antiforgeryToken);

    public static Task<HttpResponseMessage> UnOccupyGroupAsync(
        this HttpClient client,
        string? antiforgeryToken,
        AccommodationRequestIdentification groupId)
        => client.PostJsonWithTokenHeaderAsync(Url(groupId.ProjectId, "UnOccupyGroup"), groupId, antiforgeryToken);

    public static Task<HttpResponseMessage> UnOccupyRoomTypeAsync(
        this HttpClient client,
        string? antiforgeryToken,
        AccommodationTypeIdentification typeId)
        => client.PostJsonWithTokenHeaderAsync(Url(typeId.ProjectId, "UnOccupyRoomType"), typeId, antiforgeryToken);

    public static Task<HttpResponseMessage> UnOccupyRoomAsync(
        this HttpClient client,
        string? antiforgeryToken,
        AccommodationRoomIdentification roomId)
        => client.PostJsonWithTokenHeaderAsync(Url(roomId.ProjectId, "UnOccupyRoom"), roomId, antiforgeryToken);

    public static Task<HttpResponseMessage> AddRoomsAsync(
        this HttpClient client,
        string? antiforgeryToken,
        AccommodationTypeIdentification typeId,
        string roomNames)
        => client.PostJsonWithTokenHeaderAsync(Url(typeId.ProjectId, "AddRooms"), new AddRoomsRequest(typeId, roomNames), antiforgeryToken);

    public static Task<HttpResponseMessage> RenameRoomAsync(
        this HttpClient client,
        string? antiforgeryToken,
        AccommodationRoomIdentification roomId,
        string name)
        => client.PostJsonWithTokenHeaderAsync(Url(roomId.ProjectId, "RenameRoom"), new RenameRoomRequest(roomId, name), antiforgeryToken);

    public static Task<HttpResponseMessage> DeleteRoomAsync(
        this HttpClient client,
        string? antiforgeryToken,
        AccommodationRoomIdentification roomId)
        => client.PostJsonWithTokenHeaderAsync(Url(roomId.ProjectId, "DeleteRoom"), roomId, antiforgeryToken);

    /// <summary>
    /// Модель контрола так, как её запрашивает остров: тип проживания — голым числом в query,
    /// проект к нему приклеивает биндер.
    /// </summary>
    public static Task<HttpResponseMessage> GetRoomsAsync(this HttpClient client, AccommodationTypeIdentification typeId)
        => client.GetAsync(Url(typeId.ProjectId, "GetRooms") + $"&roomTypeId={typeId.AccommodationTypeId}");

    /// <summary>
    /// Сколько человек живёт в комнате по данным страницы комнат: ячейка «Места» строки комнаты
    /// («занято / вместимость»). Остров пререндерится на сервере, поэтому это та же разметка,
    /// что видит мастер, — проверять состояние по странице честнее, чем запросом в базу.
    /// </summary>
    public static async Task<int> GetOccupancyAsync(this HttpClient client, string roomTypeDetailsUrl, int roomId)
    {
        var response = await client.GetAsync(roomTypeDetailsUrl);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, $"Страница {roomTypeDetailsUrl} не открылась");

        var document = await response.AsHtmlDocument();

        var cell = document.DocumentNode.SelectSingleNode(
                $"//tr[@data-room-id='{roomId}']/td[contains(@class, 'rooms-room-occupancy')]")
            ?? throw new InvalidOperationException(
                $"На странице {roomTypeDetailsUrl} нет комнаты {roomId}");

        var occupied = WebUtility.HtmlDecode(cell.InnerText).Split('/')[0].Trim();
        return int.Parse(occupied);
    }
}
