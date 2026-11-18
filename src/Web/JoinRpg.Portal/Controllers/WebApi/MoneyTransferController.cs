using JoinRpg.Domain;
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
        => await WrongStatusToBadRequest(() => client.Approve(new MoneyTransferIdentification(projectId, transferId)));

    [HttpPost]
    public async Task<ActionResult> Decline(ProjectIdentification projectId, [FromQuery] int transferId)
        => await WrongStatusToBadRequest(() => client.Decline(new MoneyTransferIdentification(projectId, transferId)));

    /// <summary>
    /// Перевод мог успеть обработать другой мастер — это штатный исход, а не 500-я.
    /// </summary>
    private async Task<ActionResult> WrongStatusToBadRequest(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (EntityWrongStatusException)
        {
            return BadRequest("Этот перевод уже подтверждён или отклонён. Обновите страницу.");
        }

        return Ok();
    }
}
