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

    private static readonly UserInfoHeader[] Masters =
    [
        new(new UserIdentification(42), new UserDisplayName("Иван Мастеров", null), null),
    ];

    private readonly FakeExpensesClient expensesClient = new([Expense]);

    public ExpensesPageTest()
    {
        Services.AddSingleton<IExpensesClient>(expensesClient);
        Services.AddSingleton<IMasterClient>(new FakeMasterClient(Masters));
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

        ClickButton(cut, "Новый расход");

        var today = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        cut.Find("dialog input[name=SpentAt]").GetAttribute("value").ShouldBe(today);
    }

    [Fact]
    public async Task EditButtonDoesNotStuckInProgressAfterCancel()
    {
        var cut = Render<ExpensesPage>(p => p.Add(x => x.ProjectId, ProjectId));

        ClickEditButton(cut);
        DialogCaption(cut).ShouldBe("Редактирование расхода");

        await CancelDialogAsync(cut);

        // Регрессия: после отмены кнопка «Править» зависала в состоянии прогресса
        // (JoinButton.AutoProgress) и переставала реагировать на клики. Закрытие диалога
        // в разметке не видно (видимость управляется JS), поэтому проверяем кнопку.
        var editButton = FindEditButton(cut);
        editButton.HasAttribute("disabled").ShouldBeFalse();
        editButton.InnerHtml.ShouldNotContain("#hourglass"); // иконка прогресса

        ClickEditButton(cut);
        DialogCaption(cut).ShouldBe("Редактирование расхода");
    }

    [Fact]
    public void RowWithoutAvailableActionsRendersNoButtonGroup()
    {
        // Расход не мой и нет права на деньги — ни одной кнопки действия,
        // пустая JoinButtonGroup не должна оставлять пустой div в строке.
        // Группа кнопок в шапке страницы (Новый расход / Скачать Excel) не считается —
        // проверяем только строки таблицы.
        expensesClient.MyExpenses = [];
        var cut = Render<ExpensesPage>(p => p.Add(x => x.ProjectId, ProjectId));

        cut.FindAll("tr div[role=group]").ShouldBeEmpty();
    }

    [Fact]
    public void NewExpenseFormPrefillsCurrentUserAsMaster()
    {
        expensesClient.CurrentUser = new UserIdentification(42);
        var cut = Render<ExpensesPage>(p => p.Add(x => x.ProjectId, ProjectId));

        ClickButton(cut, "Новый расход");

        // Мастер по умолчанию — текущий пользователь: на переключателе выбрано его имя, а не «(нет)»
        cut.Find("dialog details.join-select summary .join-select__value").TextContent.Trim().ShouldBe("Иван Мастеров");
    }

    [Fact]
    public void NewExpenseFormDefaultsSpentAtToLatestExpenseDate()
    {
        expensesClient.Expenses =
        [
            Expense with { SpentAt = new DateOnly(2026, 10, 1) },
            Expense with { ExpenseId = new ExpenseIdentification(ProjectId, 2), Name = "Свежий расход", SpentAt = new DateOnly(2026, 11, 5) },
            Expense with { ExpenseId = new ExpenseIdentification(ProjectId, 3), Name = "Расход без даты", SpentAt = null },
        ];
        var cut = Render<ExpensesPage>(p => p.Add(x => x.ProjectId, ProjectId));

        ClickButton(cut, "Новый расход");

        // Дата по умолчанию — максимальный SpentAt среди расходов (расходы без даты пропускаются)
        cut.Find("dialog input[name=SpentAt]").GetAttribute("value").ShouldBe("2026-11-05");
    }

    [Fact]
    public void NewExpenseFormOnEmptyProjectDefaultsSpentAtToToday()
    {
        expensesClient.Expenses = [];
        var cut = Render<ExpensesPage>(p => p.Add(x => x.ProjectId, ProjectId));

        // Регрессия: на пустом проекте клик «Новый расход» не падает, дата — сегодня
        ClickButton(cut, "Новый расход");

        DialogCaption(cut).ShouldBe("Новый расход"); // диалог открылся
        var today = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        cut.Find("dialog input[name=SpentAt]").GetAttribute("value").ShouldBe(today);
    }

    [Fact]
    public void SpendButtonVisibleOnlyForExpenseWithoutActual()
    {
        expensesClient.Expenses =
        [
            Expense, // факта нет
            Expense with
            {
                ExpenseId = new ExpenseIdentification(ProjectId, 2),
                Name = "Расход с фактом",
                ActualQuantity = 1,
                ActualPrice = 500,
                SpentAt = new DateOnly(2026, 11, 1),
            },
        ];
        var cut = Render<ExpensesPage>(p => p.Add(x => x.ProjectId, ProjectId));

        var rowWithoutActual = cut.FindAll("tr").Single(row => row.TextContent.Contains("Аренда зала"));
        rowWithoutActual.QuerySelectorAll("button").Any(button => button.TextContent.Trim() == "Потратил").ShouldBeTrue();
        var rowWithActual = cut.FindAll("tr").Single(row => row.TextContent.Contains("Расход с фактом"));
        rowWithActual.QuerySelectorAll("button").Any(button => button.TextContent.Trim() == "Потратил").ShouldBeFalse();
    }

    [Fact]
    public void SpendButtonOpensDialogWithActualFilledFromPlan()
    {
        var cut = Render<ExpensesPage>(p => p.Add(x => x.ProjectId, ProjectId));

        ClickButton(cut, "Потратил");

        DialogCaption(cut).ShouldBe("Редактирование расхода");
        // Факт предзаполнен из плана (2 × 1500)
        var numberInputs = cut.FindAll("dialog input[type=number]");
        numberInputs[2].GetAttribute("value").ShouldBe("2"); // факт количество
        numberInputs[3].GetAttribute("value").ShouldBe("1500"); // факт цена
    }

    [Fact]
    public void ExportLinkPresentAndButtonsStayVisibleWhenDialogOpened()
    {
        var cut = Render<ExpensesPage>(p => p.Add(x => x.ProjectId, ProjectId));

        FindExportLink(cut).GetAttribute("href").ShouldBe("/webapi/1/expenses/export?export=xlsx");

        ClickEditButton(cut);

        // Диалог открылся поверх грида: кнопки страницы никуда не пропадают
        DialogCaption(cut).ShouldBe("Редактирование расхода");
        FindExportLink(cut);
        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Новый расход");
    }

    [Fact]
    public async Task SaveSendsFilledModelToServer()
    {
        var cut = Render<ExpensesPage>(p => p.Add(x => x.ProjectId, ProjectId));

        ClickButton(cut, "Новый расход");
        var dialog = cut.Find("dialog");
        dialog.QuerySelectorAll("input[type=text]")[0].Change("Аренда зала");
        dialog.QuerySelectorAll("input[type=text]")[1].Change("Аренда базы\\Жилые корпуса");
        SelectMaster(cut, "Иван Мастеров");
        dialog.QuerySelectorAll("input[type=number]")[2].Change("2"); // факт количество
        dialog.QuerySelectorAll("input[type=number]")[3].Change("1400"); // факт цена

        await SubmitDialogAsync(cut);

        cut.WaitForAssertion(() =>
        {
            expensesClient.Created.ShouldNotBeNull();
            expensesClient.Created.Name.ShouldBe("Аренда зала");
            expensesClient.Created.CategoryPath.ShouldBe("Аренда базы\\Жилые корпуса");
            expensesClient.Created.MasterId.ShouldBe(new UserIdentification(42));
            // План подставился из факта
            expensesClient.Created.PlannedQuantity.ShouldBe(2);
            expensesClient.Created.PlannedPrice.ShouldBe(1400);
        });
    }

    [Fact]
    public async Task SaveFailureKeepsEnteredDataForRetry()
    {
        var cut = Render<ExpensesPage>(p => p.Add(x => x.ProjectId, ProjectId));

        ClickButton(cut, "Новый расход");
        var dialog = cut.Find("dialog");
        dialog.QuerySelectorAll("input[type=text]")[0].Change("Аренда зала");
        dialog.QuerySelectorAll("input[type=text]")[1].Change("Аренда базы\\Жилые корпуса");
        SelectMaster(cut, "Иван Мастеров");
        dialog.QuerySelectorAll("input[type=number]")[2].Change("2"); // факт количество
        dialog.QuerySelectorAll("input[type=number]")[3].Change("1400"); // факт цена

        expensesClient.CreateFailure = new ServerProblemException("Нет прав на деньги");
        await SubmitDialogAsync(cut);

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Ошибка: Нет прав на деньги"));

        // Регрессия: старая форма оставалась открытой с введёнными данными. Тот же диалог
        // должен принять повторный сабмит без потери модели.
        expensesClient.CreateFailure = null;
        await SubmitDialogAsync(cut);

        cut.WaitForAssertion(() => expensesClient.Created.ShouldNotBeNull());
        expensesClient.Created.Name.ShouldBe("Аренда зала");
        expensesClient.Created.CategoryPath.ShouldBe("Аренда базы\\Жилые корпуса");
        expensesClient.Created.PlannedPrice.ShouldBe(1400); // план подставился из факта
    }

    [Fact]
    public void ApprovedExpenseForMoneyMaster_DeclineLivesInDropdown()
    {
        // Одобренный расход: «Отклонить» — редкое действие, живёт в выпадашке кнопки «Изменить»
        expensesClient.Expenses = [Expense with { State = ExpenseStateView.Approved }];
        expensesClient.ManageMoney = true;
        var cut = Render<ExpensesPage>(p => p.Add(x => x.ProjectId, ProjectId));

        var row = cut.FindAll("tr").Single(row => row.TextContent.Contains("Аренда зала"));
        row.QuerySelector("details.join-dropdown li a[role=button]")!.TextContent.Trim().ShouldBe("Отклонить");
        // Факта нет — рядом на месте кнопка «Потратил»
        row.QuerySelectorAll("button").Any(button => button.TextContent.Trim() == "Потратил").ShouldBeTrue();
    }

    [Fact]
    public async Task ApprovedExpenseForMoneyMaster_ConfirmedDeclineCallsReject()
    {
        expensesClient.Expenses = [Expense with { State = ExpenseStateView.Approved }];
        expensesClient.ManageMoney = true;
        var cut = Render<ExpensesPage>(p => p.Add(x => x.ProjectId, ProjectId));

        cut.Find("details.join-dropdown li a[role=button]").Click();

        cut.Markup.ShouldContain("Отклонить расход «Аренда зала»?");
        var confirmButton = cut.FindAll(".join-dialog-footer button").Single(button => button.TextContent.Trim() == "Отклонить");
        await confirmButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        // onclose — только на диалоге подтверждения: в разметке всегда есть ещё диалог редактирования
        var confirmDialog = cut.FindAll("dialog").Single(dialog => dialog.TextContent.Contains("Отклонить расход"));
        await confirmDialog.TriggerEventAsync("onclose", EventArgs.Empty);

        expensesClient.Rejected.ShouldBe(Expense.ExpenseId);
    }

    [Fact]
    public void ProposedExpenseForMoneyMaster_NoDropdownAndInlineButtonsStay()
    {
        // Предложенный расход решается прямыми кнопками «Одобрить»/«Отклонить» — выпадашки нет
        expensesClient.ManageMoney = true; // Expense по умолчанию Proposed
        var cut = Render<ExpensesPage>(p => p.Add(x => x.ProjectId, ProjectId));

        var row = cut.FindAll("tr").Single(row => row.TextContent.Contains("Аренда зала"));
        row.QuerySelector("details.join-dropdown").ShouldBeNull();
        row.QuerySelectorAll("button").Any(button => button.TextContent.Trim() == "Одобрить").ShouldBeTrue();
        row.QuerySelectorAll("button").Any(button => button.TextContent.Trim() == "Отклонить").ShouldBeTrue();
    }

    [Fact]
    public void ApprovedExpenseForRegularMaster_NoDeclineAction()
    {
        // Мастер без права на деньги не видит никаких действий над чужим расходом,
        // даже одобренным: ни выпадашки, ни текста «Отклонить», ни группы кнопок
        expensesClient.Expenses = [Expense with { State = ExpenseStateView.Approved }];
        expensesClient.ManageMoney = false;
        expensesClient.MyExpenses = [];
        var cut = Render<ExpensesPage>(p => p.Add(x => x.ProjectId, ProjectId));

        var row = cut.FindAll("tr").Single(row => row.TextContent.Contains("Аренда зала"));
        row.QuerySelector("details.join-dropdown").ShouldBeNull();
        row.TextContent.ShouldNotContain("Отклонить");
        row.QuerySelector("div[role=group]").ShouldBeNull();
    }

    private static string DialogCaption(IRenderedComponent<ExpensesPage> cut)
        => cut.Find(".join-dialog-caption").TextContent.Trim();

    private static void ClickButton(IRenderedComponent<ExpensesPage> cut, string label)
        => cut.FindAll("button").Single(button => button.TextContent.Trim() == label).Click();

    private static async Task SubmitDialogAsync(IRenderedComponent<ExpensesPage> cut)
    {
        var footerButtons = cut.FindAll(".join-dialog-footer button");
        await footerButtons[0].ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await cut.Find("dialog").TriggerEventAsync("onclose", EventArgs.Empty);
    }

    private static async Task CancelDialogAsync(IRenderedComponent<ExpensesPage> cut)
    {
        var footerButtons = cut.FindAll(".join-dialog-footer button");
        await footerButtons[1].ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await cut.Find("dialog").TriggerEventAsync("onclose", EventArgs.Empty);
    }

    private static void SelectMaster(IRenderedComponent<ExpensesPage> cut, string displayName)
    {
        var item = cut.FindAll("dialog details.join-select .join-select__item")
            .Single(i => i.QuerySelector(".join-select__text")!.TextContent.Trim() == displayName);
        item.QuerySelector("input")!.Change(true);
    }

    private static AngleSharp.Dom.IElement FindExportLink(IRenderedComponent<ExpensesPage> cut)
        => cut.FindAll("a").Single(a => a.TextContent.Trim() == "Скачать Excel");

    private static void ClickEditButton(IRenderedComponent<ExpensesPage> cut)
        => FindEditButton(cut).Click();

    private static AngleSharp.Dom.IElement FindEditButton(IRenderedComponent<ExpensesPage> cut)
        => cut.FindAll("button").Single(button => button.TextContent.Trim() == "Изменить");

    private sealed class FakeExpensesClient(ExpenseViewModel[] expenses) : IExpensesClient
    {
        /// <summary>Все расходы проекта; по умолчанию — из конструктора.</summary>
        public ExpenseViewModel[] Expenses { get; set; } = expenses;

        /// <summary>Расходы текущего пользователя; null — совпадают с <c>Expenses</c>.</summary>
        public ExpenseViewModel[]? MyExpenses { get; set; }

        /// <summary>Текущий пользователь — предзаполнение мастера в форме создания.</summary>
        public UserIdentification CurrentUser { get; set; } = new(1);

        /// <summary>Модель последнего вызова <c>CreateExpense</c>.</summary>
        public ExpenseEditViewModel? Created { get; private set; }

        /// <summary>Есть ли право на деньги в проекте (управляет видимостью кнопок согласования).</summary>
        public bool ManageMoney { get; set; }

        /// <summary>Id расхода, отклонённого через <c>RejectExpense</c>.</summary>
        public ExpenseIdentification? Rejected { get; private set; }

        public Task<IReadOnlyList<ExpenseViewModel>> GetExpenses(ProjectIdentification projectId)
            => Task.FromResult<IReadOnlyList<ExpenseViewModel>>(Expenses);

        public Task<IReadOnlyList<ExpenseViewModel>> GetMyExpenses(ProjectIdentification projectId)
            => Task.FromResult<IReadOnlyList<ExpenseViewModel>>(MyExpenses ?? Expenses);

        public Task<IReadOnlyCollection<ExpenseCategoryInfo>> GetCategories(ProjectIdentification projectId)
            => Task.FromResult<IReadOnlyCollection<ExpenseCategoryInfo>>([]);

        public Task<bool> CanManageMoney(ProjectIdentification projectId) => Task.FromResult(ManageMoney);

        public Task<IReadOnlyCollection<string>> GetCategoryPathSuggestions(ProjectIdentification projectId)
            => Task.FromResult<IReadOnlyCollection<string>>([]);

        public Task<UserIdentification> GetCurrentUser(ProjectIdentification projectId)
            => Task.FromResult(CurrentUser);

        /// <summary>Исключение, которое бросит <c>CreateExpense</c>; null — сохранение успешно.</summary>
        public ServerProblemException? CreateFailure { get; set; }

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
        {
            Rejected = expenseId;
            return Task.CompletedTask;
        }
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
