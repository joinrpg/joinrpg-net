using JoinRpg.XGameApi.Contract.Pscb;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PscbApi;
using PscbApi.Models;

namespace JoinRpg.WebPortal.Managers.AdminTools;

/// <summary>
/// Преобразование ответов банка в контракт шлюза (ADR025).
/// </summary>
/// <remarks>
/// <para>
/// Отдельный статический класс по паттерну Builder из <c>docs/ui-pages-common.md</c>:
/// преобразование проверяется тестами без банка и без репозиториев.
/// </para>
/// <para>
/// Кодированные значения — статус платежа, метод, вид записи, коды ошибок — отдаются строками,
/// а не перечислениями: словарь значений принадлежит банку, и наше перечисление пришлось бы править
/// каждый раз, когда банк добавляет значение. Имена полей при этом наши.
/// </para>
/// </remarks>
internal static class PscbPaymentViewModelBuilder
{
    public static PscbPaymentsListResult BuildList(PaymentsListInfo info, int requestCount)
        => new()
        {
            IsSuccess = IsSuccess(info),
            ErrorCode = IdentifierOf(info.ErrorCode),
            ErrorDescription = info.ErrorDescription,
            RequestId = info.RequestId,
            RequestCount = requestCount,
            Payments = info.Payments?.Select(payment => Build(payment)).ToList() ?? [],
        };

    public static PscbPaymentsListResult ListFailure(string message, int requestCount)
        => new() { IsSuccess = false, ErrorDescription = message, RequestCount = requestCount };

    public static PscbPaymentDetailsResult BuildDetails(PaymentInfo info)
        => new()
        {
            IsSuccess = IsSuccess(info),
            ErrorCode = IdentifierOf(info.ErrorCode),
            ErrorDescription = info.ErrorDescription,
            RequestId = info.RequestId,
            Description = info.Description,
            Payment = info.Payment is { } payment ? Build(payment) : null,
            LastErrors = info.Errors?.Select(error => Build(error)).ToList() ?? [],
        };

    public static PscbPaymentDetailsResult DetailsFailure(string message)
        => new() { IsSuccess = false, ErrorDescription = message };

    public static PscbPaymentInfo Build(PaymentData payment)
        => new()
        {
            PaymentId = payment.Id,
            TransactionId = payment.TransactionId,
            OrderId = payment.OrderId,
            DisplayOrderId = payment.OrderIdDisplayValue,
            MarketPlace = payment.MarketPlace,
            Amount = payment.Amount,
            Fee = payment.Fee,
            Status = Identifier(payment.Status),
            SubStatus = IdentifierOf(payment.SubStatus),
            LastAction = payment.LastError is { } lastAction ? Build(lastAction) : null,
            Kind = IdentifierOf(payment.Type),
            PaymentMethod = Identifier(payment.PaymentMethod),
            StatusDate = payment.CurrentStatusDate,
            CustomerAccount = payment.CustomerAccount,
            CustomerComment = payment.CustomerComment,
            CustomerEmail = payment.CustomerEmail,
            CustomerPhone = payment.CustomerPhone,
            Details = payment.Details,
            MerchantData = CompactJson(payment.MerchantData),
            RecurrentPaymentToken = payment.RecurrentPaymentToken,
            RegistryId = payment.RegistryId,
            RegistryDate = payment.RegistryDate,
            Card = payment.Card is { } card ? Build(card) : null,
            Refunds = payment.Refunds?.Select(refund => Build(refund)).ToList() ?? [],
            Receipts = payment.Receipts?.Select(receipt => Build(receipt)).ToList() ?? [],
        };

    private static PscbBankCardInfo Build(BankCardData card)
        => new()
        {
            Bank = card.Bank,
            ProcessingStatus = card.ProcessingStatus,
            Country = card.Country,
            CountryCode = card.CountryCode,
            ExpirationDate = card.ExpirationDate,
            CardType = card.Type,
            MaskedNumber = card.MaskedName,
            ReferenceRetrievalNumber = card.ReferenceRetrievalNumber,
            CardholderEmail = card.CardholderEmail,
        };

    private static PscbRefundInfo Build(RefundData refund)
        => new()
        {
            RefundId = refund.Id,
            Status = Identifier(refund.Status),
            Amount = refund.Amount,
        };

    private static PscbReceiptInfo Build(ReceiptData receipt)
        => new()
        {
            ReceiptId = receipt.Id,
            Type = Identifier(receipt.Type),
            DocumentNumber = receipt.DocumentNumber,
            Date = receipt.Date,
            Amount = receipt.Amount,
            RegisterNumber = receipt.RegisterNumber,
            FiscalNumber = receipt.FiscalNumber,
            ShiftNumber = receipt.ShiftNumber,
            ReceiptNumber = receipt.ReceiptNumber,
        };

    private static PscbBankErrorInfo Build(BankResponseInfo error)
        => new()
        {
            Description = error.Description,
            PaymentsSystemCode = error.PaymentCode is { } paymentCode ? (int)paymentCode : null,
            ProcessingCenterCode = error.ProcessingCode is { } processingCode ? (int)processingCode : null,
        };

    /// <summary>
    /// Банк отвечает кодом HTTP 200 и на успех, и на отказ, поэтому статус смотрим в теле ответа.
    /// </summary>
    /// <remarks>
    /// При успехе банк ключ <c>status</c> не присылает вовсе — тело ответа начинается сразу с
    /// <c>payments</c> (проверено на боевом API 2026-10-21). Отсутствие статуса поэтому означает
    /// успех: <see cref="PaymentInfoQueryStatus.Success"/> — нулевое значение перечисления.
    /// </remarks>
    private static bool IsSuccess(PaymentInfoBase info) => info.Status == PaymentInfoQueryStatus.Success;

    private static string Identifier<TEnum>(TEnum value) where TEnum : struct, Enum => value.GetIdentifier();

    private static string? IdentifierOf<TEnum>(TEnum? value) where TEnum : struct, Enum => value?.GetIdentifier();

    private static string? CompactJson(JObject? merchantData)
        => merchantData?.ToString(Formatting.None);
}
