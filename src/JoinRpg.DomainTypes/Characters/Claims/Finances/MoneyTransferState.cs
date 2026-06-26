namespace JoinRpg.DomainTypes.Characters.Claims.Finances;

public enum MoneyTransferState
{
    Approved,
    Declined,
    PendingForReceiver,
    PendingForSender,
    PendingForBoth,
}
