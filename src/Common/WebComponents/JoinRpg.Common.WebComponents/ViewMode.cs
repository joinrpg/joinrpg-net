namespace JoinRpg.Common.WebComponents;

public enum ViewMode
{
    Show,
    ShowAsPrivate,
    Hide,
    /// <summary>
    /// Объекта больше нет (например, пользователь удалён). Показываем пояснение вместо ссылки.
    /// В отличие от <see cref="Hide"/>, тут дело не в правах: показывать просто нечего.
    /// </summary>
    Deleted
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
