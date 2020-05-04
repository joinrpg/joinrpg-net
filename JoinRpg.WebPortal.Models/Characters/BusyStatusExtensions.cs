using System.Linq;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models.Characters
{
    public static class BusyStatusExtensions
    {
        public static CharacterBusyStatusView GetBusyStatus(this Character character)
        {
            if (character.ApprovedClaim != null)
            {
                return CharacterBusyStatusView.HasPlayer;
            }
            if (character.Claims.Any(c => c.ClaimStatus.IsActive()))
            {
                return CharacterBusyStatusView.Discussed;
            }
            if (character.IsAcceptingClaims)
            {
                return CharacterBusyStatusView.NoClaims;
            }
            return CharacterBusyStatusView.Npc;
        }

        public static CharacterBusyStatusView GetBusyStatus(this CharacterView character)
        {
            if (character.ApprovedClaim != null)
            {
                return CharacterBusyStatusView.HasPlayer;
            }
            if (character.Claims.Any(c => c.IsActive))
            {
                return CharacterBusyStatusView.Discussed;
            }
            if (character.IsAcceptingClaims)
            {
                return CharacterBusyStatusView.NoClaims;
            }
            return CharacterBusyStatusView.Npc;
        }
    }

}
