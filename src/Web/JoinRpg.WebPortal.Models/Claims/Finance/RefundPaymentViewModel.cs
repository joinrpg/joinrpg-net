namespace JoinRpg.Web.Models;

public class RefundPaymentViewModel
{
    public int ClaimId { get; set; }

    public int ProjectId { get; set; }

    public int OperationId { get; set; }

    public bool ShowTransfersNotification { get; set; }
}
