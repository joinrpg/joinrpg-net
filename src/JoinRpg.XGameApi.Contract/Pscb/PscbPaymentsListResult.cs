namespace JoinRpg.XGameApi.Contract.Pscb;

/// <summary>
/// Result of the PSCB payments list request
/// </summary>
/// <remarks>
/// The bank accepts at most one month per request, so a longer period is split into chunks which
/// are requested one by one and merged (ADR025). A chunk the bank has failed fails the whole
/// request: a silently partial list in a reconciliation tool is worse than an error.
/// </remarks>
public class PscbPaymentsListResult
{
    /// <summary>
    /// true when the bank has returned the list
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Bank's error code when <see cref="IsSuccess"/> is false, e.g. NOT_AUTHORIZED or ILLEGAL_ARGUMENTS
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Why the list could not be returned: the bank's own description, or the reason the request
    /// could not be performed at all
    /// </summary>
    public string? ErrorDescription { get; set; }

    /// <summary>
    /// Bank's request identifier, to quote when asking the bank's support. The bank sends it only
    /// on failure: a successful list response carries no envelope at all, so this stays null.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Payments of the whole requested period. Empty when the request failed
    /// </summary>
    public IReadOnlyCollection<PscbPaymentListRow> Payments { get; set; } = [];

    /// <summary>
    /// How many requests to the bank the period was split into
    /// </summary>
    public int RequestCount { get; set; }
}
