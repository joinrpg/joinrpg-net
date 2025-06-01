using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Portal.Helpers;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Models.Accommodation;
using JoinRpg.Web.Models.Exporters;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[MasterAuthorize()]
[Route("{projectId}/rooms/report/")]
public class AccommodationPrintController(
    IProjectRepository projectRepository,
    IProjectService projectService,
    IExportDataService exportDataService,
    IUriService uriService,
    IAccommodationRepository accommodationRepository,
    IUserRepository userRepository,
    IProjectMetadataRepository projectMetadataRepository
    ) : Common.ControllerGameBase(projectRepository,
        projectService,
        userRepository)
{
    [HttpGet]
    public async Task<ActionResult> MainReport(ProjectIdentification projectId, string export)
    {

        var project = await projectMetadataRepository.GetProjectMetadata(projectId);

        if (project == null)
        {
            return NotFound();
        }

        if (!project.AccomodationEnabled)
        {
            return RedirectToAction("Edit", "Game");
        }

        var accommodations =
            (await accommodationRepository.GetClaimAccommodationReport(projectId)).Select(row => new AccomodationReportListItemViewModel
            {
                ProjectId = project.ProjectId,
                ClaimId = row.ClaimId,
                AccomodationType = row.AccomodationType,
                RoomName = row.RoomName,
                DisplayName = row.User.GetDisplayName(),
                FullName = row.User.FullName,
                Phone = row.User.Extra?.PhoneNumber,
            });

        var exportType = ExportTypeNameParserHelper.ToExportType(export);

        if (exportType == null)
        {

            return NotFound();
        }
        else
        {
            var generator = exportDataService.GetGenerator(exportType.Value, accommodations, new AccomodationReportExporter(uriService));

            return GeneratorResultHelper.Result(project.ProjectName + ": " + "Отчет по расселению", generator);
        }
    }
}
