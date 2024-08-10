namespace JoinRpg.Web.Models;

public class RecurrentPaymentFunctionsViewModel
{
    public int ClaimId { get; }
    public int ProjectId { get; }
    public int? RecurrentPaymentId { get; }

    public bool CanCancel { get; }

    public bool CanChange { get; }

    public bool CanSubscribe { get; }

    public bool CanForcePayment { get; }

    public RecurrentPaymentFunctionsViewModel(ClaimFeeViewModel claim, RecurrentPaymentViewModel? rp)
    {
        ClaimId = claim.ClaimId;
        ProjectId = claim.ProjectId;
        RecurrentPaymentId = rp?.RecurrentPaymentId;

        CanCancel = rp?.Status is RecurrentPaymentStatusViewModel.Active
            || (claim.HasFeeAdminAccess && rp?.Status is RecurrentPaymentStatusViewModel.Created or RecurrentPaymentStatusViewModel.Cancelling);
        CanChange = rp?.Status is RecurrentPaymentStatusViewModel.Active && claim.ShowRecurrentPaymentControls;
        CanSubscribe = rp is null && claim.ShowRecurrentPaymentControls;
        CanForcePayment = rp?.Status is RecurrentPaymentStatusViewModel.Active && claim.HasFeeAdminAccess;
    }
}
