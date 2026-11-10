using Bunit;
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

namespace JoinRpg.Web.Finances.Test;

public class ExpenseEditFormTest : BunitContext
{
    private static readonly ProjectIdentification ProjectId = new(1);

    private static readonly UserInfoHeader[] Masters =
    [
        new(new UserIdentification(1), new UserDisplayName("Иван Мастеров", null), null),
        new(new UserIdentification(2), new UserDisplayName("Марья Мастериха", null), null),
    ];

    public ExpenseEditFormTest()
    {
        Services.AddSingleton<IMasterClient>(new FakeMasterClient(Masters));
        _ = AddBunitPersistentComponentState();
        Services.AddScoped<SelectorListCache>();
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
    }

    [Fact]
    public void RendersFormFields()
    {
        var cut = RenderForm(new ExpenseEditViewModel());

        cut.FindAll("input[type=text]").Count.ShouldBe(2); // название и категория
        cut.FindAll("input[type=number]").Count.ShouldBe(4); // план и факт: количество и цена
        cut.Find("input[type=date]");
        cut.Find("textarea");
        cut.Find("details.join-select"); // селектор мастера
    }

    [Fact]
    public void CategoryFieldRendersSuggestionsAsDatalist()
    {
        string[] suggestions = ["Аренда базы", "Аренда базы\\Жилые корпуса", "Транспорт"];
        var cut = RenderForm(new ExpenseEditViewModel(), categorySuggestions: suggestions);

        var dataList = cut.Find("datalist");
        dataList.Id.ShouldNotBeNullOrWhiteSpace();
        cut.FindAll("datalist option")
            .Select(option => option.GetAttribute("value"))
            .ShouldBe(suggestions);
        // Поле категории привязано к этому datalist атрибутом list (первый input — название)
        cut.FindAll("input[type=text]")[1].GetAttribute("list").ShouldBe(dataList.Id);
    }

    [Fact]
    public void CategoryFieldWithoutSuggestionsHasNoDatalist()
    {
        var cut = RenderForm(new ExpenseEditViewModel());

        cut.FindAll("datalist").ShouldBeEmpty();
        cut.FindAll("input[type=text]")[1].HasAttribute("list").ShouldBeFalse();
    }

    [Fact]
    public void NumberFieldsHaveMinZeroAndNoMax()
    {
        var cut = RenderForm(new ExpenseEditViewModel());

        var inputs = cut.FindAll("input[type=number]");
        inputs.Count.ShouldBe(4); // план и факт: количество и цена
        foreach (var input in inputs)
        {
            input.GetAttribute("min").ShouldBe("0");
            input.HasAttribute("max").ShouldBeFalse();
        }
    }

    [Fact]
    public void CreateModeShowsAddButtonInPanel()
    {
        var cut = RenderForm(new ExpenseEditViewModel());

        cut.Markup.ShouldContain("Новый расход"); // заголовок панели
        cut.Markup.ShouldContain("Добавить");
        cut.Markup.ShouldNotContain("Сохранить");
    }

    [Fact]
    public void MasterSelectorShowsMastersFromClient()
    {
        var cut = RenderForm(new ExpenseEditViewModel());

        cut.Markup.ShouldContain("Иван Мастеров");
        cut.Markup.ShouldContain("Марья Мастериха");
    }

