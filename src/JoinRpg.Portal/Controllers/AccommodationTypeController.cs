using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.ProjectMetadata;
using JoinRpg.Web.Models.Accommodation;
using JoinRpg.WebPortal.Managers.Accommodation;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[MasterAuthorize()]
[Route("{projectId}/rooms/[action]")]
public class AccommodationTypeController(
    IAccommodationService accommodationService,
    IAccommodationTypeService accommodationTypeService,
    IAccommodationRepository accommodationRepository,
    IClaimsRepository claimsRepository,
    IProjectMetadataRepository projectMetadataRepository,
    RoomTypeRoomsViewService roomTypeRoomsViewService,
    ICurrentUserAccessor currentUserAccessor) : Common.JoinControllerGameBase
{

    /// <summary>
    /// Shows list of registered room types
    /// </summary>
    [HttpGet("~/{projectId}/rooms/")]
    public async Task<ActionResult> Index(ProjectIdentification projectId)
    {
        var project = await projectMetadataRepository.GetProjectMetadata(projectId);
        if (project == null)
        {
            return NotFound($"Project {projectId} not found");
        }

        if (!project.AccommodationSettings.Enabled)
        {
            return RedirectToAction("Edit", "Game", new { projectId = projectId.Value });
        }

        return View(new AccommodationListViewModel(project,
            await accommodationRepository.GetRoomTypesForProject(projectId),
            await claimsRepository.GetClaimsForRoomType(projectId, ClaimStatusSpec.Active, roomTypeId: null),
            currentUserAccessor));
    }

    /// <summary>
    /// Shows "Add room type" form
    /// </summary>
    [MasterAuthorize(Permission.CanManageAccommodation)]
    [HttpGet]
    public async Task<ActionResult> AddRoomType(int projectId)
    {
        var pi = await projectMetadataRepository.GetProjectMetadata(new(projectId));
        return View(new RoomTypeViewModel(currentUserAccessor.UserIdentification, pi));
    }

    /// <summary>
    /// Shows "Edit room type" form
    /// </summary>
    [HttpGet("~/{projectId}/rooms/{roomTypeId}/edit")]
    public async Task<ActionResult> EditRoomType(AccommodationTypeIdentification roomTypeId)
    {
        var pi = await projectMetadataRepository.GetProjectMetadata(roomTypeId.ProjectId);

        // Тип проживания — настройка проекта, он уже есть в метаданных (ADR015).
        var typeInfo = pi.AccommodationSettings.GetTypeById(roomTypeId);

        return View(new RoomTypeViewModel(typeInfo, currentUserAccessor.UserIdentification, pi));
    }

    /// <summary>
    /// Страница «Комнаты» типа проживания: показывает комнаты, жильцов и нерасселённые заявки
    /// и позволяет ими управлять.
    /// </summary>
    [MasterAuthorize(Permission.CanSetPlayersAccommodations)]
    [HttpGet("~/{projectId}/rooms/{roomTypeId}/details")]
    public async Task<ActionResult> RoomTypeDetails(AccommodationTypeIdentification roomTypeId)
    {
        var viewModel = await roomTypeRoomsViewService.GetRoomTypeRooms(roomTypeId);
        if (viewModel is null)
        {
            return NotFound($"Room type {roomTypeId} not found");
        }

        return View(viewModel);
    }

    /// <summary>
    /// Saves room type.
    /// If data are valid, redirects to Index
    /// If not, returns to edit mode
    /// </summary>
    [MasterAuthorize(Permission.CanManageAccommodation)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> SaveRoomType(RoomTypeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            if (model.Id == 0)
            {
                return View("AddRoomType", model);
            }

            return View("EditRoomType", model);
        }

        var projectId = new ProjectIdentification(model.ProjectId);
        var request = new AccommodationTypeRequest(
            model.Name,
            new MarkdownString(model.DescriptionEditable ?? ""),
            model.Cost,
            model.Capacity,
            model.IsPlayerSelectable);

        if (model.Id == 0)
        {
            _ = await accommodationTypeService.CreateAccommodationType(projectId, request);
        }
        else
        {
            await accommodationTypeService.UpdateAccommodationType(
                new AccommodationTypeIdentification(projectId, model.Id), request);
        }

        return RedirectToAction("Index", new { projectId = model.ProjectId });
    }

    /// <summary>
    /// Removes room type
    /// </summary>
    [MasterAuthorize(Permission.CanManageAccommodation)]
    [HttpGet]
    public async Task<ActionResult> DeleteRoomType(AccommodationTypeIdentification roomTypeId)
    {
        await accommodationTypeService.DeleteAccommodationType(roomTypeId);
        return RedirectToAction("Index", new { ProjectId = roomTypeId.ProjectId.Value });
    }

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

    [MasterAuthorize(Permission.CanSetPlayersAccommodations)]
    [HttpGet]
    public async Task<ActionResult> OccupyAll(ProjectIdentification projectId)
    {
        var project = await projectMetadataRepository.GetProjectMetadata(projectId);
        if (project == null)
        {
            return NotFound($"Project {projectId} not found");
        }

        if (!project.AccommodationSettings.Enabled)
        {
            return RedirectToAction("Edit", "Game", new { projectId = projectId.Value });
        }

        //TODO: Implement mass occupation

        return RedirectToAction("Index");
    }

    /// <summary>
    /// Выселяет всех жильцов всех комнат проекта
    /// </summary>
    [MasterAuthorize(Permission.CanSetPlayersAccommodations)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> UnOccupyAll(ProjectIdentification projectId)
    {
        var project = await projectMetadataRepository.GetProjectMetadata(projectId);
        if (project == null)
        {
            return NotFound($"Project {projectId} not found");
        }

        if (!project.AccommodationSettings.Enabled)
        {
            return RedirectToAction("Edit", "Game", new { projectId = projectId.Value });
        }

        await accommodationService.UnOccupyAllRooms(projectId);

        return RedirectToAction("Index");
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

    /// <summary>
    /// Выселяет всех жильцов всех комнат данного типа проживания
    /// </summary>
    [MasterAuthorize(Permission.CanSetPlayersAccommodations)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> UnOccupyRoomsByType(AccommodationTypeIdentification roomTypeId)
    {
        try
        {
            await accommodationService.UnOccupyRoomType(roomTypeId);
        }
        catch (AccommodationTypeNotFoundException)
        {
            return NotFound();
        }

        return RedirectToAction("RoomTypeDetails", "AccommodationType",
            new { ProjectId = roomTypeId.ProjectId.Value, RoomTypeId = roomTypeId.AccommodationTypeId });
    }

    /// <summary>
    /// Удаляет комнату
    /// </summary>
    [MasterAuthorize(Permission.CanManageAccommodation)]
    [HttpDelete]
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
}
