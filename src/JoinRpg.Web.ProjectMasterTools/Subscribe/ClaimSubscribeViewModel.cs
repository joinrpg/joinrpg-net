using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Web.ProjectMasterTools.Subscribe;

public record ClaimSubscribeViewModel
{
    public required bool IsDirect { get; set; }
    public required SubscriptionOptions ParentSubscribe { get; set; }
    public Dictionary<string, string> SubscribeReason { get; set; } = [];
}
