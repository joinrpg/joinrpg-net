namespace JoinRpg.DataModel.Finances;

public enum MoneyTransferState
{
    Approved,
    Declined,
    PendingForReceiver,
    PendingForSender,
    PendingForBoth,
}
