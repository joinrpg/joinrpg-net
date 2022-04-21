namespace JoinRpg.Web.XGameApi.Contract;

/// <summary>
/// Field value for character
/// </summary>
public class FieldValue
{
    /// <summary>
    /// Field id.
    /// Use metadata API to get metadata about fields (i.e. programatical value, label...)
    /// </summary>
    public int ProjectFieldId { get; set; }
    /// <summary>
    /// Actual value. If dropdown, id of variant
    /// </summary>
    public string Value { get; set; }
    /// <summary>
    /// Value how it meant to be displayed. If dropdown, label of variant
    /// </summary>
    public string DisplayString { get; set; }
}
