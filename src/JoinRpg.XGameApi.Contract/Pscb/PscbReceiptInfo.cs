namespace JoinRpg.XGameApi.Contract.Pscb;

/// <summary>
/// Fiscal receipt issued for a payment
/// </summary>
public class PscbReceiptInfo
{
    /// <summary>
    /// Receipt identifier in the bank's system
    /// </summary>
    public string? ReceiptId { get; set; }

    /// <summary>
    /// Receipt type as the bank reports it: "income" for an income receipt, "refund" for a refund one
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Fiscal number of the document
    /// </summary>
    public string? DocumentNumber { get; set; }

    /// <summary>
    /// Date and time of the receipt
    /// </summary>
    public DateTimeOffset? Date { get; set; }

    /// <summary>
    /// Receipt sum, RUR
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Registration number of the cash register
    /// </summary>
    public string? RegisterNumber { get; set; }

    /// <summary>
    /// Fiscal storage number
    /// </summary>
    public string? FiscalNumber { get; set; }

    /// <summary>
    /// Shift number
    /// </summary>
    public string? ShiftNumber { get; set; }

    /// <summary>
    /// Receipt number within the shift
    /// </summary>
    public string? ReceiptNumber { get; set; }
}