    [Fact]
    public void ValidSubmitReturnsFilledModel()
    {
        ExpenseEditViewModel? saved = null;
        var model = new ExpenseEditViewModel();
        var cut = RenderForm(model, onSave: savedModel => saved = savedModel);

        cut.FindAll("input[type=text]")[0].Change("Аренда зала");
        cut.FindAll("input[type=text]")[1].Change("Аренда базы\\Жилые корпуса");
        SelectMaster(cut, "Марья Мастериха");
        cut.FindAll("input[type=number]")[0].Change("3");
        cut.FindAll("input[type=number]")[1].Change("1500");
        cut.FindAll("input[type=number]")[2].Change("2");
        cut.FindAll("input[type=number]")[3].Change("1400");
        cut.Find("input[type=date]").Change("2026-02-15");
        cut.Find("textarea").Change("Оплата по счёту №7");

        cut.Find("form").Submit();

        saved.ShouldNotBeNull();
        saved.ExpenseId.ShouldBeNull();
        saved.Name.ShouldBe("Аренда зала");
        saved.CategoryPath.ShouldBe("Аренда базы\\Жилые корпуса");
        saved.MasterId.ShouldBe(new UserIdentification(2));
        saved.PlannedQuantity.ShouldBe(3);
        saved.PlannedPrice.ShouldBe(1500);
        saved.ActualQuantity.ShouldBe(2);
        saved.ActualPrice.ShouldBe(1400);
        saved.SpentAt.ShouldBe(new DateOnly(2026, 2, 15));
        saved.Description.ShouldBe("Оплата по счёту №7");
    }

    [Fact]
    public void ActualPriceFillsEmptyPlannedFields()
    {
        ExpenseEditViewModel? saved = null;
        var cut = RenderForm(new ExpenseEditViewModel(), onSave: savedModel => saved = savedModel);
        FillRequiredTextFields(cut);
        SelectMaster(cut, "Марья Мастериха");
        cut.FindAll("input[type=number]")[2].Change("2"); // факт количество
        cut.FindAll("input[type=number]")[3].Change("1400"); // факт цена

        cut.Find("form").Submit();

        saved.ShouldNotBeNull();
        saved.PlannedQuantity.ShouldBe(2);
        saved.PlannedPrice.ShouldBe(1400);
    }

    [Fact]
    public void ActualPriceDoesNotOverwriteFilledPlan()
    {
        ExpenseEditViewModel? saved = null;
        var model = new ExpenseEditViewModel { PlannedQuantity = 3, PlannedPrice = 500 };
        var cut = RenderForm(model, onSave: savedModel => saved = savedModel);
        FillRequiredTextFields(cut);
        SelectMaster(cut, "Марья Мастериха");
        cut.FindAll("input[type=number]")[2].Change("2");
        cut.FindAll("input[type=number]")[3].Change("1400");

        cut.Find("form").Submit();

        saved.ShouldNotBeNull();
        saved.PlannedQuantity.ShouldBe(3);
        saved.PlannedPrice.ShouldBe(500);
    }

    [Fact]
    public void LaterActualChangeDoesNotOverwritePlanFilledFromActual()
    {
        ExpenseEditViewModel? saved = null;
        var cut = RenderForm(new ExpenseEditViewModel(), onSave: savedModel => saved = savedModel);
        FillRequiredTextFields(cut);
        SelectMaster(cut, "Марья Мастериха");
        cut.FindAll("input[type=number]")[0].Change("1"); // плановое количество — вручную
        cut.FindAll("input[type=number]")[3].Change("1400"); // план цены заполнился из факта
        cut.FindAll("input[type=number]")[3].Change("2000"); // факт изменили — план не перезаписываем

        cut.Find("form").Submit();

        saved.ShouldNotBeNull();
        saved.PlannedPrice.ShouldBe(1400);
    }

    [Fact]
    public void SpentAtFieldLabelIsDate()
    {
        var cut = RenderForm(new ExpenseEditViewModel());

        cut.Markup.ShouldNotContain("Дата траты");
        cut.FindAll("label").ShouldContain(label => label.TextContent.Trim() == "Дата");
    }

    [Fact]
    public void InvalidFormDoesNotSubmit()
    {
        ExpenseEditViewModel? saved = null;
        var cut = RenderForm(new ExpenseEditViewModel(), onSave: savedModel => saved = savedModel);

        // Ничего не заполняли: название, категория и мастер обязательны.
        cut.Find("form").Submit();

        saved.ShouldBeNull();
        cut.Markup.ShouldContain("Укажите название расхода");
    }

