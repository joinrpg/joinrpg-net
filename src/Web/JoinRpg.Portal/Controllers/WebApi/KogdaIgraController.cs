using JoinRpg.Web.ProjectCommon.KogdaIgra;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

/// <summary>
/// В отличие от <see cref="KogdaIgraSyncController"/> (только для сайт-админов),
/// эти методы доступны любому авторизованному пользователю — они нужны, например,
/// форме создания проекта.
/// </summary>
[Route("/webapi/kogdaigra/[action]")]
[Authorize]
[IgnoreAntiforgeryToken]
public class KogdaIgraController(IKogdaIgraSyncClient client) : ControllerBase
{
    [HttpGet]
    public async Task<KogdaIgraShortViewModel[]> GetKogdaIgraCandidates() => await client.GetKogdaIgraCandidates();

    [HttpGet]
    public async Task<KogdaIgraShortViewModel[]> GetFutureKogdaIgraCandidates() => await client.GetFutureKogdaIgraCandidates();
}
