namespace JoinRpg.Web.XGameApi.Contract;

/// <summary>
/// Field metadata description
/// </summary>
public class ProjectFieldInfo
{
    /// <summary>
    /// Name
    /// </summary>
    public string FieldName { get; set; }

    /// <summary>
    /// Id
    /// </summary>
    public int ProjectFieldId { get; set; }

    /// <summary>
    /// Is active or deleted. Note that deleted fields still can have values
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Field type string
    /// </summary>
    public string FieldType { get; set; }

    /// <summary>
    /// Variants
    /// </summary>
    public IEnumerable<ProjectFieldVariant> ValueList { get; set; }

    /// <summary>
    /// Programmatic Value. Ignored by Joinrpg, to use by external system
    /// </summary>
    public string ProgrammaticValue { get; set; }
}
