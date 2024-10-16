namespace JoinRpg.WebComponents;

public enum ViewMode
{
    Show,
    ShowAsPrivate,
    Hide
}
public record UserLinkViewModel(int UserId, string DisplayName, ViewMode ViewMode)
{
    public static UserLinkViewModel Hidden = new(-1, "скрыто", ViewMode.Hide);
}