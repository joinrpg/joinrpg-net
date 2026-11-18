namespace JoinRpg.Web.Claims.Finance;

/// <summary>
/// Что показать мастеру в диалоге модерации, чтобы решать, не листая заявку.
/// </summary>
/// <param name="Title">Вид операции: «Оплата», «Запрос льготы».</param>
/// <param name="PaymentTypeName">Способ оплаты, если он у операции есть.</param>
public record FinanceModerationOperationView(
    FinanceOperationIdentification OperationId,
    string Title,
    int Money,
    DateOnly OperationDate,
    string? PaymentTypeName);
