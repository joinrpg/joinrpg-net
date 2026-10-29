using JoinRpg.Web.AdminTools.PscbPayments;
using JoinRpg.XGameApi.Contract.Pscb;

namespace JoinRpg.Blazor.Client.ApiClients;

/// <summary>
/// WASM-клиент восстановления потерянных платежей ПСКБ (ADR027).
/// </summary>
/// <remarks>
/// Запрос — POST, а он закрыт глобальным <c>AutoValidateAntiforgeryTokenAttribute</c>, поэтому
/// перед вызовом токен antiforgery устанавливается — по editing-pages-guide. Читающий
/// <see cref="PscbPaymentsHttpClient"/> этого не делает именно потому, что он GET.
/// </remarks>
public class PscbPaymentsRestoreHttpClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider) : IPscbPaymentsRestoreClient
{
    /// <inheritdoc />
    public async Task<PscbRestoreRowResult> RestoreRow(PscbRestoreRowRequest request)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsync(PscbPaymentsRoutes.RestoreRow, JsonContent.Create(request));
        return await response.ReadFromJsonOrThrowProblem<PscbRestoreRowResult>();
    }
}
