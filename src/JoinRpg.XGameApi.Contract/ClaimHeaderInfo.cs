namespace JoinRpg.XGameApi.Contract;

/// <summary>
/// Claim info
/// </summary>
public class ClaimHeaderInfo
{
    /// <summary>
    /// id
    /// </summary>
    public int ClaimId { get; set; }
    /// <summary>
    /// Name of characters
    /// </summary>
    public string CharacterName { get; set; }
    /// <summary>
    /// Player
    /// </summary>
    public CheckinPlayerInfo Player { get; set; }
}
