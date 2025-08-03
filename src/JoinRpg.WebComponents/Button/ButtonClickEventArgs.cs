using Microsoft.AspNetCore.Components.Web;

namespace JoinRpg.WebComponents;

public class ButtonClickEventArgs : EventArgs
{
    public bool? ExitProgressState { get; set; }

    public bool EnterDisabledState { get; set; }

    public MouseEventArgs? MouseEvent { get; set; }
}
