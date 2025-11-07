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

// Более широкий вариант статуса, подразумевает комбинацию нескольких статусов.
public enum ClaimStatusSpec
{
    Any,
    Active,
    InActive,
    Discussion,
    OnHold, Approved,
    ReadyForCheckIn,
    CheckedIn,
    ActiveOrOnHold,
}
