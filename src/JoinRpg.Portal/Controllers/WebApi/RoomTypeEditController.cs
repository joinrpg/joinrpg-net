using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.Accommodation;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

/// <summary>
/// Ручки острова <see cref="RoomTypeEditForm"/>: чтение, создание и изменение типа проживания,
/// переименование его категории комнат. Здесь же — удаление типа для кнопки списка типов.
/// </summary>
/// <remarks>
/// Номер типа (и категории) приходит голым числом и склеивается с проектом из query — тем самым, по которому
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

    [HttpPost]
    [RequireMaster(Permission.CanManageAccommodation)]
    public async Task<ActionResult> RenameRoomCategory(
        [FromQuery] ProjectIdentification projectId,
        [FromQuery] int roomCategoryId,
        [FromBody] RoomCategoryRenameViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return ValidationErrors();
        }

        await client.RenameRoomCategory(new RoomCategoryIdentification(projectId, roomCategoryId), model);
        return Ok();
    }

    /// <summary>
    /// Удаление типа проживания.
    /// </summary>
    /// <remarks>
    /// В отличие от остальных ручек контроллера, тела здесь нет: всё в query. Без JSON-тела
    /// запрос может отправить и обычная форма с чужого сайта, поэтому antiforgery-токен проверяется
    /// явно — атрибут на экшене перекрывает <c>[IgnoreAntiforgeryToken]</c> контроллера. Остров
    /// присылает токен заголовком.
    /// </remarks>
    [HttpPost]
    [RequireMaster(Permission.CanManageAccommodation)]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Delete(
        [FromQuery] ProjectIdentification projectId,
        [FromQuery] int roomTypeId)
    {
        var typeId = new AccommodationTypeIdentification(projectId, roomTypeId);
        try
        {
            await client.DeleteRoomType(typeId);
            return Ok();
        }
        catch (AccommodationTypeIsOccupiedException)
        {
            return BadRequest("В комнатах этого типа живут игроки — сначала выселите их");
        }
        catch (JoinRpgEntityNotFoundException)
        {
            // Повторное нажатие или тип уже удалили в соседней вкладке. 400, а не 404: текст причины
            // HTTP-клиент острова показывает только для 400.
            return BadRequest("Тип проживания не найден — возможно, его уже удалили");
        }
    }

    /// <summary>
    /// Сообщения валидации предназначены мастеру — отдаём их текстом, форма покажет как есть.
    /// </summary>
    private BadRequestObjectResult ValidationErrors()
        => BadRequest(string.Join(" ", ModelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage)));
}
