namespace JoinRpg.XGameApi.Contract;

/// <summary>
/// Character group
/// </summary>
public class GroupHeader
{
    /// <summary>
    /// Id
    /// </summary>
    public int CharacterGroupId { get; set; }
    /// <summary>
    /// Name
    /// </summary>
    public required string CharacterGroupName { get; set; }
}
