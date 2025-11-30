using JoinRpg.DataModel;

namespace JoinRpg.Services.Impl;

public static class SubscribeOptionsExtensions
{
    public static UserSubscription AssignFrom(this UserSubscription to, SubscriptionOptions @from)
    {
        to.ClaimStatusChange = from.ClaimStatusChange;
        to.Comments = from.Comments;
        to.FieldChange = from.FieldChange;
        to.MoneyOperation = from.MoneyOperation;
        to.AccommodationChange = from.AccommodationChange;
        return to;
    }
}
