using JoinRpg.Domain;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.Accommodation.Rooms;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

/// <summary>
/// Контрол расселения на странице «Комнаты» типа проживания (<see cref="RoomTypeRoomsControl"/>).
/// </summary>
/// <remarks>
/// Права, активность проекта и принадлежность комнаты и групп одному пулу проверяет сервис
/// расселения (ADR018, дефекты 1, 4, 5); атрибуты здесь повторяют его разделение прав, чтобы
/// до сервиса не доходил заведомо чужой запрос. Контроллеру остаётся сверить проект запроса
/// с проектом идентификаторов из тела и разложить доменные исключения по кодам ответа —
/// с причиной текстом: её контрол показывает мастеру.
///
/// <c>[IgnoreAntiforgeryToken]</c> здесь сознательно нет: остров присылает токен заголовком,
/// и глобальный фильтр antiforgery закрывает и эти ручки.
/// </remarks>
[Route("/webapi/accommodation-rooms/[action]")]
[RequireMaster]
public class AccommodationRoomsController(IAccommodationRoomsClient client) : ControllerBase
{
    private const string WrongProject = "Неверный запрос: объект не найден в проекте";
    private const string RoomNotFound = "Комната не найдена";
    private const string GroupNotFound = "Заявка на проживание не найдена";
    private const string TypeNotFound = "Тип проживания не найден";

    [HttpGet]
    [RequireMaster(Permission.CanSetPlayersAccommodations)]
    public async Task<ActionResult<RoomTypeRoomsViewModel>> GetRooms(
        [FromQuery] ProjectIdentification projectId,
        [FromQuery] AccommodationTypeIdentification? roomTypeId)
    {
        if (roomTypeId is null || !ModelState.IsValid || roomTypeId.ProjectId != projectId)
        {
            return BadRequest(WrongProject);
        }

        try
        {
            return await client.GetRooms(roomTypeId);
        }
        catch (AccommodationTypeNotFoundException)
        {
            return NotFound(TypeNotFound);
        }
    }

    [HttpPost]
    [RequireMaster(Permission.CanManageAccommodation)]
    public async Task<ActionResult<RoomTypeRoomsViewModel>> AddRooms(
        [FromQuery] ProjectIdentification projectId,
        [FromBody] AddRoomsRequest? request)
    {
        if (request is null || !ModelState.IsValid || request.TypeId.ProjectId != projectId)
        {
            return BadRequest(WrongProject);
        }

        try
        {
            return await client.AddRooms(request.TypeId, request.RoomNames);
        }
        catch (AccommodationTypeNotFoundException)
        {
            return NotFound(TypeNotFound);
        }
        catch (FieldRequiredException)
        {
            return BadRequest("Укажите хотя бы один номер комнаты");
        }
    }

    [HttpPost]
    [RequireMaster(Permission.CanManageAccommodation)]
    public async Task<ActionResult> RenameRoom(
        [FromQuery] ProjectIdentification projectId,
        [FromBody] RenameRoomRequest? request)
    {
        if (request is null || !ModelState.IsValid || request.RoomId.ProjectId != projectId)
        {
            return BadRequest(WrongProject);
        }

        try
        {
            await client.RenameRoom(request.RoomId, request.Name);
            return Ok();
        }
        catch (AccommodationRoomNotFoundException)
        {
            return NotFound(RoomNotFound);
        }
        catch (FieldRequiredException)
        {
            return BadRequest("Укажите название комнаты");
        }
    }

    [HttpPost]
    [RequireMaster(Permission.CanManageAccommodation)]
    public async Task<ActionResult> DeleteRoom(
        [FromQuery] ProjectIdentification projectId,
        [FromBody] AccommodationRoomIdentification? roomId)
    {
        if (roomId is null || !ModelState.IsValid || roomId.ProjectId != projectId)
        {
            return BadRequest(WrongProject);
        }

        try
        {
            await client.DeleteRoom(roomId);
            return Ok();
        }
        catch (AccommodationRoomNotFoundException)
        {
            return NotFound(RoomNotFound);
        }
        catch (RoomIsOccupiedException)
        {
            return BadRequest("В комнате живут игроки — сначала выселите их");
        }
    }

    [HttpPost]
    [RequireMaster(Permission.CanSetPlayersAccommodations)]
    public async Task<ActionResult> OccupyRoom(
        [FromQuery] ProjectIdentification projectId,
        [FromBody] OccupyRoomRequest? request)
    {
        if (request?.GroupIds is null
            || !ModelState.IsValid
            || request.RoomId.ProjectId != projectId
            || request.GroupIds.Any(g => g.ProjectId != projectId))
        {
            return BadRequest(WrongProject);
        }

        if (request.GroupIds.Count == 0)
        {
            return BadRequest("Не выбрано, кого заселять");
        }

        try
        {
            await client.OccupyRoom(request.RoomId, request.GroupIds);
            return Ok();
        }
        catch (AccommodationRoomNotFoundException)
        {
            return NotFound(RoomNotFound);
        }
        catch (AccommodationGroupNotFoundException)
        {
            return NotFound(GroupNotFound);
        }
        catch (JoinRpgInsufficientRoomSpaceException)
        {
            return BadRequest("В комнате не хватает мест");
        }
    }

    [HttpPost]
    [RequireMaster(Permission.CanSetPlayersAccommodations)]
    public async Task<ActionResult> UnOccupyGroup(
        [FromQuery] ProjectIdentification projectId,
        [FromBody] AccommodationRequestIdentification? groupId)
    {
        if (groupId is null || !ModelState.IsValid || groupId.ProjectId != projectId)
        {
            return BadRequest(WrongProject);
        }

        try
        {
            await client.UnOccupyGroup(groupId);
            return Ok();
        }
        catch (AccommodationGroupNotFoundException)
        {
            return NotFound(GroupNotFound);
        }
    }

    [HttpPost]
    [RequireMaster(Permission.CanSetPlayersAccommodations)]
    public async Task<ActionResult> UnOccupyRoom(
        [FromQuery] ProjectIdentification projectId,
        [FromBody] AccommodationRoomIdentification? roomId)
    {
        if (roomId is null || !ModelState.IsValid || roomId.ProjectId != projectId)
        {
            return BadRequest(WrongProject);
        }

        try
        {
            await client.UnOccupyRoom(roomId);
            return Ok();
        }
        catch (AccommodationRoomNotFoundException)
        {
            return NotFound(RoomNotFound);
        }
    }

    [HttpPost]
    [RequireMaster(Permission.CanSetPlayersAccommodations)]
    public async Task<ActionResult> UnOccupyRoomType(
        [FromQuery] ProjectIdentification projectId,
        [FromBody] AccommodationTypeIdentification? typeId)
    {
        if (typeId is null || !ModelState.IsValid || typeId.ProjectId != projectId)
        {
            return BadRequest(WrongProject);
        }

        try
        {
            await client.UnOccupyRoomType(typeId);
            return Ok();
        }
        catch (AccommodationTypeNotFoundException)
        {
            return NotFound(TypeNotFound);
        }
    }
}
