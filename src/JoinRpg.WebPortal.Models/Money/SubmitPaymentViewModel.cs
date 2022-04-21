using System.ComponentModel;

namespace JoinRpg.Web.Models;

public class SubmitPaymentViewModel : PaymentViewModelBase
{
    [ReadOnly(true)]
    public IEnumerable<PaymentTypeViewModel> PaymentTypes { get; set; }

    public SubmitPaymentViewModel() { }

    public SubmitPaymentViewModel(ClaimViewModel claim) : base(claim)
    {
        ActionName = "Отметить взнос";
        PaymentTypes = claim.PaymentTypes.GetUserSelectablePaymentTypes();
        CommentText = "Сдан взнос";
    }
}
