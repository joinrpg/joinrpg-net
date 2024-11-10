namespace JoinRpg.Web.Models;

public class InviteRequestViewModel
{
    public int ProjectId { get; set; }
    public int ClaimId { get; set; }
    public int RequestId { get; set; }
    public int InviteId { get; set; }
    public int ReceiverClaimOrAccommodationRequest { get; set; }
}
