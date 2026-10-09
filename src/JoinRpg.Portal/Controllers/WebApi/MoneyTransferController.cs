using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.Claims.Finance;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

/// <summary>
/// Подтверждение и отклонение переводов денег между мастерами. Кто вправе решать — проверяет сервис.
/// </summary>
[Route("/webapi/{projectId}/money-transfer/[action]")]
[RequireMaster]
public class MoneyTransferController(IMoneyTransferClient client) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> Approve(ProjectIdentification projectId, [FromQuery] int transferId)
    {
        await client.Approve(new MoneyTransferIdentification(projectId, transferId));
        return Ok();
    }

    [HttpPost]
    public async Task<ActionResult> Decline(ProjectIdentification projectId, [FromQuery] int transferId)
    {
        await client.Decline(new MoneyTransferIdentification(projectId, transferId));
        return Ok();
    }
}
