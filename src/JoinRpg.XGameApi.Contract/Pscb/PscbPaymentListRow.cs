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
/// in the JoinRpg database, and a row whose payer or project was not found carries no link and is
/// reported in <see cref="PscbPaymentsListResult.UnresolvedUsers"/> or
/// <see cref="PscbPaymentsListResult.UnresolvedProjects"/>.
/// </para>
/// <para>
/// The refund is reported by both sides: <see cref="Refunds"/> and <see cref="RefundedByBank"/>
/// restate what the bank sent, while <see cref="RefundUrl"/> is ours — a link to the JoinRpg
/// finance operation that recorded the refund.
/// </para>
/// <para>
/// <see cref="MatchState"/>, <see cref="OperationUrl"/> and <see cref="ClaimUrl"/> are the
/// reconciliation with our own database (ADR026): the bank's <c>orderId</c> is the
/// <c>CommentId</c> of a finance operation, so a row either finds one or the payment is lost.
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
    /// True when the bank reports the payment as cancelled and completely refunded — its
    /// <c>state</c> is "ref". A partially refunded payment keeps the status "end", so this flag
    /// on its own does not cover partial refunds.
    /// </summary>
    public bool RefundedByBank { get; set; }

    /// <summary>
    /// Refunds of this payment as the bank reports them — «Возврат». The bank sends the array only
    /// on the payments that actually have refunds, so it is empty for the rest.
    /// </summary>
    public IReadOnlyCollection<PscbRefundInfo> Refunds { get; set; } = [];

    /// <summary>
    /// Link to the JoinRpg finance operation that recorded the refund. Null when JoinRpg has no
    /// approved refund for this payment, while the bank's own refund is in <see cref="Refunds"/> —
    /// a discrepancy the page shows as such.
    /// </summary>
    public string? RefundUrl { get; set; }

    /// <summary>
    /// Link to the JoinRpg finance operation the bank's <see cref="OrderId"/> points at —
    /// «Ссылка на операцию». Null when there is no such operation, which is exactly what makes the
    /// payment a lost one.
    /// </summary>
    public string? OperationUrl { get; set; }

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
    /// Link to the claim the payment belongs to — «Ссылка на заявку». For a row that found its
    /// finance operation the claim is the operation's own; there is no link where no operation was
    /// found. Null when the reconciliation was not performed.
    /// </summary>
    public string? ClaimUrl { get; set; }

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

    /// <summary>
    /// What the reconciliation with the JoinRpg database made of this row — «Сверка» (ADR026).
    /// Null when the reconciliation was not performed: enriching the list with our data is
    /// best-effort, and an absent result must not read as one of the outcomes.
    /// </summary>
    public PscbPaymentMatchState? MatchState { get; set; }

    /// <summary>
    /// <see cref="MatchState"/> in human-readable Russian, the way <see cref="PaymentKind"/> and
    /// <see cref="PaymentMethod"/> are given. The enumeration stays on the wire for the logic —
    /// the restore button is enabled by it — while the text is what the page and the export show.
    /// </summary>
    public string? MatchStateText { get; set; }
}
