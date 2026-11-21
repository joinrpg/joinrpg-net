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

public class AddExpenseWidgetTest : BunitContext
{
    private static readonly ProjectIdentification ProjectId = new(1);

    private static readonly UserInfoHeader[] Masters =
    [
        new(new UserIdentification(42), new UserDisplayName("Иван Мастеров", null), null),
    ];

    private readonly FakeExpensesClient expensesClient = new();
    private readonly RecordingNavigationManager navigationManager = new();

    public AddExpenseWidgetTest()
    {
        Services.AddSingleton<IExpensesClient>(expensesClient);
        Services.AddSingleton<IMasterClient>(new FakeMasterClient(Masters));
        Services.AddSingleton<IUriLocator<UserLinkViewModel>>(new FakeUserLinkLocator());
        Services.AddSingleton<NavigationManager>(navigationManager);
        _ = AddBunitPersistentComponentState();
        Services.AddScoped<SelectorListCache>();
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
    }

    [Fact]
    public void NewExpenseDialogDefaultsMasterToCurrentUserAndDateToToday()
    {
        expensesClient.CurrentUser = new UserIdentification(42);
        var cut = Render<AddExpenseWidget>(p => p.Add(x => x.ProjectId, ProjectId));

        ClickButton(cut, "Новый расход");

        DialogCaption(cut).ShouldBe("Новый расход");
        var today = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        cut.Find("dialog input[name=SpentAt]").GetAttribute("value").ShouldBe(today);
        cut.Find("dialog details.join-select summary .join-select__value").TextContent.Trim().ShouldBe("Иван Мастеров");
    }

    [Fact]
    public async Task CancelWithoutSaveDoesNotReload()
    {
        var cut = Render<AddExpenseWidget>(p => p.Add(x => x.ProjectId, ProjectId));

        ClickButton(cut, "Новый расход");
        await CancelDialogAsync(cut);

        navigationManager.ReloadRequested.ShouldBeFalse();
        expensesClient.Created.ShouldBeNull();
    }

