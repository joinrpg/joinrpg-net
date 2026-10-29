namespace JoinRpg.Web.Claims.UnifiedGrid;

public interface IUnifiedGridClient
{
    /// <summary>
    /// Строки объединённого грида (ADR027).
    /// </summary>
    /// <param name="scope">
    /// Какой грид просит страница. Права проверяет сервер: мастерский скоуп без доступа мастера и
    /// капитанский без правил капитана дают пустой грид.
    /// </param>
    Task<IReadOnlyCollection<UgItemViewModel>> GetGrid(ProjectIdentification projectId, UgScopeView scope, UgFilterView filter);
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
public record class UgFilterView(UgStatusFilterView Status);
