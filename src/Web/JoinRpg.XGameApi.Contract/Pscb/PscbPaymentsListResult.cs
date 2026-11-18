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
    /// Purposes that could not be linked to a project, one entry per distinct name. Empty when
    /// every purpose resolved, or when the links could not be built at all
    /// </summary>
    public IReadOnlyCollection<PscbUnresolvedProject> UnresolvedProjects { get; set; } = [];

    /// <summary>
    /// Payers that were not found in JoinRpg, one entry per distinct payer. Their payments carry
    /// no link to a profile. Ordered by the payer's id as a number — an id that is not a number
    /// comes last
    /// </summary>
    public IReadOnlyCollection<PscbUnresolvedUser> UnresolvedUsers { get; set; } = [];

    /// <summary>
    /// How many requests to the bank the period was split into
    /// </summary>
    public int RequestCount { get; set; }

    /// <summary>
    /// Set when matching the bank's list against the JoinRpg database failed, so
    /// <see cref="PscbPaymentListRow.MatchState"/> is unknown for every row; the list itself is
    /// still the bank's data. Restoration must refuse while this is set.
    /// </summary>
    public bool ReconciliationFailed { get; set; }
}
