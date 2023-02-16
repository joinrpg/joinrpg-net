using JoinRpg.DataModel;

namespace JoinRpg.Domain;

public static class SubscribeOptionsExtensions
{
    public static T AssignFrom<T>(this T to, ISubscriptionOptions @from)
        where T : ISubscriptionOptions
    {
        to.ClaimStatusChange = from.ClaimStatusChange;
        to.Comments = from.Comments;
        to.FieldChange = from.FieldChange;
        to.MoneyOperation = from.MoneyOperation;
        to.AccommodationChange = from.AccommodationChange;
        return to;
    }
}
