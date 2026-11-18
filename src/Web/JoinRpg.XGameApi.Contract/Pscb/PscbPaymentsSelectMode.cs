namespace JoinRpg.XGameApi.Contract.Pscb;

/// <summary>
/// Which payment statuses to include into the PSCB payments list
/// </summary>
public enum PscbPaymentsSelectMode
{
    /// <summary>
    /// Only completed payments. This is the bank's default
    /// </summary>
    Paid,

    /// <summary>
    /// All payments, including the unfinished ones
    /// </summary>
    Created,
}
