namespace JoinRpg.XGameApi.Contract;

public record ClaimInfo(int ClaimId, int CharacterId, PlayerContacts PlayerContacts, ClaimStatusEnum Status);

public enum ClaimStatusEnum
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
