using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.AdminTools.KogdaIgra;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

[Route("/webapi/kogdaigra/[action]")]
[AdminAuthorize]
[IgnoreAntiforgeryToken]
public class KogdaIgraSyncController(IKogdaIgraSyncClient client) : ControllerBase
{
    [HttpGet]
    public async Task<SyncStatusViewModel> GetSyncStatus() => await client.GetSyncStatus();

    [HttpGet]
    public async Task<KogdaIgraShortViewModel[]> GetKogdaIgraCandidates() => await client.GetKogdaIgraCandidates();

    [HttpGet]
    public async Task<KogdaIgraShortViewModel[]> GetKogdaIgraNotUpdated() => await client.GetKogdaIgraNotUpdated();

    [HttpPost]
    public async Task<ResyncOperationResultsViewModel> Resync() => await client.ResyncKograIgra();

    [HttpPost]
    public async Task UpdateBindings([FromBody] KogdaIgraBindViewModel command, [FromServices] IKogdaIgraBindClient bindClient)
        => await bindClient.UpdateProjectKogdaIgraBindings(command);
}
