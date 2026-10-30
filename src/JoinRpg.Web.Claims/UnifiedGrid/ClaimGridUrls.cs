namespace JoinRpg.Web.Claims.UnifiedGrid;

/// <summary>
/// Адрес мастерского грида заявок <c>/{projectId}/claims</c> (ADR027 §5). Его строят локаторы
/// (серверный и Blazor-клиентский) и редиректы со старых списков заявок (§6), поэтому он один —
/// иначе адреса разъедутся. Значения по умолчанию — статус «Активные», первая страница, без
/// фильтров — в адрес не пишутся, как это делает сам грид при смене фильтра.
/// </summary>
/// <remarks>
/// Формат значений фиксирован: 301-редиректы кешируются браузером (§6), поэтому адрес и имена
/// параметров (<see cref="UgQueryParameterNames"/>) менять нельзя, только добавлять новые.
/// </remarks>
public static class ClaimGridUrls
{
    public static string Build(
        ProjectIdentification projectId,
        UgStatusFilterView? status = null,
        UserIdentification? responsibleMasterId = null,
        CharacterGroupIdentification? characterGroupId = null,
        bool onlyWithProblems = false,
        int? page = null,
        string? export = null)
    {
        var query = new List<string>();
        if (status is not null && status != UgStatusFilterView.Active)
        {
            query.Add($"{UgQueryParameterNames.Status}={status}");
        }
        if (page > 1)
        {
            query.Add($"{UgQueryParameterNames.Page}={page}");
        }
        if (onlyWithProblems)
        {
            query.Add($"{UgQueryParameterNames.OnlyWithProblems}=true");
        }
        if (characterGroupId is not null)
        {
            query.Add($"{UgQueryParameterNames.CharacterGroupId}={Uri.EscapeDataString(characterGroupId.ToString())}");
        }
        if (responsibleMasterId is not null)
        {
            query.Add($"{UgQueryParameterNames.ResponsibleMasterId}={Uri.EscapeDataString(responsibleMasterId.ToString())}");
        }
        if (!string.IsNullOrEmpty(export))
        {
            query.Add($"{UgQueryParameterNames.Export}={Uri.EscapeDataString(export)}");
        }

        return query.Count == 0
            ? $"/{projectId.Value}/claims"
            : $"/{projectId.Value}/claims?{string.Join("&", query)}";
    }
}
