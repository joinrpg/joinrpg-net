using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Web.Plots;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;
[Route("/webapi/plots/[action]")]
[RequireMaster]
public class PlotController(IPlotClient client) : ControllerBase
{
    [HttpGet]
    public async Task<PlotFolderDto[]> GetPlotFoldersList(int projectId)
     => await client.GetPlotFoldersList(new ProjectIdentification(projectId));
}
