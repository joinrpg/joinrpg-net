using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Claims.Finance;

namespace JoinRpg.WebPortal.Managers.Claims;

internal class ChangeFeeViewService(IFinanceService financeService) : IChangeFeeClient
{
    public Task ChangeFee(ClaimIdentification claimId, ChangeFeeRequest request)
        => financeService.ChangeFee(claimId, request.FeeValue);
}
