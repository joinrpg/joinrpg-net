namespace JoinRpg.Services.Interfaces;

public class SubscribeForGroupRequest
{

    public required CharacterGroupIdentification CharacterGroupId { get; set; }
    public required SubscriptionOptions SubscriptionOptions { get; set; }
    public int MasterId { get; set; }
}
