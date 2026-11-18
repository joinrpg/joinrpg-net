using JoinRpg.XGameApi.Contract.Pscb;

namespace JoinRpg.Portal.Helpers;

/// <summary>
/// Текст отказа банка для шлюза к API ПСКБ (ADR025).
/// </summary>
/// <remarks>
/// В JSON отказ банка отдаётся в теле ответа со статусом 200, и остров показывает его как ошибку.
/// В CSV ошибку положить некуда: файл с одним заголовком админ прочитал бы как «платежей за период
/// нет». Поэтому при <c>format=csv</c> контроллеры отвечают <c>Problem(...)</c> с этим текстом.
/// </remarks>
public static class PscbBankFailureHelper
{
    /// <summary>
    /// Заголовок ответа про отказ банка.
    /// </summary>
    public const string PaymentsListTitle = "Банк не вернул список платежей";

    /// <summary>
    /// Отказ банка одной строкой: код, его описание и идентификатор запроса, который называют
    /// в поддержку банка.
    /// </summary>
    public static string Describe(PscbPaymentsListResult result)
    {
        var description = string.Join(
            "; ",
            new[]
            {
                result.ErrorCode,
                result.ErrorDescription,
                result.RequestId is { } requestId ? $"requestId: {requestId}" : null,
            }.Where(part => !string.IsNullOrWhiteSpace(part)));

        return string.IsNullOrWhiteSpace(description) ? "Банк не сообщил причину" : description;
    }
}
