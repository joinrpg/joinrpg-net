using JoinRpg.PrimitiveTypes;
using JoinRpg.Web.ProjectCommon;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

[Route("/webapi/master/[action]")]
public class ProjectMasterController : ControllerBase
{
    private readonly IMasterClient client;

    public ProjectMasterController(IMasterClient client)
    {
        this.client = client;
    }

    [HttpGet]
    public async Task<List<MasterViewModel>> GetList(int projectId)
        => await client.GetMasters(new ProjectIdentification(projectId));
}
