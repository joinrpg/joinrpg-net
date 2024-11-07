namespace JoinRpg.Domain;

public enum AddClaimForbideReason
{
    ProjectNotActive,
    ProjectClaimsClosed,
    NotForDirectClaims,
    SlotsExhausted,
    Npc,
    Busy,
    AlreadySent,
    OnlyOneCharacter,
    ApprovedClaimMovedToGroupOrSlot,
    CheckedInClaimCantBeMoved,
    CharacterInactive
}
