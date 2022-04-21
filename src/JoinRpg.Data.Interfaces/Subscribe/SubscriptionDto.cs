using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces.Subscribe;

public class SubscriptionDto : ISubscriptionOptions
{
    public bool ClaimStatusChange { get; set; }
    public bool Comments { get; set; }
    public bool FieldChange { get; set; }
    public bool MoneyOperation { get; set; }
    public bool AccommodationChange { get; set; }
}
