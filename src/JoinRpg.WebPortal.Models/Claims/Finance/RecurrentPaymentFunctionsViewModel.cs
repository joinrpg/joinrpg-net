namespace JoinRpg.Web.Models;

public class RecurrentPaymentFunctionsViewModel
{
    public bool CanCancel { get; set; }

    public bool CanChange { get; set; }

    public bool CanSubscribe { get; set; }

    public RecurrentPaymentFunctionsViewModel(ClaimFeeViewModel claim, RecurrentPaymentViewModel? rp)
    {
        CanCancel = rp?.Status is RecurrentPaymentStatusViewModel.Active or RecurrentPaymentStatusViewModel.Cancelling;
        CanChange = rp?.Status is RecurrentPaymentStatusViewModel.Active && claim.ShowRecurrentPaymentControls;
        CanSubscribe = rp is null && claim.ShowRecurrentPaymentControls;
    }
}
