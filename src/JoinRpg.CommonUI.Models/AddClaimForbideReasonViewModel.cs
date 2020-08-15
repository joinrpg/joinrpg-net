using System;
using JoinRpg.Domain;

namespace JoinRpg.CommonUI.Models
{
    public enum AddClaimForbideReasonViewModel
    {
        ProjectNotActive,
        ProjectClaimsClosed,
        NotForDirectClaims,
        SlotsExhausted,
        Npc,
        Busy,
        AlreadySent,
        OnlyOneCharacter,
        AlredySentNotApprovedClaimToAnotherPlace,
        ApprovedClaimMovedToGroup,
        CheckedInClaimCantBeMoved,

    }

    public static class AddClaimForbideReasonToViewModel
    {
        public static AddClaimForbideReasonViewModel ToViewModel(this AddClaimForbideReason reason)
        {
            switch (reason)
            {
                case AddClaimForbideReason.ProjectNotActive:
                    return AddClaimForbideReasonViewModel.ProjectNotActive;
                case AddClaimForbideReason.ProjectClaimsClosed:
                    return AddClaimForbideReasonViewModel.ProjectClaimsClosed;
                case AddClaimForbideReason.NotForDirectClaims:
                    return AddClaimForbideReasonViewModel.NotForDirectClaims;
                case AddClaimForbideReason.SlotsExhausted:
                    return AddClaimForbideReasonViewModel.SlotsExhausted;
                case AddClaimForbideReason.Npc:
                    return AddClaimForbideReasonViewModel.Npc;
                case AddClaimForbideReason.Busy:
                    return AddClaimForbideReasonViewModel.Busy;
                case AddClaimForbideReason.AlreadySent:
                    return AddClaimForbideReasonViewModel.AlreadySent;
                case AddClaimForbideReason.OnlyOneCharacter:
                    return AddClaimForbideReasonViewModel.OnlyOneCharacter;
                case AddClaimForbideReason.ApprovedClaimMovedToGroup:
                    return AddClaimForbideReasonViewModel.ApprovedClaimMovedToGroup;
                case AddClaimForbideReason.CheckedInClaimCantBeMoved:
                    return AddClaimForbideReasonViewModel.CheckedInClaimCantBeMoved;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reason), reason, message: null);
            }
        }
    }
}
