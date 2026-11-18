namespace JoinRpg.Web.ProjectMasterTools.Subscribe;

public class SubscribeListItemViewModel
{
    public required Uri Link { get; set; }
    public required string Name { get; set; }

    public required SubscribeOptionsViewModel Options { get; set; }

    public int UserSubscriptionId { get; set; }
    public int ProjectId { get; set; }
}
