namespace JoinRpg.Services.Interfaces;

/// <summary>
/// CRUD и согласование расходов проекта (ADR030). Создавать может любой мастер; править — любой
/// мастер свои расходы или созданные им; одобрять/отклонять — только с <see cref="Permission.CanManageMoney"/>.
/// </summary>
public interface IExpenseService
{
    Task<ExpenseIdentification> CreateExpense(ProjectIdentification projectId, CreateExpenseRequest request);

    Task EditExpense(ExpenseIdentification expenseId, EditExpenseRequest request);

    Task SetExpenseActual(ExpenseIdentification expenseId, ExpenseActualRequest request);

    Task ApproveExpense(ExpenseIdentification expenseId, MarkdownString? comment);

    Task RejectExpense(ExpenseIdentification expenseId, MarkdownString? comment);
}
