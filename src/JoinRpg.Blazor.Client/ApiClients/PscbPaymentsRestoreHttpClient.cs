using JoinRpg.Web.AdminTools.PscbPayments;
using JoinRpg.XGameApi.Contract.Pscb;

namespace JoinRpg.Blazor.Client.ApiClients;

/// <summary>
/// WASM-клиент восстановления потерянных платежей ПСКБ (ADR026).
/// </summary>
/// <remarks>
/// Запрос — POST, а он закрыт глобальным <c>AutoValidateAntiforgeryTokenAttribute</c>, поэтому
/// перед вызовом токен antiforgery устанавливается — по editing-pages-guide. Читающий
/// <see cref="PscbPaymentsHttpClient"/> этого не делает именно потому, что он GET.
/// </remarks>
public class PscbPaymentsRestoreHttpClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider) : IPscbPaymentsRestoreClient
{
    /// <inheritdoc />
    public async Task<PscbRestoreResult> Restore(PscbPaymentsQuery query)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsync(PscbPaymentsRoutes.Restore, JsonContent.Create(query));
        return await response.ReadFromJsonOrThrowProblem<PscbRestoreResult>();
    }
}
