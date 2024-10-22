using JoinRpg.DataModel.Finances;
using JoinRpg.Helpers;
using JoinRpg.Web.Claims.Finance;

namespace JoinRpg.Web.Models;

public class RecurrentPaymentViewModel
{
    public int ClaimId { get; set; }

    public int ProjectId { get; set; }

    public int RecurrentPaymentId { get; set; }

    public int Amount { get; set; }

    public int TotalPaid { get; set; }

    public RecurrentPaymentStatusViewModel Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }

    public string StatusText { get; set; }

    public RecurrentPaymentViewModel() { }

    public RecurrentPaymentViewModel(ClaimFeeViewModel claim, RecurrentPayment source)
    {
        ClaimId = claim.ClaimId;
        ProjectId = claim.ProjectId;
        RecurrentPaymentId = source.RecurrentPaymentId;
        Amount = source.PaymentAmount;
        TotalPaid = claim.VisibleFinanceOperations
            .Where(fo => fo.RecurrentPaymentId == source.RecurrentPaymentId && fo.OperationState == FinanceOperationStateViewModel.Approved)
            .Sum(static fo => fo.Money);
        Status = (RecurrentPaymentStatusViewModel)source.Status;
        CreatedAt = source.CreateDate;
        ClosedAt = source.CloseDate;
        StatusText = Status.GetDisplayName().ToLowerInvariant();
        switch (Status)
        {
            case RecurrentPaymentStatusViewModel.Active:
                StatusText = $"{StatusText} с {CreatedAt:d}";
                break;
            case RecurrentPaymentStatusViewModel.Cancelled:
            case RecurrentPaymentStatusViewModel.Superseded:
                StatusText = $"{StatusText} с {ClosedAt:d}";
                break;
        }
    }
}
