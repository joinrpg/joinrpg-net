namespace JoinRpg.XGameApi.Contract.Pscb;

/// <summary>
/// Outcome of restoring one candidate row (ADR028).
/// </summary>
public enum PscbRestoreRowOutcome
{
    /// <summary>
    /// The run has written the row's payment, refund, or both.
    /// </summary>
    Restored = 0,

    /// <summary>
    /// The records of this row are already in the database — an earlier run or the production
    /// path has settled them. This run has written nothing, which is the idempotency the
    /// restoration is built on.
    /// </summary>
    AlreadySettled = 1,

    /// <summary>
    /// The row was not restored: its number is taken or is not a lost one, or the write has
    /// failed. <see cref="PscbRestoreRowResult.Message"/> says which; the row is left to a human.
    /// </summary>
    Failed = 2,
}
