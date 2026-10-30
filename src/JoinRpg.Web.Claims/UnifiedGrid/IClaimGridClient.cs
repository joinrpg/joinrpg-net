namespace JoinRpg.Web.Claims.UnifiedGrid;

public interface IUnifiedGridClient
{
    /// <summary>
    /// Страница объединённого грида (ADR027).
    /// </summary>
    /// <param name="scope">
    /// Какой грид просит страница. Права проверяет сервер: мастерский скоуп без доступа мастера и
    /// капитанский без правил капитана дают пустой грид.
    /// </param>
    /// <param name="page">Номер страницы с единицы; за пределами диапазона — ближайшая существующая.</param>
    Task<UgPageViewModel> GetGrid(ProjectIdentification projectId, UgScopeView scope, UgFilterView filter, int page);
}

/// <summary>
/// Скоуп грида: мастер видит весь проект, капитан — поддеревья групп из своих правил.
/// </summary>
/// <remarks>
/// Выбирает его страница, а не права: мастер, который ещё и капитан, в кабинете капитана видит
/// свою команду, а не весь проект.
/// </remarks>
public enum UgScopeView
{
    Captain,
    Master,
}

/// <summary>
/// Фильтр грида. Условия объединяются через «И».
/// </summary>
/// <param name="OnlyWithProblems">
/// Только заявки с проблемами от <see cref="ProblemSeverity.Warning"/> и выше. Действует только в
/// мастерском скоупе: капитан проблем не видит (ADR027 §1).
/// </param>
public record class UgFilterView(UgStatusFilterView Status, bool OnlyWithProblems = false);

/// <summary>
/// Страница грида. Страница считается по персонажам, чтобы заявки персонажа не рвались на её границе.
/// </summary>
/// <param name="Page">Номер отданной страницы с единицы — запрошенный, если он был в диапазоне.</param>
public record class UgPageViewModel(
    IReadOnlyList<UgItemViewModel> Items,
    int Page,
    int TotalPages,
    int TotalCharacters);
