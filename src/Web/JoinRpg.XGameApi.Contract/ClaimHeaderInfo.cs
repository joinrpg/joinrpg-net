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
    public required string CharacterName { get; set; }
    /// <summary>
    /// Player
    /// </summary>
    public required CheckinPlayerInfo Player { get; set; }
}
