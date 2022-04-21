using JoinRpg.DataModel;

namespace JoinRpg.Web.Helpers;

public static class InviteStateExtensions
{
    public static string GetAppropriateTextStyle(this AccommodationRequest.InviteState state)
    {
        string result;
        switch (state)
        {
            case AccommodationRequest.InviteState.Unanswered:
                result = "text-warning";
                break;
            case AccommodationRequest.InviteState.Accepted:
                result = "text-success";
                break;
            case AccommodationRequest.InviteState.Declined:
                result = "text-danger";
                break;
            case AccommodationRequest.InviteState.Canceled:
                result = "text-danger";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        return result;
    }
}
