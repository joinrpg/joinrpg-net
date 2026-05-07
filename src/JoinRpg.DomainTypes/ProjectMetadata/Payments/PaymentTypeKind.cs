namespace JoinRpg.DataModel;

/// <summary>
/// Payment type kinds
/// </summary>
public enum PaymentTypeKind
{
    /// <summary>
    /// Custom, non-specific user-defined payment type
    /// </summary>
    Custom = 0,

    /// <summary>
    /// Cash payment to a master
    /// </summary>
    Cash = 1,

    /// <summary>
    /// Online payment type through online payment gate
    /// </summary>
    Online = 2,

    /// <summary>
    /// Online subscription payment
    /// </summary>
    OnlineSubscription = 3,
}
