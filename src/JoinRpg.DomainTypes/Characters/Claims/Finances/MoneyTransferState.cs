namespace JoinRpg.DomainTypes.Claims.Finances;

public enum MoneyTransferState
{
    Approved,
    Declined,
    PendingForReceiver,
    PendingForSender,
    PendingForBoth,
}
