using System.Linq;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models.Characters
{
  internal static class BusyStatusExtensions
  {
    public static CharacterBusyStatusView GetBusyStatus(this Character character)
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