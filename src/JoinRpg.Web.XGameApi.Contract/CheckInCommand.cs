namespace JoinRpg.Web.XGameApi.Contract;

/// <summary>
/// Command to check-in some claim
/// </summary>
public class CheckInCommand
{
    /// <summary>
    /// Claim id
    /// </summary>
    public int ClaimId { get; set; }
    /// <summary>
    /// Money paid
    /// </summary>
    public int MoneyPaid { get; set; }
    /// <summary>
    /// Check in time to support offlne checkin scenario. Put null here for now
    /// </summary>
    public DateTime CheckInTime { get; set; }
}
