namespace JoinRpg.Web.ProjectMasterTools.Subscribe;

public record class EditSubscribeViewModel
{
    public required SubscribeOptionsViewModel Options { get; set; }

    public required CharacterGroupIdentification GroupId { get; set; }

    public required int MasterId { get; set; }
    public required int? UserSubscriptionId { get; set; }
}
