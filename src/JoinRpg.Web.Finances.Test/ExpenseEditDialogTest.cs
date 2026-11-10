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

public class ExpenseEditDialogTest : BunitContext
{
    private static readonly ProjectIdentification ProjectId = new(1);

    private static readonly UserInfoHeader[] Masters =
    [
        new(new UserIdentification(1), new UserDisplayName("Иван Мастеров", null), null),
        new(new UserIdentification(2), new UserDisplayName("Марья Мастериха", null), null),
    ];

    public ExpenseEditDialogTest()
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
        var cut = RenderDialog();

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
        var cut = RenderDialog(categorySuggestions: suggestions);

        var dataList = cut.Find("datalist");
        dataList.Id.ShouldNotBeNullOrWhiteSpace();
        cut.FindAll("datalist option")
            .Select(option => option.GetAttribute("value"))
            .ShouldBe(suggestions);
        // Поле категории привязано к этому datalist атрибутом list (первый input — название)
        cut.FindAll("input[type=text]")[1].GetAttribute("list").ShouldBe(dataList.Id);
    }

    [Fact]
    public void CreateModeCaptionAndAddButton()
    {
        var cut = RenderDialog();
        _ = cut.InvokeAsync(() => cut.Instance.ShowAsync(new ExpenseEditViewModel(), state: null));

        cut.Find(".join-dialog-caption").TextContent.Trim().ShouldBe("Новый расход");
        FooterButtons(cut)[0].TextContent.Trim().ShouldBe("Добавить");
    }

    [Fact]
    public void EditModeCaptionSaveButtonAndStateBadge()
    {
        var cut = RenderDialog();
        var model = new ExpenseEditViewModel
        {
            ExpenseId = new ExpenseIdentification(ProjectId, 42),
            Name = "Аренда зала",
            CategoryPath = "Аренда базы",
            MasterId = new UserIdentification(1),
            PlannedQuantity = 1,
            PlannedPrice = 100,
        };
        _ = cut.InvokeAsync(() => cut.Instance.ShowAsync(model, ExpenseStateView.Rejected));

        cut.Find(".join-dialog-caption").TextContent.Trim().ShouldBe("Редактирование расхода");        FooterButtons(cut)[0].TextContent.Trim().ShouldBe("Сохранить");
        cut.Markup.ShouldContain("Удален");
    }

    [Fact]
    public async Task ActualFillsEmptyPlannedFields()
    {
        var cut = RenderDialog();
        var model = new ExpenseEditViewModel();
        var task = cut.InvokeAsync(() => cut.Instance.ShowAsync(model, state: null));

        FillRequiredTextFields(cut);
        SelectMaster(cut, "Марья Мастериха");
        cut.FindAll("input[type=number]")[2].Change("2"); // факт количество
        cut.FindAll("input[type=number]")[3].Change("1400"); // факт цена

        // Плановые поля подставились из факта прямо в форме
        cut.FindAll("input[type=number]")[0].GetAttribute("value").ShouldBe("2"); // план количество
        cut.FindAll("input[type=number]")[1].GetAttribute("value").ShouldBe("1400"); // план цена

        await SubmitAsync(cut);

        var saved = await task;
        saved.ShouldNotBeNull();
        saved.PlannedQuantity.ShouldBe(2);
        saved.PlannedPrice.ShouldBe(1400);
    }

    [Fact]
    public async Task ActualDoesNotOverwriteFilledPlan()
    {
        var cut = RenderDialog();
        var model = new ExpenseEditViewModel { PlannedQuantity = 3, PlannedPrice = 500 };
        var task = cut.InvokeAsync(() => cut.Instance.ShowAsync(model, state: null));

        FillRequiredTextFields(cut);
        SelectMaster(cut, "Марья Мастериха");
        cut.FindAll("input[type=number]")[2].Change("2");
        cut.FindAll("input[type=number]")[3].Change("1400");

        cut.FindAll("input[type=number]")[0].GetAttribute("value").ShouldBe("3");
        cut.FindAll("input[type=number]")[1].GetAttribute("value").ShouldBe("500");

        await SubmitAsync(cut);

        var saved = await task;
        saved.ShouldNotBeNull();
        saved.PlannedQuantity.ShouldBe(3);
        saved.PlannedPrice.ShouldBe(500);
    }

    [Fact]
    public async Task LaterActualChangeDoesNotOverwritePlanFilledFromActual()
    {
        var cut = RenderDialog();
        var model = new ExpenseEditViewModel();
        var task = cut.InvokeAsync(() => cut.Instance.ShowAsync(model, state: null));

        FillRequiredTextFields(cut);
        SelectMaster(cut, "Марья Мастериха");
        cut.FindAll("input[type=number]")[0].Change("1"); // плановое количество — вручную
        cut.FindAll("input[type=number]")[3].Change("1400"); // план цены заполнился из факта
        cut.FindAll("input[type=number]")[3].Change("2000"); // факт изменили — план не перезаписываем

        cut.FindAll("input[type=number]")[1].GetAttribute("value").ShouldBe("1400");

        await SubmitAsync(cut);

        var saved = await task;
        saved.ShouldNotBeNull();
        saved.PlannedPrice.ShouldBe(1400);
    }

    [Fact]
    public async Task CancelReturnsNull()
    {
        var cut = RenderDialog();
        var task = cut.InvokeAsync(() => cut.Instance.ShowAsync(new ExpenseEditViewModel(), state: null));

        FooterButtons(cut)[1].TextContent.Trim().ShouldBe("Отменить");
        await FooterButtons(cut)[1].ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await cut.Find("dialog").TriggerEventAsync("onclose", EventArgs.Empty);

        (await task).ShouldBeNull();
    }

    [Fact]
    public void InvalidFormDisablesSubmit()
    {
        var cut = RenderDialog();
        _ = cut.InvokeAsync(() => cut.Instance.ShowAsync(new ExpenseEditViewModel(), state: null));

        // Ничего не заполняли: кнопка сабмита выключена
        FooterButtons(cut)[0].HasAttribute("disabled").ShouldBeTrue();
    }

    private static IReadOnlyList<AngleSharp.Dom.IElement> FooterButtons(IRenderedComponent<ExpenseEditDialog> cut)
        => cut.FindAll(".join-dialog-footer button");

    private static async Task SubmitAsync(IRenderedComponent<ExpenseEditDialog> cut)
    {
        await FooterButtons(cut)[0].ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await cut.Find("dialog").TriggerEventAsync("onclose", EventArgs.Empty);
    }

    private static void FillRequiredTextFields(IRenderedComponent<ExpenseEditDialog> cut)
    {
        cut.FindAll("input[type=text]")[0].Change("Аренда зала");
        cut.FindAll("input[type=text]")[1].Change("Аренда базы\\Жилые корпуса");
    }

    private static void SelectMaster(IRenderedComponent<ExpenseEditDialog> cut, string displayName)
    {
        var item = cut.FindAll("details.join-select .join-select__item")
            .Single(i => i.QuerySelector(".join-select__text")!.TextContent.Trim() == displayName);
        item.QuerySelector("input")!.Change(true);
    }

    private IRenderedComponent<ExpenseEditDialog> RenderDialog(IReadOnlyCollection<string>? categorySuggestions = null)
        => Render<ExpenseEditDialog>(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.CategorySuggestions, categorySuggestions));

    private sealed class FakeMasterClient(UserInfoHeader[] masters) : IMasterClient
    {
        public Task<List<UserInfoHeader>> GetMasters(int projectId) => Task.FromResult(masters.ToList());
    }
}
