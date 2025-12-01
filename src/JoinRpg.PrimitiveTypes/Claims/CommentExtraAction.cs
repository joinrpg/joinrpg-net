namespace JoinRpg.PrimitiveTypes.Claims;

public enum CommentExtraAction
{
    ApproveFinance = 0,
    RejectFinance = 1,
    ApproveByMaster = 2,
    DeclineByMaster = 3,
    RestoreByMaster = 4,
    MoveByMaster = 5,
    DeclineByPlayer = 6,
    ChangeResponsible = 7,
    NewClaim = 8,
    OnHoldByMaster = 9,
    FeeChanged = 10,
    CheckedIn = 11,
    SecondRole = 12,
    OutOfGame = 13,
    RequestPreferential = 14,
    PaidFee = 15,
    PcsbOnlineFeeAccepted = 16,
}
