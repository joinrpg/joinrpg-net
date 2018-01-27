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
using System.Text;
using Newtonsoft.Json;

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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ProjectAccommodationEdit(RoomViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            await _accommodationService.RegisterNewProjectAccommodationAsync(model.GetProjectAccommodationMock()).ConfigureAwait(false);
            return RedirectToAction("Edit", routeValues: new { model.ProjectId, AccommodationId = model.RoomTypeId });
        }

        [NonAction]
        public async Task<ActionResult> ProjectAccommodationEdit(int projectId, int accommodationTypeId, int projectAccommodationId)
        {
            var model = await _accommodationService.GetProjectAccommodationByIdAsync(projectAccommodationId).ConfigureAwait(false);
            if (model == null || model.ProjectId != projectId || model.AccommodationTypeId != accommodationTypeId)
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }
            ViewBag.AccomodationName = $"«{model.Project.ProjectName}\\{model.ProjectAccommodationType.Name}»";
            return View("ProjectAccommodationEdit", new RoomViewModel(model));
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
                    return new ContentResult()
                    {
                        ContentType = "application/json",
                        ContentEncoding = Encoding.UTF8,
                        Content = JsonConvert.SerializeObject(
                            _accommodationService.AddRooms(projectId, roomTypeId, name))
                    };
                }
            }
            catch
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }
    }
}