    [Fact]
    public void EditModeShowsStateBadgeAndKeepsExpenseId()
    {
        ExpenseEditViewModel? saved = null;
        var model = new ExpenseEditViewModel
        {
            ExpenseId = new ExpenseIdentification(new ProjectIdentification(1), 42),
            Name = "Аренда зала",
            CategoryPath = "Аренда базы",
            MasterId = new UserIdentification(1),
            PlannedQuantity = 1,
            PlannedPrice = 100,
        };
        var cut = RenderForm(model, state: ExpenseStateView.Rejected, onSave: savedModel => saved = savedModel);

        cut.Markup.ShouldContain("Удален");
        cut.Markup.ShouldContain("Редактирование расхода");
        cut.Markup.ShouldContain("Сохранить");
        cut.Markup.ShouldNotContain("Добавить");

        cut.Find("form").Submit();

        saved.ShouldNotBeNull();
        saved.ExpenseId.ShouldBe(new ExpenseIdentification(new ProjectIdentification(1), 42));
    }

    [Fact]
    public void EditModeWithActualShowsCombinedStatus()
    {
        var model = new ExpenseEditViewModel
        {
            ExpenseId = new ExpenseIdentification(new ProjectIdentification(1), 42),
            Name = "Аренда зала",
            CategoryPath = "Аренда базы",
            MasterId = new UserIdentification(1),
            PlannedPrice = 100,
            ActualQuantity = 1,
            ActualPrice = 50,
        };
        var cut = RenderForm(model, state: ExpenseStateView.Rejected);

        cut.Markup.ShouldContain("Не согласован");
    }

    [Fact]
    public void EditModeKeepsSavedSpentAt()
    {
        var model = new ExpenseEditViewModel
        {
            ExpenseId = new ExpenseIdentification(new ProjectIdentification(1), 42),
            Name = "Аренда зала",
            CategoryPath = "Аренда базы",
            MasterId = new UserIdentification(1),
            PlannedPrice = 100,
            SpentAt = new DateOnly(2026, 2, 15),
        };

        var cut = RenderForm(model);

        cut.Find("input[name=SpentAt]").GetAttribute("value").ShouldBe("2026-02-15");
    }

    [Fact]
    public void CancelButtonInvokesCallback()
    {
        var cancelled = false;
        var cut = RenderForm(new ExpenseEditViewModel(), onCancel: () => cancelled = true);

        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Отменить").Click();

        cancelled.ShouldBeTrue();
    }

    private static void FillRequiredTextFields(IRenderedComponent<ExpenseEditForm> cut)
    {
        cut.FindAll("input[type=text]")[0].Change("Аренда зала");
        cut.FindAll("input[type=text]")[1].Change("Аренда базы\\Жилые корпуса");
    }

    private static void SelectMaster(IRenderedComponent<ExpenseEditForm> cut, string displayName)
    {
        var item = cut.FindAll("details.join-select .join-select__item")
            .Single(i => i.QuerySelector(".join-select__text")!.TextContent.Trim() == displayName);
        item.QuerySelector("input")!.Change(true);
    }

    private IRenderedComponent<ExpenseEditForm> RenderForm(
        ExpenseEditViewModel model,
        ExpenseStateView? state = null,
        IReadOnlyCollection<string>? categorySuggestions = null,
        Action<ExpenseEditViewModel>? onSave = null,
        Action? onCancel = null)
        => Render<ExpenseEditForm>(p => p
            .Add(x => x.Model, model)
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.State, state)
            .Add(x => x.CategorySuggestions, categorySuggestions)
            .Add(x => x.OnSave, EventCallback.Factory.Create<ExpenseEditViewModel>(this, savedModel => onSave?.Invoke(savedModel)))
            .Add(x => x.OnCancel, EventCallback.Factory.Create(this, () => onCancel?.Invoke())));

    private sealed class FakeMasterClient(UserInfoHeader[] masters) : IMasterClient
    {
        public Task<List<UserInfoHeader>> GetMasters(int projectId) => Task.FromResult(masters.ToList());
    }
}
