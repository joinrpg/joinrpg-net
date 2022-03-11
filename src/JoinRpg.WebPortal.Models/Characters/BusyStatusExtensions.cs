using System.Linq;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Web.Models.Characters
{
    public static class BusyStatusExtensions
    {
        public static CharacterBusyStatusView GetBusyStatus(this Character character)
        {
            return character switch
            {

                { CharacterType: CharacterType.NonPlayer } => CharacterBusyStatusView.Npc,
                { CharacterType: CharacterType.Slot } => CharacterBusyStatusView.Slot,
                { CharacterType: CharacterType.Player, ApprovedClaim: not null } => CharacterBusyStatusView.HasPlayer,
                { CharacterType: CharacterType.Player } when character.Claims.Any(c => c.ClaimStatus.IsActive()) => CharacterBusyStatusView.Discussed,
                { CharacterType: CharacterType.Player } => CharacterBusyStatusView.NoClaims,
                _ => CharacterBusyStatusView.Unknown,
            };
        }

        public static CharacterBusyStatusView GetBusyStatus(this CharacterView character)
        {
            return character switch
            {
                { CharacterTypeInfo: { CharacterType: CharacterType.NonPlayer } } => CharacterBusyStatusView.Npc,
                { CharacterTypeInfo: { CharacterType: CharacterType.Slot } } => CharacterBusyStatusView.Slot,
                { CharacterTypeInfo: { CharacterType: CharacterType.Player }, ApprovedClaim: not null } => CharacterBusyStatusView.HasPlayer,
                { CharacterTypeInfo: { CharacterType: CharacterType.Player }, } when character.Claims.Any(c => c.IsActive) => CharacterBusyStatusView.Discussed,
                { CharacterTypeInfo: { CharacterType: CharacterType.Player } } => CharacterBusyStatusView.NoClaims,
                _ => CharacterBusyStatusView.Unknown,
            };
        }
    }

}
