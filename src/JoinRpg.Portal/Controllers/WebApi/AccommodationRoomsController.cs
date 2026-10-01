using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.WebPortal.Managers.Accommodation;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

/// <summary>
/// Операции над комнатами и расселением, которые вызывает скрипт <c>rooms.js</c> со страницы
/// «Комнаты»: отвечают кодами ответа, а не вью.
/// </summary>
/// <remarks>
/// Адреса у всех экшенов заданы явно и умышленно легаси-вида (<c>/{projectId}/rooms/...</c>,
/// без префикса <c>/webapi</c>, как у соседей по папке): их собирает строкой <c>rooms.js</c>,
/// поэтому переезд кода в другой класс не должен менять ни один внешний адрес.
/// В частности <see cref="DeleteRoom"/> раньше получал адрес не из своего атрибута, а из
/// маршрута-соглашения на классе (<c>{projectId}/rooms/[action]</c>) — здесь такого маршрута
/// нет, и прежний адрес закреплён явно.
/// </remarks>
[MasterAuthorize()]
public class AccommodationRoomsController(
    IAccommodationService accommodationService,
    IProjectMetadataRepository projectMetadataRepository) : ControllerBase
{
    [MasterAuthorize(Permission.CanSetPlayersAccommodations)]
    [HttpPost("~/{projectId}/rooms/occupyroom")]
    public async Task<ActionResult> OccupyRoom(
        ProjectIdentification projectId,
        AccommodationRoomIdentification? room,
        string reqId)
    {
        // Комнату собирает модель-биндер (ProjectEntityIdModelBinder склеивает голое число из
        // запроса с проектом маршрута). Группы приходят списком через запятую — биндером такое
        // не разобрать, поэтому они по-прежнему разбираются здесь.
        var groupIds = (reqId ?? "").Split(',')
            .Select(s => int.TryParse(s, out var val) ? val : 0)
            .Where(val => val > 0)
            .Select(val => new AccommodationRequestIdentification(projectId, val))
            .ToList();

        if (room is null || !ModelState.IsValid || groupIds.Count == 0)
        {
            return BadRequest();
        }

        // Проверки прав, активности проекта и принадлежности комнаты и групп одному пулу делает
        // сервис (ADR018, дефекты 1, 4, 5) — контроллеру остаётся разложить доменные исключения
        // по кодам ответа. Ловить всё подряд с кодом 500 больше незачем.
        try
        {
            await accommodationService.OccupyRoom(room, groupIds);
            return Ok();
        }
        catch (AccommodationRoomNotFoundException)
        {
            return NotFound();
        }
        catch (AccommodationGroupNotFoundException)
        {
            return NotFound();
        }
        catch (JoinRpgInsufficientRoomSpaceException)
        {
            return BadRequest();
        }
    }

    /// <summary>
    /// Выселяет одну группу жильцов из комнаты
    /// </summary>
    /// <remarks>
    /// Адрес маршрута оставлен прежним (<c>unoccupyroom</c>): его собирает строкой скрипт
    /// <c>rooms.js</c>, а имя экшена приведено к тому, что операция делает на самом деле.
    /// </remarks>
    [MasterAuthorize(Permission.CanSetPlayersAccommodations)]
    [HttpPost("~/{projectId}/rooms/unoccupyroom")]
    public async Task<ActionResult> UnOccupyGroup(AccommodationRequestIdentification? reqId)
    {
        // Типизированный идентификатор собирает модель-биндер: голое число из запроса он
        // склеивает с текущим проектом маршрута (ProjectEntityIdModelBinder).
        if (reqId is null || !ModelState.IsValid)
        {
            return BadRequest();
        }

        try
        {
            await accommodationService.UnOccupyGroup(reqId);
            return Ok();
        }
        catch (AccommodationGroupNotFoundException)
        {
            return NotFound();
        }
    }

    [MasterAuthorize(Permission.CanManageAccommodation)]
    [HttpPost("~/{projectId}/rooms/addroom")]
    public async Task<ActionResult> AddRoom(AccommodationTypeIdentification? roomTypeId, string name)
    {
        if (roomTypeId is null || !ModelState.IsValid)
        {
            return BadRequest();
        }

        //TODO: Implement room names checking
        //TODO: Implement new rooms HTML returning
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(roomTypeId.ProjectId);

        // Категорию по типу проживания знают только метаданные — конвертации идентификаторов
        // в домене нет и заводить её нельзя (ADR018, §2).
        var typeInfo = projectInfo.AccommodationSettings.GetTypeByIdOrDefault(roomTypeId);
        if (typeInfo is null)
        {
            return NotFound();
        }

        // Синтаксис поля ввода («1,2,5-8») разбирает web-слой: сервис принимает готовые имена.
        var roomNames = RoomNamesParser.Parse(name);
        if (roomNames.Count == 0)
        {
            return BadRequest();
        }

        _ = await accommodationService.AddRooms(typeInfo.RoomCategoryId, roomNames);
        return StatusCode(201);
    }

    /// <summary>
    /// Переименовывает комнату
    /// </summary>
    [MasterAuthorize(Permission.CanManageAccommodation)]
    [HttpPost("~/{projectId}/rooms/editroom")]
    public async Task<ActionResult> EditRoom(AccommodationRoomIdentification? room, string name)
    {
        if (room is null || !ModelState.IsValid)
        {
            return BadRequest();
        }

        try
        {
            await accommodationService.RenameRoom(room, name);
            return Ok();
        }
        catch (AccommodationRoomNotFoundException)
        {
            return NotFound();
        }
        catch (FieldRequiredException)
        {
            return BadRequest();
        }
    }

    /// <summary>
    /// Удаляет комнату
    /// </summary>
    [MasterAuthorize(Permission.CanManageAccommodation)]
    [HttpDelete("~/{projectId}/rooms/deleteroom")]
    public async Task<ActionResult> DeleteRoom(AccommodationRoomIdentification? roomId)
    {
        // Проверки прав, активности проекта и принадлежности комнаты проекту делает сервис
        // (ADR018, дефекты 1, 2, 4) — контроллеру остаётся отличить «неверный запрос» от «упало».
        if (roomId is null || !ModelState.IsValid)
        {
            return BadRequest();
        }

        try
        {
            await accommodationService.DeleteRoom(roomId);
            return Ok();
        }
        catch (AccommodationRoomNotFoundException)
        {
            return NotFound();
        }
        catch (RoomIsOccupiedException)
        {
            return BadRequest();
        }
    }
}
