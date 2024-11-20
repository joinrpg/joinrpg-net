namespace JoinRpg.Domain;

public enum AddClaimForbideReason
{
    ProjectNotActive,
    ProjectClaimsClosed,
    SlotsExhausted,
    Npc,
    Busy,
    AlreadySent,
    OnlyOneCharacter,
    ApprovedClaimMovedToGroupOrSlot,
    CheckedInClaimCantBeMoved,
    CharacterInactive
}
