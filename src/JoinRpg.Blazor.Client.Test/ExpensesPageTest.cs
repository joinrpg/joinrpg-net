using Bunit;
using JoinRpg.Blazor.Client.ApiClients;
using JoinRpg.Blazor.Client.Expenses;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes.Finances;
using JoinRpg.Web.Finances.Expenses;
using JoinRpg.Web.ProjectCommon;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace JoinRpg.Blazor.Client.Test;

public class ExpensesPageTest : BunitContext
{
    private static readonly ProjectIdentification ProjectId = new(1);

    private static readonly ExpenseViewModel Expense = new(
        new ExpenseIdentification(ProjectId, 1),
        "Аренда зала",
        "Аренда базы\\Жилые корпуса",
        PlannedQuantity: 2,
        PlannedPrice: 1500,
        ActualQuantity: null,
        ActualPrice: null,
        SpentAt: null,
        State: ExpenseStateView.Proposed,
        MasterName: "Иван Мастеров",
        CreatedByName: "Иван Мастеров");

    private readonly FakeExpensesClient expensesClient = new([Expense]);

    public ExpensesPageTest()
    {
        Services.AddSingleton<IExpensesClient>(expensesClient);
        Services.AddSingleton<IMasterClient>(new FakeMasterClient([]));
        Services.AddSingleton<IUriLocator<UserLinkViewModel>>(new FakeUserLinkLocator());
        _ = AddBunitPersistentComponentState();
        Services.AddScoped<SelectorListCache>();
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
    }

    [Fact]
    public void NewExpenseFormDefaultsSpentAtToToday()
    {
        var cut = Render<ExpensesPage>(p => p.Add(x => x.ProjectId, ProjectId));

        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Новый расход").Click();

        var today = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        cut.Find("input[name=SpentAt]").GetAttribute("value").ShouldBe(today);
    }

    [Fact]
    public void EditButtonDoesNotStuckInProgressAfterCancel()
    {
        var cut = Render<ExpensesPage>(p => p.Add(x => x.ProjectId, ProjectId));

        ClickEditButton(cut);
        cut.Markup.ShouldContain("Редактирование расхода");

        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Отменить").Click();
        cut.Markup.ShouldNotContain("Редактирование расхода");

        // Регрессия: после отмены кнопка «Править» зависала в состоянии прогресса
        // (JoinButton.AutoProgress) и переставала реагировать на клики.
        var editButton = FindEditButton(cut);
        editButton.HasAttribute("disabled").ShouldBeFalse();
        editButton.InnerHtml.ShouldNotContain("#hourglass"); // иконка прогресса

        ClickEditButton(cut);
        cut.Markup.ShouldContain("Редактирование расхода");
    }

    [Fact]
    public void RowWithoutAvailableActionsRendersNoButtonGroup()
    {
        // Расход не мой и нет права на деньги — ни одной кнопки действия,
        // пустая JoinButtonGroup не должна оставлять пустой div в строке.
        expensesClient.MyExpenses = [];
        var cut = Render<ExpensesPage>(p => p.Add(x => x.ProjectId, ProjectId));

        cut.FindAll("div[role=group]").ShouldBeEmpty();
    }

    private static void ClickEditButton(IRenderedComponent<ExpensesPage> cut)
        => FindEditButton(cut).Click();

    private static AngleSharp.Dom.IElement FindEditButton(IRenderedComponent<ExpensesPage> cut)
        => cut.FindAll("button").Single(button => button.TextContent.Trim() == "Править");

    private sealed class FakeExpensesClient(ExpenseViewModel[] expenses) : IExpensesClient
    {
        /// <summary>Расходы текущего пользователя; null — совпадают с <c>expenses</c>.</summary>
        public ExpenseViewModel[]? MyExpenses { get; set; }

        public Task<IReadOnlyList<ExpenseViewModel>> GetExpenses(ProjectIdentification projectId)
            => Task.FromResult<IReadOnlyList<ExpenseViewModel>>(expenses);

        public Task<IReadOnlyList<ExpenseViewModel>> GetMyExpenses(ProjectIdentification projectId)
            => Task.FromResult<IReadOnlyList<ExpenseViewModel>>(MyExpenses ?? expenses);

        public Task<IReadOnlyCollection<ExpenseCategoryInfo>> GetCategories(ProjectIdentification projectId)
            => Task.FromResult<IReadOnlyCollection<ExpenseCategoryInfo>>([]);

        public Task<bool> CanManageMoney(ProjectIdentification projectId) => Task.FromResult(false);

        public Task<IReadOnlyCollection<string>> GetCategoryPathSuggestions(ProjectIdentification projectId)
            => Task.FromResult<IReadOnlyCollection<string>>([]);

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

    private sealed class FakeMasterClient(UserInfoHeader[] masters) : IMasterClient
    {
        public Task<List<UserInfoHeader>> GetMasters(int projectId) => Task.FromResult(masters.ToList());
    }

    private sealed class FakeUserLinkLocator : IUriLocator<UserLinkViewModel>
    {
        public Uri GetUri(UserLinkViewModel target) => new($"https://example.org/user/{target.UserId}");
    }
}
