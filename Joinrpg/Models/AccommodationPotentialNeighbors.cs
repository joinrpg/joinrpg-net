using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
    public class AccommodationPotentialNeighbors
    {
        public int ClaimId { get; set; }
        public string ClaimName { get; set; }
        public string UserName { get; set; }
        public NeighborType Type { get; set; }

        public AccommodationPotentialNeighbors(Claim claim, NeighborType type)
        {
            ClaimId = claim.ClaimId;
            ClaimName = claim.Name;
            UserName = claim.Player.PrefferedName;
            Type = type;
        }
    }

    public enum NeighborType
    {
        Current,
        WithSameType,
        NoRequest
    }
}
