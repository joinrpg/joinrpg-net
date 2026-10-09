using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.Accommodation;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

/// <summary>
/// Ручки острова <see cref="RoomTypeEditForm"/>: чтение, создание и изменение типа проживания.
/// </summary>
/// <remarks>
/// Номер типа приходит голым числом и склеивается с проектом из query — тем самым, по которому
/// проверены права. Так тип чужого проекта подсунуть нельзя.
/// </remarks>
[Route("/webapi/room-type/[action]")]
[RequireMaster]
[IgnoreAntiforgeryToken]
public class RoomTypeEditController(IRoomTypeEditClient client) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RoomTypeEditViewModel>> Get(
        [FromQuery] ProjectIdentification projectId,
        [FromQuery] int roomTypeId)
        => await client.GetRoomType(new AccommodationTypeIdentification(projectId, roomTypeId));

    [HttpGet]
    [RequireMaster(Permission.CanManageAccommodation)]
    public async Task<ActionResult<RoomTypeEditViewModel>> New([FromQuery] ProjectIdentification projectId)
        => await client.GetNewRoomType(projectId);

    [HttpPost]
    [RequireMaster(Permission.CanManageAccommodation)]
    public async Task<ActionResult> Create(
        [FromQuery] ProjectIdentification projectId,
        [FromBody] RoomTypeEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return ValidationErrors();
        }

        await client.CreateRoomType(projectId, model);
        return Ok();
    }

    [HttpPost]
    [RequireMaster(Permission.CanManageAccommodation)]
    public async Task<ActionResult> Update(
        [FromQuery] ProjectIdentification projectId,
        [FromQuery] int roomTypeId,
        [FromBody] RoomTypeEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return ValidationErrors();
        }

        await client.UpdateRoomType(new AccommodationTypeIdentification(projectId, roomTypeId), model);
        return Ok();
    }

    /// <summary>
    /// Сообщения валидации предназначены мастеру — отдаём их текстом, форма покажет как есть.
    /// </summary>
    private BadRequestObjectResult ValidationErrors()
        => BadRequest(string.Join(" ", ModelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage)));
}
