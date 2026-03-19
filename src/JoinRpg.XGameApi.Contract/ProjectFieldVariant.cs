namespace JoinRpg.XGameApi.Contract;

/// <summary>
/// Variant (possible dropdown value)
/// </summary>
public class ProjectFieldVariant
{
    /// <summary>
    /// Id
    /// </summary>
    public int ProjectFieldVariantId { get; set; }
    /// <summary>
    /// Label
    /// </summary>
    public string Label { get; set; } = null!;
    /// <summary>
    /// Active/deleted. Note that deleted variants can still occur!
    /// </summary>
    public bool IsActive { get; set; }
    /// <summary>
    /// Descripton (HTML)
    /// </summary>
    public string Description { get; set; } = null!;
    /// <summary>
    /// Master Descripton (HTML)
    /// </summary>
    public string MasterDescription { get; set; } = null!;
    /// <summary>
    /// Programmatic Value. Ignored by Joinrpg, to use by external system
    /// </summary>
    public string? ProgrammaticValue { get; set; }
}
