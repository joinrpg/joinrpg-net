namespace JoinRpg.Web.XGameApi.Contract;

/// <summary>
/// Validation of claim for checkin
/// </summary>
public class ClaimCheckInValidationResult
{
    /// <summary>
    /// Id
    /// </summary>
    public int ClaimId { get; set; }
    /// <summary>
    /// Checked in already (couldnot checkin twice)
    /// </summary>
    public bool CheckedIn { get; set; }
    /// <summary>
    /// Approved claim or not (not approved could not checkin)
    /// </summary>
    public bool Approved { get; set; }
    /// <summary>
    /// Every filed filled (if not, could not checkin)
    /// </summary>
    public bool EverythingFilled { get; set; }
    /// <summary>
    /// Could we checkin !CheckedIn AND Approved AND EverythingFilled
    /// </summary>
    public bool CheckInPossible { get; set; }
    /// <summary>
    /// Balance to pay
    /// </summary>
    public int ClaimFeeBalance { get; set; }
    /// <summary>
    /// Handouts to deliver
    /// </summary>
    public HandoutItem[] Handouts { get; set; }
}
