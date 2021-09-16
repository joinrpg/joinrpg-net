namespace JoinRpg.Web.Models
{
    public class InviteRequestViewModel
    {
        public const string AccommodationRequestPrefix = "ar";
        public int ProjectId { get; set; }
        public int ClaimId { get; set; }
        public int RequestId { get; set; }
        public int InviteId { get; set; }
        public string ReceiverClaimOrAccommodationRequest { get; set; }
    }
}
