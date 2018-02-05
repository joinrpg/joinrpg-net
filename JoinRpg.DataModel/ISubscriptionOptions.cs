namespace JoinRpg.DataModel
{
    public interface ISubscriptionOptions
    {
        bool ClaimStatusChange { get; set; }
        bool Comments { get; set; }
        bool FieldChange { get; set; }
        bool MoneyOperation { get; set; }
        bool AccommodationChange { get; set; }
    }
}
