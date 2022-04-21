using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Accommodation;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[MasterAuthorize()]
[Route("{projectId}/rooms/[action]")]
public class AccommodationTypeController : Common.ControllerGameBase
{
    private IAccommodationRepository AccommodationRepository { get; }
    private readonly IAccommodationService _accommodationService;

    public AccommodationTypeController(
        IProjectRepository projectRepository,
        IProjectService projectService,
        IAccommodationService accommodationService,
        IAccommodationRepository accommodationRepository,
        IUserRepository userRepository) : base(projectRepository,
            projectService,
            userRepository)
    {
        AccommodationRepository = accommodationRepository;
        _accommodationService = accommodationService;
    }

    /// <summary>
    /// Shows list of registered room types
    /// </summary>
    [HttpGet("~/{projectId}/rooms/")]
    public async Task<ActionResult> Index(int projectId)
    {
        var project = await ProjectRepository.GetProjectWithDetailsAsync(projectId);
        if (project == null)
        {
            return NotFound($"Project {projectId} not found");
        }

        if (!project.Details.EnableAccommodation)
        {
            return RedirectToAction("Edit", "Game");
        }

        return View(new AccommodationListViewModel(project,
            await AccommodationRepository.GetRoomTypesForProject(projectId),
            CurrentUserId));
    }

    /// <summary>
    /// Shows "Add room type" form
    /// </summary>
    [MasterAuthorize(Permission.CanManageAccommodation)]
    [HttpGet]
    public async Task<ActionResult> AddRoomType(int projectId)
    {
        return View(new RoomTypeViewModel(
            await ProjectRepository.GetProjectAsync(projectId), CurrentUserId));
    }

    /// <summary>
    /// Shows "Edit room type" form
    /// </summary>
    [HttpGet("~/{projectId}/rooms/{roomTypeId}/edit")]
    public async Task<ActionResult> EditRoomType(int projectId, int roomTypeId)
    {
        var entity = await _accommodationService.GetRoomTypeAsync(roomTypeId).ConfigureAwait(false);
        if (entity == null || entity.ProjectId != projectId)
        {
            return Forbid();
        }

        return View(new RoomTypeViewModel(entity, CurrentUserId));
    }

    /// <summary>
    /// Shows "Edit room type" form
    /// </summary>
    [MasterAuthorize(Permission.CanSetPlayersAccommodations)]
    [HttpGet("~/{projectId}/rooms/{roomTypeId}/details")]
    public async Task<ActionResult> EditRoomTypeRooms(int projectId, int roomTypeId)
    {
        var entity = await _accommodationService.GetRoomTypeAsync(roomTypeId);
        if (entity == null || entity.ProjectId != projectId)
        {
            return Forbid();
        }

        return View(new RoomTypeViewModel(entity, CurrentUserId));
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
        _ = await _accommodationService.SaveRoomTypeAsync(model.ToEntity()).ConfigureAwait(false);
        return RedirectToAction("Index", new { projectId = model.ProjectId });
    }

    /// <summary>
    /// Removes room type
    /// </summary>
    [MasterAuthorize(Permission.CanManageAccommodation)]
    [HttpGet]
    public async Task<ActionResult> DeleteRoomType(int roomTypeId, int projectId)
    {
        await _accommodationService.RemoveRoomType(roomTypeId).ConfigureAwait(false);
        return RedirectToAction("Index", new { ProjectId = projectId });
    }

    [MasterAuthorize(Permission.CanSetPlayersAccommodations)]
    [HttpPost("~/{projectId}/rooms/occupyroom")]
    public async Task<ActionResult> OccupyRoom(int projectId, int roomTypeId, int room, string reqId)
    {
        try
        {
            IReadOnlyCollection<int> ids = reqId.Split(',')
                .Select(s => int.TryParse(s, out var val) ? val : 0)
                .Where(val => val > 0)
                .ToList();
            if (ids.Count > 0)
            {
                await _accommodationService.OccupyRoom(new OccupyRequest()
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
    public async Task<ActionResult> OccupyAll(int projectId)
    {
        var project = await ProjectRepository.GetProjectWithDetailsAsync(projectId);
        if (project == null)
        {
            return NotFound($"Project {projectId} not found");
        }

        if (!project.Details.EnableAccommodation)
        {
            return RedirectToAction("Edit", "Game");
        }

        //TODO: Implement mass occupation

        return RedirectToAction("Index");
    }

    [MasterAuthorize(Permission.CanSetPlayersAccommodations)]
    [HttpGet]
    public async Task<ActionResult> UnOccupyAll(int projectId)
    {
        var project = await ProjectRepository.GetProjectWithDetailsAsync(projectId);
        if (project == null)
        {
            return NotFound($"Project {projectId} not found");
        }

        if (!project.Details.EnableAccommodation)
        {
            return RedirectToAction("Edit", "Game");
        }

        await _accommodationService.UnOccupyAll(projectId);

        return RedirectToAction("Index");
    }

    [MasterAuthorize(Permission.CanSetPlayersAccommodations)]
    [HttpPost("~/{projectId}/rooms/unoccupyroom")]
    public async Task<ActionResult> UnOccupyRoom(int projectId, int roomTypeId, int room, int reqId)
    {
        try
        {
            await _accommodationService.UnOccupyRoom(new UnOccupyRequest()
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
            await _accommodationService.UnOccupyRoomType(projectId, roomTypeId);
            return RedirectToAction("EditRoomTypeRooms", "AccommodationType",
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
    /// Removes room
    /// </summary>
    [MasterAuthorize(Permission.CanManageAccommodation)]
    [HttpDelete]
    public async Task<ActionResult> DeleteRoom(int projectId, int roomTypeId, int roomId)
    {
        try
        {
            await _accommodationService.DeleteRoom(roomId, projectId, roomTypeId).ConfigureAwait(false);
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

    [MasterAuthorize(Permission.CanManageAccommodation)]
    [HttpPost("~/{projectId}/rooms/addroom")]
    public async Task<ActionResult> AddRoom(int projectId, int roomTypeId, string name)
    {
        try
        {
            //TODO: Implement room names checking
            //TODO: Implement new rooms HTML returning
            _ = await _accommodationService.AddRooms(projectId, roomTypeId, name);
            return StatusCode(201);
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
    /// Applies new name to a room or adds a new room(s)
    /// </summary>
    [MasterAuthorize(Permission.CanManageAccommodation)]
    [HttpPost("~/{projectId}/rooms/editroom")]
    public async Task<ActionResult> EditRoom(int projectId, int roomTypeId, string room, string name)
    {
        try
        {
            if (int.TryParse(room, out var roomId))
            {
                await _accommodationService.EditRoom(roomId, name, projectId, roomTypeId);
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
}
