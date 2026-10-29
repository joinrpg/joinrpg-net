namespace JoinRpg.XGameApi.Contract.Pscb;

/// <summary>
/// What one restoration run made of one candidate row of the bank's list (ADR027).
/// </summary>
public class PscbRestoreRowResult
{
    /// <summary>
    /// Payment number on the merchant side — the <c>orderId</c> of the bank's row.
    /// </summary>
    public string? OrderId { get; set; }

    /// <summary>
    /// Outcome of the row.
    /// </summary>
    public PscbRestoreRowOutcome Outcome { get; set; }

    /// <summary>
    /// Human-readable Russian detail: what was restored, or why it was not. A row that carries
    /// several records (a payment and its refund) reports both in this text.
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// How many lost payments this row has written: one, or none when the row was not restored.
    /// </summary>
    public int RestoredPaymentsCount { get; set; }

    /// <summary>
    /// How many refunds this row has written — the refund of a restored lost payment, or a refund
    /// restored on its own. A lost payment the bank also refunded writes two records, so both
    /// counters can be one on the same row.
    /// </summary>
    public int RestoredRefundsCount { get; set; }
}
