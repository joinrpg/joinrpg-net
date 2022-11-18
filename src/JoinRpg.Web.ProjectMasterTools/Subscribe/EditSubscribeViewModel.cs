namespace JoinRpg.Web.ProjectMasterTools.Subscribe;

public class EditSubscribeViewModel
{
    public required SubscribeOptionsViewModel Options { get; set; }

    public required int GroupId { get; set; }

    public required int MasterId { get; set; }
    public required int? UserSubscriptionId { get; set; }
}
