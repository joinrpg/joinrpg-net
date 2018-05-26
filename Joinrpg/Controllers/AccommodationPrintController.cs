using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Accommodation;
using JoinRpg.Web.Filter;

namespace JoinRpg.Web.Controllers
{
    [MasterAuthorize()]
    public class AccommodationPrintController : Common.ControllerGameBase
    {
        private IAccommodationRepository AccommodationRepository { get; }
        private IUriService UriService { get; }

        public AccommodationPrintController(ApplicationUserManager userManager,
            [NotNull]
            IProjectRepository projectRepository,
            IProjectService projectService,
            IExportDataService exportDataService,
            IUriService uriService,
            IAccommodationRepository accommodationRepository) : base(userManager,
            projectRepository,
            projectService,
            exportDataService)
        {
            UriService = uriService;
            AccommodationRepository = accommodationRepository;
        }

        [HttpGet]
        public async Task<ActionResult> MainReport(int projectId, string export)
        {

            var project = await ProjectRepository.GetProjectWithDetailsAsync(projectId)
                .ConfigureAwait(false);

            if (project == null) return HttpNotFound();
            if (!project.Details.EnableAccommodation) return RedirectToAction("Edit", "Game");

            var accommodations =
                (await AccommodationRepository.GetClaimAccommodationReport(projectId)).Select(row => new AccomodationReportListItemViewModel
                {
                    ProjectId = project.ProjectId,
                    ClaimId = row.ClaimId,
                    AccomodationType = row.AccomodationType,
                    RoomName = row.RoomName,
                    DisplayName = row.User.GetDisplayName(),
                    FullName = row.User.FullName,
                    Phone = row.User.Extra?.PhoneNumber,
                });
            
            var exportType = GetExportTypeByName(export);

            if (exportType == null)
            {

                return HttpNotFound();
            }
            else
            {

                return
                    await
                        ExportWithCustomFronend(accommodations, "Отчет по расселению", exportType.Value,
                            new AccomodationReportExporter(UriService), project.ProjectName);
            }
        }

    }
}
