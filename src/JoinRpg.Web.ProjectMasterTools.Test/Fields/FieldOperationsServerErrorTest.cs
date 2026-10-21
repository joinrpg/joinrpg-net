using AngleSharp.Dom;
using Bunit;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectMasterTools.Fields;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.ProjectMasterTools.Test.Fields;

/// <summary>
/// Острова операций с полями показывают причину отказа сервера, а на сбое — свой текст, не сообщение исключения (#5735).
/// </summary>
public class FieldOperationsServerErrorTest : BunitContext
{
    private const string Blocked = "Проект заблокирован на время восстановления данных, изменения в нём сейчас невозможны";
    private const string RawMessage = "Response status code does not indicate success: 502 (Bad Gateway).";

    private static readonly ProjectFieldIdentification FieldId = new(new ProjectIdentification(1), 2);

    private readonly FakeFieldClient client = new();

    public FieldOperationsServerErrorTest()
    {
        Services.AddSingleton<IProjectFieldOperationsClient>(client);
        Services.AddSingleton<IProjectFieldUriLocator>(new FakeUriLocator());
        Services.AddSingleton<IMoveClient>(new NotUsedMoveClient());
        Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        var dialogModule = JSInterop.SetupModule("/_content/JoinRpg.Common.WebComponents/component-interop.js");
        dialogModule.SetupVoid("showModal", _ => true);
        dialogModule.SetupVoid("closeModal", _ => true);
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
    }

