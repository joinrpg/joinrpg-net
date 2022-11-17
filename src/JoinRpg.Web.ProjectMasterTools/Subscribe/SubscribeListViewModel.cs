namespace JoinRpg.Web.ProjectMasterTools.Subscribe;

public class SubscribeListViewModel
{
    public required List<SubscribeListItemViewModel> Items { get; set; }

    public required string[] PaymentTypeNames { get; set; }

    public bool AllowChanges { get; set; }

    public int ProjectId { get; set; }
    public int MasterId { get; set; }
}
