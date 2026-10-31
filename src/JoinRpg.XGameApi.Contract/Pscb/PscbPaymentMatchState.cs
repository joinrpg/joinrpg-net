namespace JoinRpg.XGameApi.Contract.Pscb;

/// <summary>
/// Outcome of matching one row of the bank's payments list against the JoinRpg database (ADR028).
/// </summary>
/// <remarks>
/// <para>
/// The bank's <c>orderId</c> is the <c>CommentId</c> of our finance operation padded with zeroes
/// to 10 digits, so a row either points at an operation or it does not. The value tells the admin
/// what the row means: only <see cref="Lost"/> and <see cref="RefundMissing"/> are candidates for
/// the restore, and only when everything it needs resolves unambiguously.
/// </para>
/// <para>
/// A row that was not matched at all — because enriching the list with our data failed — carries
/// no value: <see cref="PscbPaymentListRow.MatchState"/> is nullable, and a missing reconciliation
/// must not look like a result.
/// </para>
/// </remarks>
public enum PscbPaymentMatchState
{
    /// <summary>
    /// The <c>orderId</c> is not a number, so it cannot point at a finance operation at all.
    /// The bank spells an abandoned attempt whose number the merchant reused as
    /// <c>__REPLACED_0000644272</c> (ADR025).
    /// </summary>
    NoRecord = 0,

    /// <summary>
    /// The bank got no money for this row: its <c>state</c> is neither <c>end</c> nor <c>ref</c> —
    /// an expired or a failed payment. There is nothing to reconcile.
    /// </summary>
    NoMoney = 1,

    /// <summary>
    /// The operation exists and is settled: an online payment in the approved state, with our own
    /// approved refund where the bank reports one.
    /// </summary>
    Settled = 2,

    /// <summary>
    /// The operation exists but is not settled — declined, invalid, expired or still proposed —
    /// while the bank says the money arrived. Shown as a discrepancy and left as it is: the tool
    /// never changes an operation that already exists (ADR028).
    /// </summary>
    NotSettled = 3,

    /// <summary>
    /// The bank has the money and we have no operation for it — a lost payment, the one this tool
    /// restores.
    /// </summary>
    Lost = 4,

    /// <summary>
    /// The payment itself is settled, but the refund the bank reports has no approved operation of
    /// ours. Only the refund is restored.
    /// </summary>
    RefundMissing = 5,
}