    [Fact]
    public async Task DeleteFieldShowsRefusalReason()
    {
        client.FailWith = new ServerProblemException(Blocked);

        var cut = await DeleteField();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain(Blocked));
    }

    [Fact]
    public async Task DeleteFieldHidesUnexpectedError()
    {
        client.FailWith = new HttpRequestException(RawMessage);

        var cut = await DeleteField();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось удалить поле"));
        cut.Markup.ShouldNotContain("Bad Gateway");
    }

    [Fact]
    public async Task SortVariantsShowsRefusalReason()
    {
        client.FailWith = new ServerProblemException(Blocked);

        var cut = await SortVariantsByLabel();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain(Blocked));
    }

    [Fact]
    public async Task SortVariantsHidesUnexpectedError()
    {
        client.FailWith = new HttpRequestException(RawMessage);

        var cut = await SortVariantsByLabel();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось отсортировать значения"));
        cut.Markup.ShouldNotContain("Bad Gateway");
    }

    [Fact]
    public async Task MassAddVariantsShowsRefusalReason()
    {
        client.FailWith = new ServerProblemException(Blocked);

        var cut = await MassAddVariants();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain(Blocked));
    }

    [Fact]
    public async Task MassAddVariantsHidesUnexpectedError()
    {
        client.FailWith = new HttpRequestException(RawMessage);

        var cut = await MassAddVariants();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось добавить значения"));
        cut.Markup.ShouldNotContain("Bad Gateway");
    }

    [Fact]
    public async Task MassAddTimeSlotsShowsRefusalReason()
    {
        client.FailWith = new ServerProblemException(Blocked);

        var cut = await MassAddTimeSlots();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain(Blocked));
    }

    [Fact]
    public async Task MassAddTimeSlotsHidesUnexpectedError()
    {
        client.FailWith = new HttpRequestException(RawMessage);

        var cut = await MassAddTimeSlots();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось добавить значения"));
        cut.Markup.ShouldNotContain("Bad Gateway");
    }

    private async Task<IRenderedComponent<FieldEditControl>> DeleteField()
    {
        var item = new GameFieldListItemViewModel
        {
            Id = FieldId,
            Name = "Поле",
            IsActive = true,
            GroupNames = [],
            Variants = [],
        };
        var cut = Render<FieldEditControl>(p => p
            .Add(x => x.Item, item)
            .Add(x => x.AllItems, [item]));

        // Единственная кнопка вне диалогов — «Удалить», остальные — ссылки
        ButtonsOutsideDialogs(cut).Single().Click();
        await ConfirmAsync(cut, "Удалить поле «Поле»?");
        return cut;
    }

    private async Task<IRenderedComponent<FieldValuesListControl>> SortVariantsByLabel()
    {
        var cut = Render<FieldValuesListControl>(p => p
            .Add(x => x.FieldId, FieldId)
            .Add(x => x.Items,
            [
                new FieldVariantListItemViewModel
                {
                    VariantId = new ProjectFieldVariantIdentification(FieldId, 3),
                    Label = "Значение",
                    IsActive = true,
                    WasEverUsed = true,
                },
            ]));

        ButtonsOutsideDialogs(cut).Single(b => b.TextContent.Contains("Отсортировать по алфавиту")).Click();
        await ConfirmAsync(cut, "Отсортировать значения по алфавиту?");
        return cut;
    }

    private async Task<IRenderedComponent<FieldValuesMassAddButton>> MassAddVariants()
    {
        var cut = Render<FieldValuesMassAddButton>(p => p.Add(x => x.FieldId, FieldId));

        ButtonsOutsideDialogs(cut).Single().Click();
        await cut.Find("textarea").ChangeAsync(new ChangeEventArgs { Value = "Первое\nВторое" });
        await SubmitFormDialogAsync(cut);
        return cut;
    }

    private async Task<IRenderedComponent<TimeSlotMassAddButton>> MassAddTimeSlots()
    {
        var cut = Render<TimeSlotMassAddButton>(p => p
            .Add(x => x.Model, new TimeSlotMassAddViewModel(FieldId, new DateOnly(2026, 7, 10), 50)));

        ButtonsOutsideDialogs(cut).Single().Click();
        await SubmitFormDialogAsync(cut);
        return cut;
    }

    private static IEnumerable<IElement> ButtonsOutsideDialogs<T>(IRenderedComponent<T> cut)
        where T : IComponent
        => cut.FindAll("button").Where(b => b.Closest("dialog") is null);

    /// <summary>
    /// Подтверждение у кнопки с AutoConfirm: «да» в диалоге с этим вопросом, затем закрытие диалога
    /// </summary>
    private static async Task ConfirmAsync<T>(IRenderedComponent<T> cut, string question)
        where T : IComponent
    {
        IElement Dialog() => cut.FindAll("dialog").Single(d => d.TextContent.Contains(question));

        var yes = Dialog().QuerySelectorAll(".join-dialog-footer button").First(b => !b.TextContent.Contains("Отмен"));
        await yes.ClickAsync(new MouseEventArgs());
        await Dialog().TriggerEventAsync("onclose", EventArgs.Empty);
    }

    private static async Task SubmitFormDialogAsync<T>(IRenderedComponent<T> cut)
        where T : IComponent
    {
        await cut.FindAll(".join-dialog-footer button")[0].ClickAsync(new MouseEventArgs());
        await cut.Find("dialog").TriggerEventAsync("onclose", EventArgs.Empty);
    }

    private class FakeFieldClient : IProjectFieldOperationsClient
    {
        public Exception? FailWith { get; set; }

        public Task Delete(ProjectFieldIdentification fieldId) => Fail();
        public Task DeleteVariant(ProjectFieldVariantIdentification variantId) => Fail();
        public Task DeleteUnusedVariants(ProjectFieldIdentification fieldId) => Fail();
        public Task CreateTimeSlots(TimeSlotMassAddRequest request) => Fail();
        public Task SortTimeSlotsByStartTime(ProjectFieldIdentification fieldId) => Fail();
        public Task SortVariantsByLabel(ProjectFieldIdentification fieldId) => Fail();
        public Task CreateVariants(FieldValuesMassAddRequest request) => Fail();
        public Task<ProjectFieldSettingsViewModel> GetFieldSettings(ProjectIdentification projectId) => throw new NotSupportedException();
        public Task SaveFieldSettings(ProjectFieldSettingsViewModel model) => throw new NotSupportedException();

        private Task Fail() => FailWith is null ? Task.CompletedTask : Task.FromException(FailWith);
    }

    private class FakeUriLocator : IProjectFieldUriLocator
    {
        public Uri GetEditUri(ProjectFieldIdentification fieldId) => new("https://localhost/field/edit");
        public Uri GetCreateVariantUri(ProjectFieldIdentification fieldId) => new("https://localhost/field/variant/create");
        public Uri GetEditVariantUri(ProjectFieldVariantIdentification variantId) => new("https://localhost/field/variant/edit");
    }

    private class NotUsedMoveClient : IMoveClient
    {
        public Task<string[]> MoveAfterAsync(string selfId, string parentId, string? moveAfterId) => throw new NotSupportedException();
    }
}
