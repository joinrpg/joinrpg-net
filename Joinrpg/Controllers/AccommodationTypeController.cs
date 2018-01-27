using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
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

        [HttpGet]
        public async Task<ActionResult> Index(int projectId)
        {

            var project = await ProjectRepository.GetProjectWithDetailsAsync(projectId).ConfigureAwait(false);
            var accommodations = (await _accommodationService.GetAccommodationForProject(projectId).ConfigureAwait(false)).Select(x =>
                new RoomTypeViewModel()
                {
                    Name = x.Name,
                    Cost = x.Cost,
                    ProjectId = x.ProjectId,
                    Id = x.Id,
                    Capacity = x.Capacity,
                    Description = x.Description,
                    IsAutoFilledAccommodation = x.IsAutoFilledAccommodation,
                    IsInfinite = x.IsInfinite,
                    IsPlayerSelectable = x.IsPlayerSelectable,
                    Accommodations = x.ProjectAccommodations.Select(projectAccomodaion => new RoomViewModel(projectAccomodaion)).ToList()
                }
              ).ToList();
            if (project == null) return HttpNotFound();
            if (!project.Details.EnableAccommodation) return RedirectToAction("Edit", "Game");
            var res = new AccommodationListViewModel
            {
                ProjectId = projectId,
                ProjectName = project.ProjectName,
                RoomsTypes = accommodations,
                CanManageRooms = project.HasMasterAccess(CurrentUserId, acl => acl.CanManageAccommodation),
                CanAssignRooms = project.HasMasterAccess(CurrentUserId, acl => acl.CanSetPlayersAccommodations),
            };
            return View(res);
        }

        [MasterAuthorize(Permission.CanManageAccommodation)]
        [HttpPost]
        [ValidateAntiForgeryToken]        
        public async Task<ActionResult> Edit(RoomTypeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("TypeDetails", model);
            }
            await _accommodationService.RegisterNewAccommodationTypeAsync(model.GetProjectAccommodationTypeMock()).ConfigureAwait(false);
            return RedirectToAction("Index", routeValues: new { model.ProjectId });
        }

        [MasterAuthorize()]
        [HttpGet]
        public async Task<ActionResult> RoomTypeDetails(int projectId, int roomTypeId)
        {
            var model = await _accommodationService.GetAccommodationByIdAsync(roomTypeId).ConfigureAwait(false);
            if (model == null || model.ProjectId != projectId)
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            return View("TypeDetails", new RoomTypeViewModel(model, CurrentUserId));
        }

        [MasterAuthorize(Permission.CanManageAccommodation)]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ActionResult> DeleteRoomType(int accomodationTypeId, int projectId)
        {
            await _accommodationService.RemoveAccommodationType(accomodationTypeId).ConfigureAwait(false);
            return RedirectToAction("Index", new { ProjectId = projectId });
        }

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
