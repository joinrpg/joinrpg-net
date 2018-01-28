using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
    public class InviteRequestViewModel
    {
        public int ProjectId { get; set; }
        public int ClaimId { get; set; }
        public int RequestId { get; set; }
        public int ReceiverClaimId { get; set; }
        public int InviteId { get; set; }
        public AccommodationRequest.InviteState InviteState { get; set; }
    }
}
