using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Impl;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Accomodation;
using JoinRpg.Web.Filter;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
    [MasterAuthorize(Permission.CanChangeProjectProperties)]
    public class AccomodationTypeController : Common.ControllerGameBase
    {
        private readonly IAccomodationService _accomodationService;
        public AccomodationTypeController(ApplicationUserManager userManager, [NotNull] IProjectRepository projectRepository,
                                            IProjectService projectService, IExportDataService exportDataService,
                                               IAccomodationService accomodationService) : base(userManager, projectRepository, projectService, exportDataService)
        {
            _accomodationService = accomodationService;
        }

        [HttpGet]
        public async Task<ActionResult> Index(int projectId)
        {

            var project = await ProjectRepository.GetProjectWithDetailsAsync(projectId);
            var accomodations = (await _accomodationService.GetAccomodationForProject(projectId)).Select(x =>
                new AccomomodationTypeViewModel()
                {
                    Name = x.Name,
                    Cost = x.Cost,
                    ProjectId = x.ProjectId,
                    Id = x.Id,
                }
              ).ToList();
            if (project == null) return HttpNotFound();
            if (!project.Details.EnableAccomodation) return RedirectToAction("Edit", "Game");
            var res = new AccomodationListViewModel
            {
                ProjectId = projectId,
                ProjectName = project.ProjectName,
                AccomomodationTypes = accomodations
            };
            return View(res);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(AccomomodationTypeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            await _accomodationService.RegisterNewAccomodationTypeAsync(model.GetProjectAccomodationMock());
            return RedirectToAction("Index", routeValues: new { ProjectId = model.ProjectId });
        }

        [HttpGet]
        public async Task<ActionResult> Edit(int projectId, int accomodationId)
        {
            var model = await _accomodationService.GetAccomodationByIdAsync(accomodationId);
            if (model == null || model.ProjectId != projectId)
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            return View("Edit", new AccomomodationTypeViewModel(model));
        }


        [HttpDelete]
        public async Task<ActionResult> Delete(int accomodationId)
        {
            await _accomodationService.RemoveAccomodationType(accomodationId);
            return RedirectToAction("Index", new { ProjectId = 1 });
        }
    }
}
