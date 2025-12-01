using JoinRpg.PrimitiveTypes.Users;

namespace JoinRpg.PrimitiveTypes;

public record class SubscriptionOptions
{
    public required bool ClaimStatusChange { get; set; }
    public required bool Comments { get; set; }
    public required bool FieldChange { get; set; }
    public required bool MoneyOperation { get; set; }
    public required bool AccommodationChange { get; set; }
    public required bool AccommodationInvitesChange { get; set; }

    public bool AnySet => ClaimStatusChange || Comments || FieldChange || MoneyOperation || AccommodationChange;

    public static SubscriptionOptions CreateAllSet()
        => new()
        {
            AccommodationChange = true,
            FieldChange = true,
            ClaimStatusChange = true,
            MoneyOperation = true,
            Comments = true,
            AccommodationInvitesChange = true,
        };

    public static SubscriptionOptions CreateNoneSet()
        => new()
        {
            AccommodationChange = false,
            FieldChange = false,
            ClaimStatusChange = false,
            MoneyOperation = false,
            Comments = false,
            AccommodationInvitesChange = false,
        };
}

public record class UserSubscribe(UserInfoHeader User, SubscriptionOptions Options)
{
    public UserSubscribe(UserInfoHeader User) : this(User, SubscriptionOptions.CreateAllSet()) { }
}
