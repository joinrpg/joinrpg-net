namespace JoinRpg.Web.Accommodation;

internal static class InviteStateExtensions
{
    /// <summary>Цвет, которым показывается разрешённое состояние приглашения</summary>
    public static string GetAppropriateTextStyle(this InviteState state) => state switch
    {
        InviteState.Unanswered => "text-warning",
        InviteState.Accepted => "text-success",
        InviteState.Declined => "text-danger",
        InviteState.Canceled => "text-danger",
        _ => throw new ArgumentOutOfRangeException(nameof(state)),
    };
}
