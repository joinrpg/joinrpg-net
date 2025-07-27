namespace JoinRpg.WebComponents;

/// <summary>
/// Provides access to common dialog capabilities that could be managed from dialog body.
/// </summary>
public interface IDialog
{
    /// <summary>
    /// The caption of dialog.
    /// </summary>
    string? Caption { get; set; }



    /// <summary>
    /// Closes the dialog.
    /// </summary>
    void Close();
}
