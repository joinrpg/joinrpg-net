using JoinRpg.DomainTypes.Finances;
using JoinRpg.Web.Finances.Expenses;

namespace JoinRpg.Blazor.Client.ApiClients;

public class ExpensesClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider) : IExpensesClient
{
    public async Task<IReadOnlyList<ExpenseViewModel>> GetExpenses(ProjectIdentification projectId)
        => await (await httpClient.GetAsync($"webapi/{projectId.Value}/expenses/list"))
            .ReadFromJsonOrThrowProblem<IReadOnlyList<ExpenseViewModel>>();

    public async Task<IReadOnlyList<ExpenseViewModel>> GetMyExpenses(ProjectIdentification projectId)
        => await (await httpClient.GetAsync($"webapi/{projectId.Value}/expenses/my"))
            .ReadFromJsonOrThrowProblem<IReadOnlyList<ExpenseViewModel>>();

    public async Task<IReadOnlyCollection<ExpenseCategoryInfo>> GetCategories(ProjectIdentification projectId)
        => await (await httpClient.GetAsync($"webapi/{projectId.Value}/expense-categories"))
            .ReadFromJsonOrThrowProblem<IReadOnlyCollection<ExpenseCategoryInfo>>();

    public async Task<bool> CanManageMoney(ProjectIdentification projectId)
        => await (await httpClient.GetAsync($"webapi/{projectId.Value}/expenses/canmanagemoney"))
            .ReadFromJsonOrThrowProblem<bool>();

    public async Task<IReadOnlyCollection<string>> GetCategoryPathSuggestions(ProjectIdentification projectId)
        => await (await httpClient.GetAsync($"webapi/{projectId.Value}/expenses/categorypathsuggestions"))
            .ReadFromJsonOrThrowProblem<IReadOnlyCollection<string>>();

    public async Task<UserIdentification> GetCurrentUser(ProjectIdentification projectId)
        => await (await httpClient.GetAsync($"webapi/{projectId.Value}/expenses/currentuser"))
            .ReadFromJsonOrThrowProblem<UserIdentification>();

    public async Task<ExpenseIdentification> CreateExpense(ProjectIdentification projectId, ExpenseEditViewModel model)
        => await Post(
            $"webapi/{projectId.Value}/expenses/create",
            model,
            response => response.ReadFromJsonOrThrowProblem<ExpenseIdentification>());

    public async Task EditExpense(ExpenseIdentification expenseId, ExpenseEditViewModel model)
        => await Post($"webapi/{expenseId.ProjectId.Value}/expenses/edit?expenseId={expenseId.ExpenseId}", model, EnsureSuccess);

    public async Task SetExpenseActual(ExpenseIdentification expenseId, ExpenseActualDto model)
        => await Post($"webapi/{expenseId.ProjectId.Value}/expenses/setactual?expenseId={expenseId.ExpenseId}", model, EnsureSuccess);

    public async Task ApproveExpense(ExpenseIdentification expenseId, string? comment)
        => await Post($"webapi/{expenseId.ProjectId.Value}/expenses/approve?expenseId={expenseId.ExpenseId}", new { Comment = comment }, EnsureSuccess);

    public async Task RejectExpense(ExpenseIdentification expenseId, string? comment)
        => await Post($"webapi/{expenseId.ProjectId.Value}/expenses/reject?expenseId={expenseId.ExpenseId}", new { Comment = comment }, EnsureSuccess);

    private static async Task<bool> EnsureSuccess(HttpResponseMessage response)
    {
        await response.EnsureSuccessOrThrowProblem();
        return true;
    }

    private async Task<TResult> Post<TResult>(string url, object? body, Func<HttpResponseMessage, Task<TResult>> read)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsJsonAsync(url, body);
        return await read(response);
    }
}
