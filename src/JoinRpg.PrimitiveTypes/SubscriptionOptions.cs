using JoinRpg.DataModel;

namespace JoinRpg.PrimitiveTypes;

public class SubscriptionOptions : ISubscriptionOptions
{
    public required bool ClaimStatusChange { get; set; }
    public required bool Comments { get; set; }
    public required bool FieldChange { get; set; }
    public required bool MoneyOperation { get; set; }
    public required bool AccommodationChange { get; set; }
}
