namespace JoinRpg.XGameApi.Contract.Pscb;

/// <summary>
/// What one restoration run made of one candidate row of the bank's list (ADR026).
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
}
