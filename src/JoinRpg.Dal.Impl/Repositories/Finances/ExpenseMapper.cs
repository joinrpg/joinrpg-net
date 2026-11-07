namespace JoinRpg.Dal.Impl.Repositories;

/// <summary>
/// Чистое преобразование DataModel → доменные типы расходов (ADR030), паттерн
/// RoomCategoryPlanMapper (ADR018): вынесено из репозитория, чтобы покрываться
/// юнит-тестами без базы.
/// </summary>
internal static class ExpenseMapper
{
    public static ExpenseInfo Map(Expense expense, IReadOnlyDictionary<int, string> categoryNames)
        => new(
            expense.GetId(),
            expense.Name,
            expense.Description,
            [.. expense.CategoryPathIds.Select(id => new ExpenseCategoryBriefInfo(
                new ExpenseCategoryIdentification(new ProjectIdentification(expense.ProjectId), id),
                categoryNames[id]))],
            expense.PlannedQuantity,
            expense.PlannedPrice,
            expense.ActualQuantity,
            expense.ActualPrice,
            expense.SpentAt is { } spentAt ? DateOnly.FromDateTime(spentAt) : null,
            expense.State,
            new UserIdentification(expense.MasterId),
            new UserIdentification(expense.CreatedById));

    public static ExpenseCategoryInfo MapCategory(ExpenseCategory category)
        => new(
            category.GetId(),
            category.Name,
            category.ParentCategoryId is { } parentId
                ? new ExpenseCategoryIdentification(new ProjectIdentification(category.ProjectId), parentId)
                : null,
            category.IsActive);
}
