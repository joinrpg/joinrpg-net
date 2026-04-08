namespace JoinRpg.DataModel.Finances;

/// <summary>
/// Describes how a field with price should be treated when issuing a receipt.
/// </summary>
public enum ReceiptItemType
{
    /// <summary>
    /// Field should not have its own receipt position.
    /// </summary>
    IncludeIntoPrimary,

    /// <summary>
    /// Field represents a certain commodity.
    /// </summary>
    Commodity,

    /// <summary>
    /// Field represents a certain service.
    /// </summary>
    Service,
}
