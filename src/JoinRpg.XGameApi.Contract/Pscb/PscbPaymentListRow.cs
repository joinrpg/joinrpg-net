namespace JoinRpg.XGameApi.Contract.Pscb;

/// <summary>
/// One row of the PSCB payments list — the final output model of the listing (ADR025).
/// </summary>
/// <remarks>
/// <para>
/// The row carries exactly the fields the listing shows and exports; everything else the bank
/// sends is available through the payment details endpoint (<see cref="PscbPaymentInfo"/>).
/// </para>
/// <para>
/// Coded values the bank owns are given in human-readable Russian where the vocabulary is known
/// (<see cref="PaymentKind"/>, <see cref="PaymentMethod"/>); <see cref="Status"/> keeps the bank's
/// own identifier because its vocabulary belongs to the bank.
/// </para>
/// <para>
/// <see cref="UserUrl"/> and <see cref="ProjectUrl"/> are ours, not the bank's: both are looked up
/// in the JoinRpg database, and a row whose payer or project was not found carries no link.
/// </para>
/// </remarks>
public class PscbPaymentListRow
{
    /// <summary>
    /// Payment number on the merchant side — «Номер платежа». For JoinRpg this is the finance
    /// operation id padded with zeroes to 10 digits.
    /// </summary>
    public string? OrderId { get; set; }

    /// <summary>
    /// Payment number as shown to the payer — «Отображаемый номер платежа».
    /// </summary>
    public string? DisplayOrderId { get; set; }

    /// <summary>
    /// Payment number in the bank's database — «Номер в банке».
    /// </summary>
    public string? BankPaymentId { get; set; }

    /// <summary>
    /// Payment number in the payment system — «Номер транзакции».
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    /// Payment purpose — «Назначение платежа». JoinRpg generates it as
    /// «Билет (организационный взнос) участника на {project name}», so the ticket prefix is
    /// stripped and only the project name is kept. Recurrent payments have no such prefix and
    /// keep their purpose text as is.
    /// </summary>
    public string? ProjectName { get; set; }

    /// <summary>
    /// Link to the project whose name <see cref="ProjectName"/> turned out to be. Null when no
    /// project with exactly that name was found, or when several were.
    /// </summary>
    public string? ProjectUrl { get; set; }

    /// <summary>
    /// Payment sum, RUR — «Сумма».
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Commission paid by the payer, RUR — «Комиссия».
    /// </summary>
    public decimal? Fee { get; set; }

    /// <summary>
    /// Payment status — «Статус». The bank's own identifier: new, sent, end, ref, exp, hold,
    /// canceled, err, rej, undef, done.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Moment when the payment entered its current status — «Дата».
    /// </summary>
    public DateTimeOffset StatusDate { get; set; }

    /// <summary>
    /// Kind of the record — «Вид»: «Платёж» or «Счёт».
    /// </summary>
    public string? PaymentKind { get; set; }

    /// <summary>
    /// How the payer paid — «Способ», in human-readable form. An identifier this code does not
    /// know is passed through as the bank returned it.
    /// </summary>
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Payer identifier issued by the merchant — «Пользователь». For JoinRpg this is the user's
    /// database id.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Link to the payer's profile. The user is looked up in the database by <see cref="UserId"/>
    /// and, when that finds nobody, by <see cref="UserEmail"/> — so the link may lead to a user
    /// whose id differs from the one the bank returned. Null when neither lookup found anybody.
    /// </summary>
    public string? UserUrl { get; set; }

    /// <summary>
    /// Payer's email — «Email пользователя».
    /// </summary>
    public string? UserEmail { get; set; }

    /// <summary>
    /// Comment the payer has left — «Комментарий плательщика».
    /// </summary>
    public string? PayerComment { get; set; }

    /// <summary>
    /// Code from the payments system, part of the bank's <c>lastError</c>.
    /// Null when the bank sent no code — it spells that as the text "null".
    /// </summary>
    public int? ErrorCode { get; set; }

    /// <summary>
    /// Code from the processing center, part of the bank's <c>lastError</c>.
    /// </summary>
    public int? ErrorSubCode { get; set; }

    /// <summary>
    /// Outcome of the last action with the payment, in the bank's own words — «Состояние». For a
    /// finished payment this is not an error but a description, e.g. «Платеж завершен».
    /// </summary>
    public string? ErrorDescription { get; set; }
}
