using JoinRpg.Web.AdminTools.PscbPayments;
using JoinRpg.XGameApi.Contract.Pscb;

namespace JoinRpg.Blazor.Client.ApiClients;

/// <summary>
/// WASM-клиент шлюза к API ПСБ (ADR025).
/// </summary>
/// <remarks>
/// Оба запроса — GET, поэтому токен antiforgery не нужен: глобальная проверка его касается только
/// запросов с телом. Ссылку на CSV остров строит сам из тех же параметров
/// (<see cref="PscbPaymentsRoutes.GetPaymentsUri"/>), отдельный запрос за ней не ходит.
/// </remarks>
public class PscbPaymentsHttpClient(HttpClient httpClient) : IPscbPaymentsClient
{
    /// <inheritdoc />
    public async Task<PscbPaymentsListResult> GetPayments(PscbPaymentsQuery query)
    {
        var response = await httpClient.GetAsync(PscbPaymentsRoutes.GetPaymentsUri(query));
        await response.EnsureSuccessOrThrowProblem();
        return await response.Content.ReadFromJsonAsync<PscbPaymentsListResult>()
            ?? throw new ServerProblemException("Не удалось получить список платежей с сервера");
    }

    /// <inheritdoc />
    public async Task<PscbPaymentDetailsResult> GetPaymentInfo(PscbPaymentDetailsQuery query)
    {
        var response = await httpClient.GetAsync(PscbPaymentsRoutes.GetPaymentInfoUri(query));
        await response.EnsureSuccessOrThrowProblem();
        return await response.Content.ReadFromJsonAsync<PscbPaymentDetailsResult>()
            ?? throw new ServerProblemException("Не удалось получить параметры платежа с сервера");
    }
}
