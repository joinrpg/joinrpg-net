using System.Linq;
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
    [MasterAuthorize(Permission.CanChangeProjectProperties)]
    public class AccommodationTypeController : Common.ControllerGameBase
    {
        private readonly IAccommodationService _accommodationService;
        public AccommodationTypeController(ApplicationUserManager userManager, [NotNull] IProjectRepository projectRepository,
                                            IProjectService projectService, IExportDataService exportDataService,
                                               IAccommodationService accommodationService) : base(userManager, projectRepository, projectService, exportDataService)
        {
            _accommodationService = accommodationService;
        }

        [HttpGet]
        public async Task<ActionResult> Index(int projectId)
        {

            var project = await ProjectRepository.GetProjectWithDetailsAsync(projectId).ConfigureAwait(false);
            var accommodations = (await _accommodationService.GetAccommodationForProject(projectId).ConfigureAwait(false)).Select(x =>
                new AccommodationTypeViewModel()
                {
                    Name = x.Name,
                    Cost = x.Cost,
                    ProjectId = x.ProjectId,
                    Id = x.Id,
                    Capacity = x.ProjectAccommodations.Sum(c => c.Capacity)
                }
              ).ToList();
            if (project == null) return HttpNotFound();
            if (!project.Details.EnableAccomodation) return RedirectToAction("Edit", "Game");
            var res = new AccommodationListViewModel
            {
                ProjectId = projectId,
                ProjectName = project.ProjectName,
                AccommodationTypes = accommodations
            };
            return View(res);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(AccommodationTypeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            await _accommodationService.RegisterNewAccommodationTypeAsync(model.GetProjectAccommodationTypeMock()).ConfigureAwait(false);
            return RedirectToAction("Index", routeValues: new {model.ProjectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ProjectAccommodationEdit(ProjectAccommodationVewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            await _accommodationService.RegisterNewProjectAccommodationAsync(model.GetProjectAccommodationMock()).ConfigureAwait(false);
            return RedirectToAction("Edit", routeValues: new {model.ProjectId, AccomodationId = model.AccommodationTypeId });
        }



        [HttpGet]
        public async Task<ActionResult> Edit(int projectId, int accommodationId)
        {
            var model = await _accommodationService.GetAccommodationByIdAsync(accommodationId).ConfigureAwait(false);
            if (model == null || model.ProjectId != projectId)
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            return View("Edit", new AccommodationTypeViewModel(model));
        }


        [HttpGet]
        public async Task<ActionResult> ProjectAccommodationEdit(int projectId, int accommodationTypeId, int projectAccommodationId)
        {
            var model = await _accommodationService.GetProjectAccommodationByIdAsync(projectAccommodationId).ConfigureAwait(false);
            if (model == null || model.ProjectId != projectId || model.AccommodationTypeId != accommodationTypeId)
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }
            ViewBag.AccomodationName = $"«{model.Project.ProjectName}\\{model.ProjectAccommodationType.Name}»";
            return View("ProjectAccommodationEdit", new ProjectAccommodationVewModel(model));
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int accomodationTypeId, int projectId)
        {
            await _accommodationService.RemoveAccommodationType(accomodationTypeId).ConfigureAwait(false);
            return RedirectToAction("Index", new { ProjectId = projectId });
        }


        [HttpDelete]
        [ValidateAntiForgeryToken]
        [Route("accommodationType/{ProjectId:int}/ProjectAccommodationDelete")]
        public async Task<ActionResult> ProjectAccommodationDelete(int projectId, int accommodationTypeId, int projectAccommodationId)
        {
            await _accommodationService.RemoveProjectAccommodation(projectAccommodationId).ConfigureAwait(false);
            return RedirectToAction("Edit", routeValues: new { ProjectId = projectId, AccomodationId = accommodationTypeId });
        }
    }
}
