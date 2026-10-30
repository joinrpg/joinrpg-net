namespace JoinRpg.Web.AdminTools.PscbPayments;

/// <summary>
/// Запрос восстановления одной потерянной строки списка банка (ADR028).
/// </summary>
/// <remarks>
/// Сервер заново скачивает период <see cref="Query"/> и восстанавливает только строку
/// с <see cref="OrderId"/>, беря суммы и даты у банка, а не из этого объекта: клиентскому JSON
/// сервер не доверяет никогда.
/// </remarks>
/// <param name="Query">Параметры периода — те же, которыми остров загрузил список.</param>
/// <param name="OrderId">Номер платежа на нашей стороне — <c>orderId</c> строки списка банка.</param>
public record PscbRestoreRowRequest(PscbPaymentsQuery Query, string OrderId);
