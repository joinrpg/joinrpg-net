namespace JoinRpg.DataModel;

public enum FinanceOperationState
{
    /// <summary>
    /// Operation approved
    /// </summary>
    Approved,

    /// <summary>
    /// Operation is being processed
    /// </summary>
    Proposed,

    /// <summary>
    /// Operation was declined
    /// </summary>
    Declined,

    /// <summary>
    /// Operation is invalid (typically online payment that doesn't have a corresponding object on the bank side)
    /// </summary>
    Invalid,

    /// <summary>
    /// Operation has expired (user has entered the payment process but not finished until timeout)
    /// </summary>
    Expired,
}
