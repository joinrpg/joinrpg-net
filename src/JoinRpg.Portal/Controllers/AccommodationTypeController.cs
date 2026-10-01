using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.ProjectMetadata;
using JoinRpg.Web.Models.Accommodation;
using JoinRpg.WebPortal.Managers.Accommodation;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

/// <summary>
/// Страницы раздела «Поселение»: отдают вью или редирект. Операции, которые вызывает скрипт
/// <c>rooms.js</c> и которые отвечают кодами ответа, живут в
/// <see cref="WebApi.AccommodationRoomsController"/>.
/// </summary>
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
}
