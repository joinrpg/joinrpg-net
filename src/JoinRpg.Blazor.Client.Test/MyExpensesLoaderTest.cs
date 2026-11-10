using Bunit;
using JoinRpg.Blazor.Client.ApiClients;
using JoinRpg.Blazor.Client.Expenses;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes.Finances;
using JoinRpg.Web.Finances.Expenses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace JoinRpg.Blazor.Client.Test;

public class MyExpensesLoaderTest : BunitContext
{
    private static readonly ProjectIdentification ProjectId = new(1);

    public MyExpensesLoaderTest()
    {
        Services.AddSingleton<IExpensesClient>(new FakeExpensesClient(Expenses));
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void TotalsExcludeRejectedAndApprovedCountsPlannedCost()
    {
        var cut = Render<MyExpensesLoader>(p => p.Add(x => x.ProjectId, ProjectId));

        // 2×1500 + 10×100 = 4000; Rejected (5×1000) не учитывается ни в одной сумме.
        cut.Markup.ShouldContain("Запланировано:");
        cut.Markup.ShouldContain("<b>4000 ₽</b>");
        cut.Markup.ShouldContain("Одобрено:");
        cut.Markup.ShouldContain("<b>3000 ₽</b>");
    }

    [Fact]
    public void MasterColumnHidden()
    {
        var cut = Render<MyExpensesLoader>(p => p.Add(x => x.ProjectId, ProjectId));

        cut.FindAll("th").ShouldNotContain(header => header.TextContent.Trim() == "Мастер");
        cut.Markup.ShouldNotContain("Иван Мастеров");
        cut.Markup.ShouldContain("Аренда зала");
    }

    private static readonly ExpenseViewModel[] Expenses =
    [
        new(
            new ExpenseIdentification(ProjectId, 1),
            "Аренда зала",
            "Аренда базы\\Жилые корпуса",
            PlannedQuantity: 2,
            PlannedPrice: 1500,
            ActualQuantity: null,
            ActualPrice: null,
            SpentAt: null,
            State: ExpenseStateView.Approved,
            MasterName: "Иван Мастеров",
            CreatedByName: "Иван Мастеров"),
        new(
            new ExpenseIdentification(ProjectId, 2),
            "Канцелярия",
            "Оргвзнос\\Расходники",
            PlannedQuantity: 10,
            PlannedPrice: 100,
            ActualQuantity: null,
            ActualPrice: null,
            SpentAt: null,
            State: ExpenseStateView.Proposed,
            MasterName: "Иван Мастеров",
            CreatedByName: "Иван Мастеров"),
        new(
            new ExpenseIdentification(ProjectId, 3),
            "Лишний реквизит",
            "Реквизит",
            PlannedQuantity: 5,
            PlannedPrice: 1000,
            ActualQuantity: null,
            ActualPrice: null,
            SpentAt: null,
            State: ExpenseStateView.Rejected,
            MasterName: "Иван Мастеров",
            CreatedByName: "Иван Мастеров"),
    ];

    private sealed class FakeExpensesClient(ExpenseViewModel[] myExpenses) : IExpensesClient
    {
        public Task<IReadOnlyList<ExpenseViewModel>> GetMyExpenses(ProjectIdentification projectId)
            => Task.FromResult<IReadOnlyList<ExpenseViewModel>>(myExpenses);

        public Task<IReadOnlyList<ExpenseViewModel>> GetExpenses(ProjectIdentification projectId)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<ExpenseCategoryInfo>> GetCategories(ProjectIdentification projectId)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<string>> GetCategoryPathSuggestions(ProjectIdentification projectId)
            => throw new NotSupportedException();

        public Task<UserIdentification> GetCurrentUser(ProjectIdentification projectId)
            => throw new NotSupportedException();

        public Task<bool> CanManageMoney(ProjectIdentification projectId) => throw new NotSupportedException();

        public Task<ExpenseIdentification> CreateExpense(ProjectIdentification projectId, ExpenseEditViewModel model)
            => throw new NotSupportedException();

        public Task EditExpense(ExpenseIdentification expenseId, ExpenseEditViewModel model)
            => throw new NotSupportedException();

        public Task SetExpenseActual(ExpenseIdentification expenseId, ExpenseActualDto model)
            => throw new NotSupportedException();

        public Task ApproveExpense(ExpenseIdentification expenseId, string? comment)
            => throw new NotSupportedException();

        public Task RejectExpense(ExpenseIdentification expenseId, string? comment)
            => throw new NotSupportedException();
    }
}
