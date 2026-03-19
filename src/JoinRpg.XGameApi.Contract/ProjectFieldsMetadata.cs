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
    public string ProjectName { get; set; } = null!;
    /// <summary>
    /// Fields
    /// </summary>
    public IEnumerable<ProjectFieldInfo> Fields { get; set; } = null!;
}
