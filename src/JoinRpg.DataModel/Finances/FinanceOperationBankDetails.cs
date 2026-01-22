using System.ComponentModel.DataAnnotations;

namespace JoinRpg.DataModel.Finances;

public class FinanceOperationBankDetails
{
    public int CommentId { get; set; }

    /// <summary>
    /// Identifier of a refund operation received from bank.
    /// </summary>
    [MaxLength(256)]
    public string? BankRefundKey { get; set; }

    /// <summary>
    /// Identifier of an operation received from bank.
    /// </summary>
    [MaxLength(256)]
    public string? BankOperationKey { get; set; }

    [MaxLength(2048)]
    public string? QrCodeLink { get; set; }

    [MaxLength(2048)]
    public string? QrCodeMeta { get; set; }


}
