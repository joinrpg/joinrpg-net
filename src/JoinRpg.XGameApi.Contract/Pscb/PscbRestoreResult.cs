namespace JoinRpg.XGameApi.Contract.Pscb;

/// <summary>
/// Outcome of one restoration run over a downloaded period (ADR026).
/// </summary>
/// <remarks>
/// <para>
/// The run re-downloads the period and restores every row the reconciliation marked as
/// <see cref="PscbPaymentMatchState.Lost"/> or <see cref="PscbPaymentMatchState.RefundMissing"/>
/// with no <see cref="PscbPaymentListRow.RestoreBlockReason"/> — one row at a time, continuing
/// after a failure. Refusals of the whole run (the bank did not return the list, the period is
/// invalid, too many records for one run) are reported as an error response, not as this object.
/// </para>
/// <para>
/// A row that was already restored by an earlier run is reported as
/// <see cref="PscbRestoreRowOutcome.AlreadySettled"/> and is not counted here: the counters are
/// what this run has written.
/// </para>
/// </remarks>
public class PscbRestoreResult
{
    /// <summary>
    /// How many lost payments this run has inserted.
    /// </summary>
    public int RestoredPaymentsCount { get; set; }

    /// <summary>
    /// How many refunds this run has inserted — both the refunds of restored payments and the
    /// refunds restored on their own.
    /// </summary>
    public int RestoredRefundsCount { get; set; }

    /// <summary>
    /// Outcome of every candidate row, in the order of the bank's list.
    /// </summary>
    public IReadOnlyCollection<PscbRestoreRowResult> Rows { get; set; } = [];
}
