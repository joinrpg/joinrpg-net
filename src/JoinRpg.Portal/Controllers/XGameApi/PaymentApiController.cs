using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.XGameApi;
[Route("x-game-api/{projectId}/finance"), XGameMasterAuthorize()]
public class PaymentApiController(IFinanceService financeService) : XGameApiController
{
    [HttpPost]
    public async Task<string> MarkPayment(int projectId, int claimId, int paymentTypeId, int amount, string? comment)
    {
        await financeService.FeeAcceptedOperation(
            new FeeAcceptedOperationRequest()
            {
                Contents = comment ?? "",
                ClaimId = claimId,
                FeeChange = 0,
                Money = amount,
                OperationDate = DateTime.Now,
                PaymentTypeId = new PrimitiveTypes.ProjectMetadata.Payments.PaymentTypeIdentification(projectId, paymentTypeId),
            });
        return "ok";
    }
}
