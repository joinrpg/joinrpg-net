using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces;

public class SubscribeForGroupRequest : ISubscriptionOptions
{

    public int ProjectId { get; set; }
    public int CharacterGroupId { get; set; }
    public bool ClaimStatusChange { get; set; }
    public bool Comments { get; set; }
    public bool FieldChange { get; set; }
    public bool MoneyOperation { get; set; }
    public bool AccommodationChange { get; set; }
}
