namespace JoinRpg.WebComponents;

public class DialogCloseEventArgs : EventArgs
{
    /// <summary>
    /// Name of a button which was hit by user.
    /// </summary>
    public string? ButtonName { get; internal set; }
}

public class DialogClosingEventArgs : DialogCloseEventArgs
{
    public bool Allow { get; set; }
}
