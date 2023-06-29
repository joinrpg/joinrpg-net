namespace JoinRpg.Helpers.Web;

/// <summary>
/// Marker class (requires HTML sanitize)
/// </summary>
public class UnSafeHtml
{
    /// <summary>
    /// Value that requires validation
    /// </summary>
    public string UnValidatedValue { get; }

    private UnSafeHtml(string value) => UnValidatedValue = value;

    /// <summary>
    /// Conversion from string
    /// </summary>
    public static implicit operator UnSafeHtml?(string? s)
    {
        return s == null ? null : new UnSafeHtml(s);
    }

    /// <summary>
    /// Do not show UnvalidatedValue to prevent string.Format extracting it to
    /// not-markered string
    /// </summary>
    public override string ToString() => "UnSafeHtml";
}
