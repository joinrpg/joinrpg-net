namespace JoinRpg.Web.Claims.UnifiedGrid;

/// <summary>
/// Строит адреса мастерского грида заявок (ADR027 §5–§6): на них ведут меню, шапка группы и
/// страница мастеров. Реализован и на сервере, и в Blazor-клиенте; согласованность реализаций
/// проверяет <c>UriLocatorConsistencyTests</c>.
/// </summary>
public interface IClaimsGridUriLocator
{
    /// <summary>
    /// Адрес грида с фильтром. Значения по умолчанию (статус «Активные», первая страница, без
    /// фильтров) в адрес не пишутся — как у самого грида.
    /// </summary>
    Uri GetGridUri(
        ProjectIdentification projectId,
        UgStatusFilterView? status = null,
        UserIdentification? responsibleMasterId = null,
        CharacterGroupIdentification? characterGroupId = null,
        bool onlyWithProblems = false,
        int? page = null);
}
