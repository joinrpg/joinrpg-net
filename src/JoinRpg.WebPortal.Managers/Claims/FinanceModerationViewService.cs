using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Claims.Finance;

namespace JoinRpg.WebPortal.Managers.Claims;

/// <remarks>
/// Операция заведена в комментарии с тем же id: решение по ней — ответ на этот комментарий
/// (<see cref="IClaimService.ModerateFinanceOperation"/>).
/// </remarks>
internal class FinanceModerationViewService(IClaimService claimService) : IFinanceModerationClient
{
    public Task Approve(FinanceOperationIdentification operationId, FinanceModerationDecisionRequest request)
        => Moderate(operationId, request, FinanceOperationAction.Approve);

    public Task Decline(FinanceOperationIdentification operationId, FinanceModerationDecisionRequest request)
        => Moderate(operationId, request, FinanceOperationAction.Decline);

    private Task Moderate(FinanceOperationIdentification operationId, FinanceModerationDecisionRequest request, FinanceOperationAction action)
        => claimService.ModerateFinanceOperation(
            operationId.ClaimId,
            operationId.FinanceOperationId,
            request.CommentText ?? "",
            action);
}
