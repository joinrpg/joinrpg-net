using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Accommodation;
using JoinRpg.Web.Filter;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
    [MasterAuthorize()]
    public class AccommodationTypeController : Common.ControllerGameBase
    {
        private readonly IAccommodationService _accommodationService;

        public AccommodationTypeController(ApplicationUserManager userManager,
            [NotNull]
            IProjectRepository projectRepository,
            IProjectService projectService,
            IExportDataService exportDataService,
            IAccommodationService accommodationService) : base(userManager,
            projectRepository,
            projectService,
            exportDataService)
        {
            _accommodationService = accommodationService;
        }

        /// <summary>
        /// Shows list of registered room types
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> Index(int projectId)
        {
            var project = await ProjectRepository.GetProjectWithDetailsAsync(projectId);
            if (project == null)
                return HttpNotFound($"Project {projectId} not found");
            if (!project.Details.EnableAccommodation)
                return RedirectToAction("Edit", "Game");

            return View(new AccommodationListViewModel(project,
                await _accommodationService.GetRoomTypes(projectId),
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
        [HttpGet]
        public async Task<ActionResult> EditRoomType(int projectId, int roomTypeId)
        {
            var entity = await _accommodationService.GetAccommodationByIdAsync(roomTypeId).ConfigureAwait(false);
            if (entity == null || entity.ProjectId != projectId)
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
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
                    return View("AddRoomType", model);
                return View("EditRoomType", model);
            }
            await _accommodationService.SaveRoomTypeAsync(model.ToEntity()).ConfigureAwait(false);
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
            }
            catch
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        /// <summary>
        /// Applies new name to a room or adds a new room(s)
        /// </summary>
        [MasterAuthorize(Permission.CanManageAccommodation)]
        [HttpGet]
        public async Task<ActionResult> EditRoom(int projectId, int roomTypeId, string room, string name)
        {
            try
            {
                if (int.TryParse(room, out int roomId))
                {
                    await _accommodationService.EditRoom(roomId, name, projectId, roomTypeId);
                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }
                else
                {
                    await _accommodationService.AddRooms(projectId, roomTypeId, name);
                    return new HttpStatusCodeResult(HttpStatusCode.Created);
                    //TODO: Fix problem with rooms IDs

                    //return View("_AddedRoomsList",
                    //    (await _accommodationService.AddRooms(projectId, roomTypeId, name))
                    //        .Select(pa => new RoomViewModel(pa)));
                }
            }
            catch
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }
    }
}
