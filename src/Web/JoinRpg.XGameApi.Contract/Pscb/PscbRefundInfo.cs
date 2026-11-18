namespace JoinRpg.XGameApi.Contract.Pscb;

/// <summary>
/// Refund of a payment
/// </summary>
public class PscbRefundInfo
{
    /// <summary>
    /// Refund identifier in the bank's system
    /// </summary>
    public string? RefundId { get; set; }

    /// <summary>
    /// Refund status as the bank reports it: PROC, END, ERR or DELAY
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Refunded sum, RUR
    /// </summary>
    public decimal Amount { get; set; }
}
