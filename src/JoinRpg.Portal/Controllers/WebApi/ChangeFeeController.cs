using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Claims.Finance;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

/// <summary>
/// Ручное назначение размера базового взноса заявки. Право CanManageMoney проверяет сервис.
/// </summary>
[Route("/webapi/{projectId}/claims/{claimId}/change-fee")]
[RequireMaster]
public class ChangeFeeController(IFinanceService financeService) : ControllerBase
{
    /// <summary>Без <c>[ApiController]</c> пустое тело приходит null-ом — отказ, а не 500-я.</summary>
    private const string EmptyBody = "Пустой запрос. Обновите страницу и попробуйте ещё раз.";

    [HttpPost]
    public async Task<ActionResult> Index(
        ClaimIdentification claimId,
        [FromBody] ChangeFeeRequest? request)
    {
        if (request is null)
        {
            return BadRequest(EmptyBody);
        }

        await financeService.ChangeFee(claimId, request.FeeValue);
        return Ok();
    }
}
