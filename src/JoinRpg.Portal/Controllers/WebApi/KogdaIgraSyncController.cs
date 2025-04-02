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

    [HttpPost]
    public async Task<ResyncOperationResultsViewModel> Resync() => await client.ResyncKograIgra();
}
