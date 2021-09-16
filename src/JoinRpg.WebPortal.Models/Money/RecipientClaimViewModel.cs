using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models
{
    public class RecipientClaimViewModel : JoinSelectListItem
    {
        public int ClaimId { get; }

        public string Name { get; }

        public User Player { get; }

        public string Status { get; }

        public RecipientClaimViewModel(Claim source)
        {
            ClaimId = source.ClaimId;
            Name = source.Name;
            Player = source.Player;
            Status = ((ClaimStatusView)source.ClaimStatus).GetDisplayName();

            Text = $"{Name} ({Status}" + (Player != null ? $", {Player.GetDisplayName()}" : "") + ")";
            Value = ClaimId;
        }
    }
}
