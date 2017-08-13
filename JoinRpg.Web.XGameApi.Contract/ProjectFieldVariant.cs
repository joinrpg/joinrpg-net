namespace JoinRpg.Web.XGameApi.Contract
{
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
    public string Label { get; set; }
    /// <summary>
    /// Active/deleted. Note that deleted variants can still occur!
    /// </summary>
    public bool IsActive { get; set; }
    /// <summary>
    /// Descripton (HTML)
    /// </summary>
    public string Description { get; set; }
    /// <summary>
    /// Master Descripton (HTML)
    /// </summary>
    public string MasterDescription { get; set; }
    /// <summary>
    /// Programmatic Value. Ignored by Joinrpg, to use by external system
    /// </summary>
    public string ProgrammaticValue { get; set; }
  }
}