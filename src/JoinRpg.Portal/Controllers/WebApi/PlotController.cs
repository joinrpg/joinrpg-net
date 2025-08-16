using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.Plots;
using JoinRpg.Web.Plots;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;
[Route("/webapi/plots/[action]")]
[IgnoreAntiforgeryToken]
[RequireMaster]
public class PlotController(IPlotClient client) : ControllerBase
{
    [HttpGet]
    public async Task<PlotFolderDto[]> GetPlotFoldersList(int projectId)
     => await client.GetPlotFoldersList(new ProjectIdentification(projectId));

    [HttpPost]
    [RequireMaster(Permission.CanManagePlots)]
    public async Task<ActionResult> UnPublishVersion([FromQuery] ProjectIdentification projectId, [FromQuery] PlotVersionIdentification plotVersion)
    {
        if (projectId != plotVersion.ProjectId)
        {
            return Unauthorized();
        }
        await client.UnPublishVersion(plotVersion);
        return Ok();
    }

    [HttpPost]
    [RequireMaster(Permission.CanManagePlots)]
    public async Task<ActionResult> DeleteElement([FromQuery] ProjectIdentification projectId, [FromQuery] PlotElementIdentification elementId)
    {
        if (projectId != elementId.ProjectId)
        {
            return Unauthorized();
        }
        await client.DeleteElement(elementId);
        return Ok();
    }

    [HttpPost]
    [RequireMaster(Permission.CanManagePlots)]
    public async Task<ActionResult> UnDeleteElement([FromQuery] ProjectIdentification projectId, [FromQuery] PlotElementIdentification elementId)
    {
        if (projectId != elementId.ProjectId)
        {
            return Unauthorized();
        }
        await client.UnDeleteElement(elementId);
        return Ok();
    }

    [HttpPost]
    [RequireMaster(Permission.CanManagePlots)]
    public async Task<ActionResult> PublishVersion([FromQuery] ProjectIdentification projectId, [FromBody] PublishPlotElementViewModel viewModel)
    {
        if (projectId != viewModel.ProjectId)
        {
            return Unauthorized();
        }
        await client.PublishVersion(viewModel);
        return Ok();
    }
}
