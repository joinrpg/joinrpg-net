using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Extensions;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Web.Claims;

namespace JoinRpg.Web.Models.Characters;

public static class BusyStatusExtensions
{
    public static CharacterBusyStatusView GetBusyStatus(this Character character)
        => GetBusyStatus(character.ToCharacterTypeInfo(), character.ApprovedClaimId is not null, character.Claims.Any(c => c.ClaimStatus.IsActive()));

    public static CharacterBusyStatusView GetBusyStatus(this UgDto character)
        => GetBusyStatus(character.CharacterTypeInfo, character.ApprovedClaimUserId is not null, character.HasActiveClaims);

    public static CharacterBusyStatusView GetBusyStatus(this CharacterView character)
        => GetBusyStatus(character.CharacterTypeInfo, character.ApprovedClaim is not null, character.Claims.Any(c => c.IsActive));

    private static CharacterBusyStatusView GetBusyStatus(CharacterTypeInfo typeInfo, bool hasApproved, bool hasActive)
    {
        var tuple = (typeInfo, hasApproved, hasActive);
        return tuple switch
        {

            { typeInfo.CharacterType: CharacterType.NonPlayer } => CharacterBusyStatusView.Npc,
            { typeInfo.CharacterType: CharacterType.Slot, typeInfo.IsHot: true } => CharacterBusyStatusView.HotSlot,
            { typeInfo.CharacterType: CharacterType.Slot } => CharacterBusyStatusView.Slot,
            { typeInfo.CharacterType: CharacterType.Player, hasApproved: true } => CharacterBusyStatusView.HasPlayer,
            { typeInfo.CharacterType: CharacterType.Player, hasActive: true } => CharacterBusyStatusView.Discussed,
            { typeInfo.CharacterType: CharacterType.Player, typeInfo.IsHot: true } => CharacterBusyStatusView.HotVacancy,
            { typeInfo.CharacterType: CharacterType.Player } => CharacterBusyStatusView.Vacancy,
            _ => CharacterBusyStatusView.Unknown,
        };
    }
}

