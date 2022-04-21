namespace JoinRpg.Web.XGameApi.Contract.Schedule;

/// <summary>
/// Information about program item author
/// </summary>
public class AuthorInfoApi
{
    /// <summary>
    /// Display name (plain text)
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Id of user which represents author in joinrpg. Stable, never changes. 
    /// </summary>
    public int UserId { get; set; }
}
