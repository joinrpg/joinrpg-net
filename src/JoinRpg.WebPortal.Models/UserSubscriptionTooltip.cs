namespace JoinRpg.Domain;

public class UserSubscriptionTooltip
{
    public string Tooltip { get; set; }
    public bool HasFullParentSubscription { get; set; }
    public bool IsDirect { get; set; }
    public bool ClaimStatusChange { get; set; }
    public bool Comments { get; set; }
    public bool FieldChange { get; set; }
    public bool MoneyOperation { get; set; }
}
