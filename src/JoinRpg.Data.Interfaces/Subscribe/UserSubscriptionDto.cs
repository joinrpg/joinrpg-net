namespace JoinRpg.Data.Interfaces.Subscribe;

public class UserSubscriptionDto
{
    public UserSubscriptionDto()
    {
    }

    public int UserSubscriptionId { get; set; }
    public int ProjectId { get; set; }
    public int? CharacterGroupId { get; set; }
    public string CharacterGroupName { get; set; }
    public int? CharacterId { get; set; }
    public string CharacterNames { get; set; }
    public int? ClaimId { get; set; }
    public string? ClaimName { get; set; }

    public SubscriptionDto Options { get; set; }
}
