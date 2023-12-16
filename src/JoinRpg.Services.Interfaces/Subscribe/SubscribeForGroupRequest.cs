using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Services.Interfaces;

public class SubscribeForGroupRequest
{

    public required int ProjectId { get; set; }
    public required int CharacterGroupId { get; set; }
    public required SubscriptionOptions SubscriptionOptions { get; set; }
    public int MasterId { get; set; }
}
