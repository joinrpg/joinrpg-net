namespace JoinRpg.XGameApi.Contract;

public record ClaimDetails(int ClaimId, int CharacterId, PlayerContacts PlayerContacts, ClaimStatusEnum Status);

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
