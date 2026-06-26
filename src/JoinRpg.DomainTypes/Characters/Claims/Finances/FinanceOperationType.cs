namespace JoinRpg.DomainTypes.Characters.Claims.Finances;

/// <summary>
/// Types of finance operations
/// </summary>
public enum FinanceOperationType
{
    /// <summary>
    /// Request for preferential fee
    /// </summary>
    PreferentialFeeRequest = -2,

    /// <summary>
    /// Finance operation submits payment using cash or custom payment type
    /// </summary>
    Submit = 0,

    /// <summary>
    /// Online payment
    /// </summary>
    Online,

    /// <summary>
    /// Refund operation
    /// </summary>
    Refund,

    /// <summary>
    /// Money transfer to another claim
    /// </summary>
    TransferTo,

    /// <summary>
    /// Money transfer from another claim
    /// </summary>
    TransferFrom,
}
