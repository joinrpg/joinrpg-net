namespace JoinRpg.DomainTypes.Finances;

/// <summary>
/// Состояние согласования расхода (ADR030).
/// </summary>
public enum ExpenseState
{
    Proposed,
    Approved,
    Rejected,
}

/// <summary>Сегмент снапшота пути категории расхода (от корня к листу).</summary>
public record ExpenseCategoryBriefInfo(
    ExpenseCategoryIdentification CategoryId,
    string Name);

/// <summary>
/// Категория расхода проекта (ADR030) — узел дерева. Плоский список отдаётся UI для
/// автодополнения поля пути и фильтра грида; пути строит клиент.
/// </summary>
/// <param name="HasActiveDirectExpenses">Есть ли у категории хотя бы один привязанный
/// расход не в состоянии Rejected — по этому флагу сервис строит подсказки путей.</param>
/// <param name="FullPath">Полный путь категории от корня через «\» (например
/// «Аренда базы\Жилые корпуса») — тот же формат, что вводится в поле «Категория».</param>
public record ExpenseCategoryInfo(
    ExpenseCategoryIdentification CategoryId,
    string Name,
    ExpenseCategoryIdentification? ParentCategoryId,
    bool IsActive,
    bool HasActiveDirectExpenses,
    string FullPath);

/// <summary>
/// Расход проекта (ADR030): план (количество × цена) и, позже, факт (количество × цена).
/// Description — markdown. Доменный тип данных — сущность DataModel за пределы Dal не прокидывается.
/// </summary>
public record ExpenseInfo(
    ExpenseIdentification ExpenseId,
    string Name,
    MarkdownString? Description,
    IReadOnlyCollection<ExpenseCategoryBriefInfo> CategoryPath,
    int PlannedQuantity,
    decimal PlannedPrice,
    int? ActualQuantity,
    decimal? ActualPrice,
    DateOnly? SpentAt,
    ExpenseState State,
    UserIdentification MasterId,
    UserIdentification CreatedById)
{
    public decimal PlannedCost => ExpenseCost.Planned(PlannedQuantity, PlannedPrice);

    public decimal? ActualCost => ExpenseCost.Actual(ActualQuantity, ActualPrice);

    /// <summary>
    /// Влияние расхода на «Деньги на руках» мастера: фактическая сумма, если расход одобрен,
    /// иначе 0 (в том числе одобренный без факта и отклонённый).
    /// </summary>
    public decimal ActualBalanceEffect => State switch
    {
        ExpenseState.Approved => ActualCost ?? 0,
        _ => 0,
    };

    /// <summary>
    /// Влияние расхода на колонку «Запланировано» баланса мастера: отклонённые — 0,
    /// одобренные с фактом (уже учтены в <see cref="ActualBalanceEffect"/>) — 0,
    /// остальные (одобренные без факта, на рассмотрении) — факт, если он есть, иначе план.
    /// </summary>
    public decimal PlannedBalanceEffect => State switch
    {
        ExpenseState.Rejected => 0,
        ExpenseState.Approved when ActualCost is not null => 0,
        _ => ActualCost ?? PlannedCost,
    };
}
