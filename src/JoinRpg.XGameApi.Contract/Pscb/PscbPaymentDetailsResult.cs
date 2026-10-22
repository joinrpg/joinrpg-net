namespace JoinRpg.XGameApi.Contract.Pscb;

/// <summary>
/// Result of the PSCB payment details request
/// </summary>
public class PscbPaymentDetailsResult
{
    /// <summary>
    /// true when the bank has returned the payment
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Bank's error code when <see cref="IsSuccess"/> is false, e.g. UNKNOWN_PAYMENT
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Why the payment could not be returned: the bank's own description, or the reason the request
    /// could not be performed at all
    /// </summary>
    public string? ErrorDescription { get; set; }

    /// <summary>
    /// Bank's request identifier. Quote it when asking the bank's support
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Bank's description of the current state of the payment
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The payment itself. Null when the bank has not returned one
    /// </summary>
    public PscbPaymentInfo? Payment { get; set; }

    /// <summary>
    /// Errors the bank has reported for the last action performed with the payment
    /// </summary>
    public IReadOnlyCollection<PscbBankErrorInfo> LastErrors { get; set; } = [];
}
