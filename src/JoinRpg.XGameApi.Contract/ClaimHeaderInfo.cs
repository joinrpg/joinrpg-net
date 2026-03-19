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
    public string CharacterName { get; set; } = null!;
    /// <summary>
    /// Player
    /// </summary>
    public PlayerInfo Player { get; set; } = null!;
}
