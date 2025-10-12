namespace JoinRpg.PrimitiveTypes.Claims;

public enum ClaimStatus
{
    AddedByUser,
    AddedByMaster,
    Approved,
    DeclinedByUser,
    DeclinedByMaster,
    Discussed,
    OnHold,
    CheckedIn,
}


public enum ClaimDenialReason
{
    Unavailable,
    Refused,
    NotRespond,
    Removed,
    NotSuitable,
    NotImplementable,
}


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
    CharacterInactive,
    RealNameMissing,
    PhoneMissing,
    TelegramMissing,
    VkontakteMissing,
}
