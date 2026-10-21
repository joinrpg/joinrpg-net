namespace JoinRpg.XGameApi.Contract.Pscb;

/// <summary>
/// Error the bank reported for the last action performed with a payment
/// </summary>
public class PscbBankErrorInfo
{
    /// <summary>
    /// Free-form description of the error
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Numeric code from the payments system. Zero means no error
    /// </summary>
    public int? PaymentsSystemCode { get; set; }

    /// <summary>
    /// Numeric code from the processing center
    /// </summary>
    public int? ProcessingCenterCode { get; set; }
}
