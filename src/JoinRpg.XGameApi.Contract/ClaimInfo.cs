namespace JoinRpg.XGameApi.Contract;
public record ClaimInfo(int ClaimId, int CharacterId, PlayerContacts PlayerContacts, ClaimStatus Status);

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
