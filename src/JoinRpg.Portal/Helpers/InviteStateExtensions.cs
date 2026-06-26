using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.Web.Helpers;

public static class InviteStateExtensions
{
    public static string GetAppropriateTextStyle(this InviteState state)
    {
        string result;
        switch (state)
        {
            case InviteState.Unanswered:
                result = "text-warning";
                break;
            case InviteState.Accepted:
                result = "text-success";
                break;
            case InviteState.Declined:
                result = "text-danger";
                break;
            case InviteState.Canceled:
                result = "text-danger";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        return result;
    }
}
