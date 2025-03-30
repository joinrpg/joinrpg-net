namespace JoinRpg.WebComponents;

public enum ViewMode
{
    Show,
    ShowAsPrivate,
    Hide
}

public static class ViewModeSelector
{
    public static ViewMode Create(bool isPublic, bool canViewPrivate)
    {
        return (isPublic, canViewPrivate) switch
        {
            (true, _) => ViewMode.Show,
            (false, true) => ViewMode.ShowAsPrivate,
            (false, false) => ViewMode.Hide,
        };
    }
}
