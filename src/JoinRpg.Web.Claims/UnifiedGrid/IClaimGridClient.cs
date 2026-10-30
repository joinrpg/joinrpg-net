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
/// <param name="Group">
/// Только персонажи этой группы и её подгрупп. Действует только в мастерском скоупе: селектор групп
/// капитану недоступен, а его группы задают правила капитана (ADR027 §1).
/// </param>
/// <param name="Responsible">
/// Только заявки, где этот мастер ответственный; персонажи без таких заявок пропадают. Действует
/// только в мастерском скоупе.
/// </param>
public record class UgFilterView(
    UgStatusFilterView Status,
    bool OnlyWithProblems = false,
    CharacterGroupIdentification? Group = null,
    UserIdentification? Responsible = null);

/// <summary>
/// Имена параметров query string грида (ADR027 §5) — те же, что у webapi <c>GetGrid</c>: страница и
/// API понимают один адрес, а редиректы строят его без таблицы соответствия.
/// </summary>
public static class UgQueryParameterNames
{
    public const string Status = "status";
    public const string Page = "page";
    public const string OnlyWithProblems = "onlyWithProblems";
    public const string CharacterGroupId = "characterGroupId";
    public const string ResponsibleMasterId = "responsibleMasterId";
}

/// <summary>
/// Страница грида. Страница считается по персонажам, чтобы заявки персонажа не рвались на её границе.
/// </summary>
/// <param name="Page">Номер отданной страницы с единицы — запрошенный, если он был в диапазоне.</param>
/// <param name="OperationClaimIds">
/// Все заявки результата фильтра, без пейджинга: по ним работают выгрузка и массовые операции
/// (ADR027 §5). У капитана пусто — ему они недоступны.
/// </param>
/// <param name="OperationCharacterIds">
/// Персонажи с показанными заявками (вакансий нет) — печать и бейджи. У капитана пусто.
/// </param>
public record class UgPageViewModel(
    IReadOnlyList<UgItemViewModel> Items,
    int Page,
    int TotalPages,
    int TotalCharacters,
    IReadOnlyCollection<ClaimIdentification> OperationClaimIds,
    IReadOnlyCollection<CharacterIdentification> OperationCharacterIds);
