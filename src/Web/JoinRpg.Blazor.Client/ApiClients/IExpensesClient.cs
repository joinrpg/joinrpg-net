using JoinRpg.DomainTypes.Finances;
using JoinRpg.Web.Finances.Expenses;

namespace JoinRpg.Blazor.Client.ApiClients;

/// <summary>
/// Расходы проекта (ADR030): список, «мои расходы», создание/правка, факт траты,
/// согласование. Права на конкретное действие проверяет сервер.
/// </summary>
public interface IExpensesClient
{
    Task<IReadOnlyList<ExpenseViewModel>> GetExpenses(ProjectIdentification projectId);

    Task<IReadOnlyList<ExpenseViewModel>> GetMyExpenses(ProjectIdentification projectId);

    Task<IReadOnlyCollection<ExpenseCategoryInfo>> GetCategories(ProjectIdentification projectId);

    /// <summary>Может ли текущий мастер одобрять и отклонять расходы — для кнопок на странице.</summary>
    Task<bool> CanManageMoney(ProjectIdentification projectId);

    /// <summary>Полные пути категорий для подсказок поля «Категория»: категории с не отклонёнными расходами и их предки.</summary>
    Task<IReadOnlyCollection<string>> GetCategoryPathSuggestions(ProjectIdentification projectId);

    /// <summary>Текущий пользователь — мастер по умолчанию в форме нового расхода.</summary>
    Task<UserIdentification> GetCurrentUser(ProjectIdentification projectId);

    Task<ExpenseIdentification> CreateExpense(ProjectIdentification projectId, ExpenseEditViewModel model);

    Task EditExpense(ExpenseIdentification expenseId, ExpenseEditViewModel model);

    Task SetExpenseActual(ExpenseIdentification expenseId, ExpenseActualDto model);

    Task ApproveExpense(ExpenseIdentification expenseId, string? comment);

    Task RejectExpense(ExpenseIdentification expenseId, string? comment);
}

/// <summary>Фактическая трата по расходу; имена полей совпадают с сервисным <c>ExpenseActualRequest</c>.</summary>
public sealed record ExpenseActualDto(int ActualQuantity, decimal ActualPrice, DateOnly SpentAt);