    [Fact]
    public async Task SaveCreatesExpenseAndReloadsPage()
    {
        expensesClient.CurrentUser = new UserIdentification(42);
        var cut = Render<AddExpenseWidget>(p => p.Add(x => x.ProjectId, ProjectId));

        ClickButton(cut, "Новый расход");
        FillRequiredFields(cut);

        await SubmitDialogAsync(cut);

        cut.WaitForAssertion(() =>
        {
            expensesClient.Created.ShouldNotBeNull();
            expensesClient.Created.Name.ShouldBe("Аренда зала");
            expensesClient.Created.CategoryPath.ShouldBe("Аренда базы\\Жилые корпуса");
            expensesClient.Created.MasterId.ShouldBe(new UserIdentification(42));
            navigationManager.ReloadRequested.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task SaveFailureShowsErrorAndKeepsData()
    {
        var cut = Render<AddExpenseWidget>(p => p.Add(x => x.ProjectId, ProjectId));

        ClickButton(cut, "Новый расход");
        FillRequiredFields(cut);

        expensesClient.CreateFailure = new ServerProblemException("Нет прав на деньги");
        await SubmitDialogAsync(cut);

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Ошибка: Нет прав на деньги"));
        navigationManager.ReloadRequested.ShouldBeFalse();

        // Регрессия: введённые данные не должны пропадать — тот же диалог принимает повторный сабмит
        expensesClient.CreateFailure = null;
        await SubmitDialogAsync(cut);

        cut.WaitForAssertion(() =>
        {
            expensesClient.Created.ShouldNotBeNull();
            expensesClient.Created.Name.ShouldBe("Аренда зала");
        });
    }

    [Fact]
    public async Task AddAnotherReopensPrefilledAndCancelAfterSaveReloads()
    {
        expensesClient.CurrentUser = new UserIdentification(42);
        var cut = Render<AddExpenseWidget>(p => p.Add(x => x.ProjectId, ProjectId));

        ClickButton(cut, "Новый расход");
        FillRequiredFields(cut);
        cut.Find("dialog input[type=date]")!.Change("2026-11-01");
        cut.Find("dialog input[type=checkbox]").Change(true); // «Продолжить добавлять расходы»

        await SubmitDialogAsync(cut);

        cut.WaitForAssertion(() => expensesClient.Created.ShouldNotBeNull());
        navigationManager.ReloadRequested.ShouldBeFalse();

        // Диалог открылся заново: название и суммы пустые, мастер/категория/дата скопированы
        cut.WaitForAssertion(() =>
        {
            var reopened = cut.Find("dialog");
            reopened.QuerySelectorAll("input[type=text]")[0].GetAttribute("value").ShouldBeNullOrEmpty(); // название
            reopened.QuerySelectorAll("input[type=text]")[1].GetAttribute("value").ShouldBe("Аренда базы\\Жилые корпуса"); // категория
            reopened.QuerySelector("input[type=date]")!.GetAttribute("value").ShouldBe("2026-11-01");
            foreach (var numberInput in reopened.QuerySelectorAll("input[type=number]"))
            {
                numberInput.GetAttribute("value").ShouldBeNullOrEmpty();
            }
            reopened.QuerySelector("details.join-select summary .join-select__value")!.TextContent.Trim().ShouldBe("Иван Мастеров");
            reopened.QuerySelector("input[type=checkbox]")!.HasAttribute("checked").ShouldBeTrue();
        });

        await CancelDialogAsync(cut);

        // За сессию расход уже сохранён — страница перезагружается, статические таблицы обновятся
        navigationManager.ReloadRequested.ShouldBeTrue();
    }

    private static void FillRequiredFields(IRenderedComponent<AddExpenseWidget> cut)
    {
        var dialog = cut.Find("dialog");
        dialog.QuerySelectorAll("input[type=text]")[0].Change("Аренда зала");
        dialog.QuerySelectorAll("input[type=text]")[1].Change("Аренда базы\\Жилые корпуса");
        SelectMaster(cut, "Иван Мастеров");
        dialog.QuerySelectorAll("input[type=number]")[2].Change("2"); // факт количество
        dialog.QuerySelectorAll("input[type=number]")[3].Change("1400"); // факт цена
    }

    private static string DialogCaption(IRenderedComponent<AddExpenseWidget> cut)
        => cut.Find(".join-dialog-caption").TextContent.Trim();

    private static void ClickButton(IRenderedComponent<AddExpenseWidget> cut, string label)
        => cut.FindAll("button").Single(button => button.TextContent.Trim() == label).Click();

    private static async Task SubmitDialogAsync(IRenderedComponent<AddExpenseWidget> cut)
    {
        var footerButtons = cut.FindAll(".join-dialog-footer button");
        await footerButtons[0].ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await cut.Find("dialog").TriggerEventAsync("onclose", EventArgs.Empty);
    }

    private static async Task CancelDialogAsync(IRenderedComponent<AddExpenseWidget> cut)
    {
        var footerButtons = cut.FindAll(".join-dialog-footer button");
        await footerButtons[1].ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await cut.Find("dialog").TriggerEventAsync("onclose", EventArgs.Empty);
    }

    private static void SelectMaster(IRenderedComponent<AddExpenseWidget> cut, string displayName)
    {
        var item = cut.FindAll("dialog details.join-select .join-select__item")
            .Single(i => i.QuerySelector(".join-select__text")!.TextContent.Trim() == displayName);
        item.QuerySelector("input")!.Change(true);
    }

    private sealed class RecordingNavigationManager : NavigationManager
    {
        /// <summary>Виджет запросил полную перезагрузку страницы (forceLoad).</summary>
        public bool ReloadRequested { get; private set; }

        public RecordingNavigationManager() =>
            Initialize("https://localhost/", "https://localhost/183/money/moneysummary");

        protected override void NavigateToCore(string uri, NavigationOptions options)
            => ReloadRequested = options.ForceLoad;
    }

    private sealed class FakeExpensesClient : IExpensesClient
    {
        /// <summary>Текущий пользователь — предзаполнение мастера в форме создания.</summary>
        public UserIdentification CurrentUser { get; set; } = new(1);

        /// <summary>Модель последнего вызова <c>CreateExpense</c>.</summary>
        public ExpenseEditViewModel? Created { get; private set; }

        /// <summary>Исключение, которое бросит <c>CreateExpense</c>; null — сохранение успешно.</summary>
        public ServerProblemException? CreateFailure { get; set; }

        public Task<IReadOnlyList<ExpenseViewModel>> GetExpenses(ProjectIdentification projectId)
            => Task.FromResult<IReadOnlyList<ExpenseViewModel>>([]);

        public Task<IReadOnlyList<ExpenseViewModel>> GetMyExpenses(ProjectIdentification projectId)
            => Task.FromResult<IReadOnlyList<ExpenseViewModel>>([]);

        public Task<IReadOnlyCollection<ExpenseCategoryInfo>> GetCategories(ProjectIdentification projectId)
            => Task.FromResult<IReadOnlyCollection<ExpenseCategoryInfo>>([]);

        public Task<bool> CanManageMoney(ProjectIdentification projectId) => Task.FromResult(false);

        public Task<IReadOnlyCollection<string>> GetCategoryPathSuggestions(ProjectIdentification projectId)
            => Task.FromResult<IReadOnlyCollection<string>>([]);

        public Task<UserIdentification> GetCurrentUser(ProjectIdentification projectId)
            => Task.FromResult(CurrentUser);

        public Task<ExpenseIdentification> CreateExpense(ProjectIdentification projectId, ExpenseEditViewModel model)
        {
            if (CreateFailure is { } failure)
            {
                return Task.FromException<ExpenseIdentification>(failure);
            }
            Created = model;
            return Task.FromResult(new ExpenseIdentification(projectId, 99));
        }

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
