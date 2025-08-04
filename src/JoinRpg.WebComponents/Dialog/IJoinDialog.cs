namespace JoinRpg.WebComponents;

/// <summary>
/// Provides access to common dialog capabilities that could be managed from dialog body.
/// </summary>
public interface IJoinDialog
{
    /// <summary>
    /// Dialog caption.
    /// </summary>
    /// <remarks>This property ignored when <see cref="JoinDialog.CaptionContent"/> is explicitly set.</remarks>
    string? Caption { get; set; }

    /// <summary>
    /// Collection of <see cref="DialogButton"/> with dialog button definitions.
    /// </summary>
    /// <remarks>This property ignored when <see cref="JoinDialog.FooterContent"/> is explicitly set.</remarks>
    /// <seealso cref="DialogButtons"/>
    IReadOnlyCollection<DialogButton>? Buttons { get; set; }
}
