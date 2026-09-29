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
    public async Task<ActionResult> OccupyRoom(int projectId, int roomTypeId, int room, string reqId)
    {
        try
        {
            var ids = reqId.Split(',')
                .Select(s => int.TryParse(s, out var val) ? val : 0)
                .Where(val => val > 0)
                .ToList();
            if (ids.Count > 0)
            {
                await accommodationService.OccupyRoom(new OccupyRequest()
                {
                    AccommodationRequestIds = ids,
                    ProjectId = projectId,
                    RoomId = room,
                });
                return Ok();
            }
        }
        catch (Exception e) when (e is ArgumentException || e is JoinRpgEntityNotFoundException)
        {
        }
        catch
        {
            return StatusCode(500);
        }

        return BadRequest();
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

    [MasterAuthorize(Permission.CanSetPlayersAccommodations)]
    [HttpGet]
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

        await accommodationService.UnOccupyAll(projectId);

        return RedirectToAction("Index");
    }

    [MasterAuthorize(Permission.CanSetPlayersAccommodations)]
    [HttpPost("~/{projectId}/rooms/unoccupyroom")]
    public async Task<ActionResult> UnOccupyRoom(int projectId, int roomTypeId, int room, int reqId)
    {
        try
        {
            await accommodationService.UnOccupyRoom(new UnOccupyRequest()
            {
                ProjectId = projectId,
                AccommodationRequestId = reqId,
            });
            return Ok();
        }
        catch (Exception e) when (e is ArgumentException || e is JoinRpgEntityNotFoundException)
        {
        }
        catch
        {
            return StatusCode(500);
        }
        return BadRequest();
    }

    [MasterAuthorize(Permission.CanSetPlayersAccommodations)]
    [HttpGet]
    public async Task<ActionResult> UnOccupyRoom(int projectId, int roomTypeId)
    {
        try
        {
            await accommodationService.UnOccupyRoomType(projectId, roomTypeId);
            return RedirectToAction("RoomTypeDetails", "AccommodationType",
                new { ProjectId = projectId, RoomTypeId = roomTypeId });
        }
        catch (Exception e) when (e is ArgumentException || e is JoinRpgEntityNotFoundException)
        {
        }
        catch
        {
            return StatusCode(500);
        }
        return BadRequest();
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

        _ = await accommodationService.AddRooms(typeInfo.RoomCategoryId, name);
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
