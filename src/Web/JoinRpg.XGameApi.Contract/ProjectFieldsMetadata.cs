namespace JoinRpg.XGameApi.Contract;

/// <summary>
/// Project fields
/// </summary>
public class ProjectFieldsMetadata
{
    /// <summary>
    /// Id
    /// </summary>
    public int ProjectId { get; set; }
    /// <summary>
    /// Name
    /// </summary>
    public required string ProjectName { get; set; }
    /// <summary>
    /// Fields
    /// </summary>
    public required IEnumerable<ProjectFieldInfo> Fields { get; set; }
}
