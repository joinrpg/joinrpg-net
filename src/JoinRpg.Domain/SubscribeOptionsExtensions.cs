using JoinRpg.DataModel;

namespace JoinRpg.Domain
{
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

        public static bool AnySet(this ISubscriptionOptions request)
        {
            return request.ClaimStatusChange || request.Comments || request.FieldChange ||
                   request.MoneyOperation || request.AccommodationChange;
        }

        public static bool AnyNotSet(this ISubscriptionOptions request)
        {
            return !request.AccommodationChange || !request.ClaimStatusChange ||
                   !request.Comments || !request.FieldChange || !request.MoneyOperation;
        }

        public static T OrSetIn<T>(this T to, ISubscriptionOptions @from)
            where T : ISubscriptionOptions
        {
            to.ClaimStatusChange |= from.ClaimStatusChange;
            to.Comments |= from.Comments;
            to.FieldChange |= from.FieldChange;
            to.MoneyOperation |= from.MoneyOperation;
            to.AccommodationChange |= from.AccommodationChange;
            return to;
        }

        public static T AndSetIn<T>(this T to, ISubscriptionOptions @from)
            where T : ISubscriptionOptions
        {
            to.ClaimStatusChange &= from.ClaimStatusChange;
            to.Comments &= from.Comments;
            to.FieldChange &= from.FieldChange;
            to.MoneyOperation &= from.MoneyOperation;
            to.AccommodationChange &= from.AccommodationChange;
            return to;
        }

        public static T AndNotSetIn<T>(this T to, ISubscriptionOptions @from)
            where T : ISubscriptionOptions
        {
            to.ClaimStatusChange &= !from.ClaimStatusChange;
            to.Comments &= !from.Comments;
            to.FieldChange &= !from.FieldChange;
            to.MoneyOperation &= !from.MoneyOperation;
            to.AccommodationChange &= !from.AccommodationChange;
            return to;
        }

        public static ISubscriptionOptions AllSet()
        {
            return new OptionsImpl()
            {
                AccommodationChange = true,
                FieldChange = true,
                ClaimStatusChange = true,
                MoneyOperation = true,
                Comments = true,
            };
        }

        private class OptionsImpl : ISubscriptionOptions
        {
            public bool ClaimStatusChange { get; set; }
            public bool Comments { get; set; }
            public bool FieldChange { get; set; }
            public bool MoneyOperation { get; set; }
            public bool AccommodationChange { get; set; }
        }
    }
}
