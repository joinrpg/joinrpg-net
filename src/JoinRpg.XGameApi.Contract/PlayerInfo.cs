namespace JoinRpg.XGameApi.Contract;

/// <summary>
/// Player info
/// </summary>
public class PlayerInfo
{
    /// <summary>
    /// Id
    /// </summary>
    public int PlayerId { get; set; }
    /// <summary>
    /// Nick name
    /// </summary>
    public string NickName { get; set; } = null!;
    /// <summary>
    /// Fulll name
    /// </summary>
    public string FullName { get; set; } = null!;
    /// <summary>
    /// Other nicks to search
    /// </summary>
    public string OtherNicks { get; set; } = null!;
}
