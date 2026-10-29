using Bunit;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectMasterTools.Finance;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.ProjectMasterTools.Test.Finance;

/// <summary>
/// Панель «Методы оплаты»: включение и выключение через API без перезагрузки страницы,
/// «Включить» у ещё не созданного метода создаёт его, отказ сервера показывается мастеру.
/// </summary>
public class FinancePaymentTypesPanelTest : BunitContext
{
    private static readonly ProjectIdentification ProjectId = new(1);
    private static readonly UserLinkViewModel Master = new(new UserIdentification(2), "Мастер", ViewMode.Show);
    private static readonly UserLinkViewModel PaymentsUser = new(new UserIdentification(9000), "Платёжная система", ViewMode.Show);

    private readonly FakeFinanceSettingsClient client = new();

    public FinancePaymentTypesPanelTest()
    {
        Services.AddSingleton<IFinanceSettingsClient>(client);
        Services.AddSingleton<IFinanceSettingsUriLocator>(new FakeUriLocator());
        Services.AddSingleton<IMasterClient>(new FakeMasterClient());
        Services.AddSingleton<IUriLocator<UserLinkViewModel>>(new FakeUserLinkLocator());
        _ = AddBunitPersistentComponentState();
        Services.AddScoped<SelectorListCache>();
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
    }

    private static FinancePaymentTypeItemViewModel Online(bool isActive = false, bool canToggle = false)
        => new(isActive ? new PaymentTypeIdentification(ProjectId, 50) : null, PaymentTypeKind.Online, "Онлайн", isActive, false, PaymentsUser, canToggle);

    private static FinancePaymentTypeItemViewModel PotentialCash()
        => new(null, PaymentTypeKind.Cash, "Наличными", false, false, Master, CanToggle: true);

    private static FinancePaymentTypeItemViewModel Custom(int id, bool isActive = true)
        => new(new PaymentTypeIdentification(ProjectId, id), PaymentTypeKind.Custom, "Карта", isActive, false, Master, CanToggle: true);

    [Fact]
    public async Task EnablingPotentialCashCreatesItForThatMaster()
    {
        client.PaymentTypes.Add(PotentialCash());
        var cut = RenderPanel();

        await ClickAndConfirm(cut, "Включить", "Включить метод оплаты «Наличными»?");

        var created = client.CreatedPaymentType.ShouldNotBeNull();
        created.Kind.ShouldBe(PaymentTypeKind.Cash);
        created.MasterId.ShouldBe(Master.UserId);
        created.Name.ShouldBeNull();
        cut.WaitForAssertion(() => RowButtons(cut).ShouldContain("Выключить"));
    }

    [Fact]
    public async Task AdminEnablingOnlinePaymentCreatesItWithoutMaster()
    {
        client.PaymentTypes.Add(Online(canToggle: true));
        var cut = RenderPanel();

        await ClickAndConfirm(cut, "Включить", "Включить метод оплаты «Онлайн»?");

        var created = client.CreatedPaymentType.ShouldNotBeNull();
        created.Kind.ShouldBe(PaymentTypeKind.Online);
        // Онлайн-оплата не привязана к мастеру — сервис сам подставит служебного пользователя
        created.MasterId.ShouldBeNull();
    }

    [Fact]
    public async Task DisablingExistingTypeTogglesIt()
    {
        client.PaymentTypes.Add(Custom(7));
        var cut = RenderPanel();

        await ClickAndConfirm(cut, "Выключить", "Выключить метод оплаты «Карта»?");

        client.ToggledPaymentType.ShouldBe(new PaymentTypeIdentification(ProjectId, 7));
        cut.WaitForAssertion(() => RowButtons(cut).ShouldContain("Включить"));
    }

    [Fact]
    public async Task RefusalIsShown()
    {
        client.PaymentTypes.Add(Custom(7));
        client.SaveError = "Метод оплаты не найден. Обновите страницу и попробуйте ещё раз.";
        var cut = RenderPanel();

        await ClickAndConfirm(cut, "Выключить", "Выключить метод оплаты «Карта»?");

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Метод оплаты не найден"));
        RowButtons(cut).ShouldContain("Выключить");
    }

    [Fact]
    public void MasterSeesHowToEnableOnlinePaymentsInsteadOfButton()
    {
        client.PaymentTypes.Add(Online(canToggle: false));
        var cut = RenderPanel();

        RowButtons(cut).ShouldBe(["Узнать, как включить"]);
        cut.Find("a[href='/OnlinePayments']").ShouldNotBeNull();
    }

    [Fact]
    public void ActiveCustomTypeCanBeEdited()
    {
        client.PaymentTypes.Add(Custom(7));
        var cut = RenderPanel();

        cut.Find("a[href='/1/money/EditPaymentType?PaymentTypeId=7']").TextContent.ShouldContain("Изменить");
    }

    [Fact]
    public void FeeWithoutActivePaymentTypesWarnsEvenInCollapsedHeader()
    {
        client.HasFeeSettings = true;
        client.PaymentTypes.Add(PotentialCash());
        var cut = RenderPanel();

        cut.Find(".panel-heading").TextContent.ShouldContain("Нет методов оплаты");
    }

    [Fact]
    public void WithoutFeeNoWarning()
    {
        client.PaymentTypes.Add(PotentialCash());
        var cut = RenderPanel();

        cut.Find(".panel-heading").TextContent.ShouldNotContain("Нет методов оплаты");
    }

    [Fact]
    public void WithoutMoneyPermissionNothingCanBeChanged()
    {
        client.HasEditAccess = false;
        client.PaymentTypes.Add(Custom(7));
        client.PaymentTypes.Add(PotentialCash());
        var cut = RenderPanel();

        cut.FindAll("button").ShouldNotContain(b => b.TextContent.Contains("Добавить метод оплаты"));
        RowButtons(cut).ShouldBeEmpty();
    }

    [Fact]
    public void ArchivedProjectShowsListReadOnly()
    {
        client.ProjectStatus = ProjectLifecycleStatus.Archived;
        client.PaymentTypes.Add(Custom(7));
        var cut = RenderPanel();

        cut.Find("table").TextContent.ShouldContain("Карта");
        RowButtons(cut).ShouldBeEmpty();
    }

    private IRenderedComponent<FinancePaymentTypesPanel> RenderPanel()
        => Render<FinancePaymentTypesPanel>(p => p.Add(x => x.ProjectId, ProjectId));

    /// <summary>Действия в строках таблицы: кнопки и ссылки-кнопки, без диалогов подтверждения и ссылок на профили мастеров.</summary>
    private static string[] RowButtons(IRenderedComponent<FinancePaymentTypesPanel> cut)
        => [.. RowActions(cut).Select(b => b.TextContent.Trim()).Where(t => t.Length > 0)];

    private static IEnumerable<AngleSharp.Dom.IElement> RowActions(IRenderedComponent<FinancePaymentTypesPanel> cut)
        => cut.FindAll("table button, table a")
            .Where(e => e.Closest("dialog") is null && e.GetAttribute("href")?.StartsWith("/user/") != true);

    private static async Task ClickAndConfirm(IRenderedComponent<FinancePaymentTypesPanel> cut, string buttonText, string question)
    {
        var click = RowActions(cut).Single(b => b.TextContent.Trim() == buttonText).ClickAsync(new MouseEventArgs());
        var confirm = cut.FindAll("dialog").Single(d => d.TextContent.Contains(question));
        await confirm.QuerySelectorAll("button").Single(b => b.TextContent.Trim() == buttonText).ClickAsync(new MouseEventArgs());
        await cut.FindAll("dialog").Single(d => d.TextContent.Contains(question)).TriggerEventAsync("onclose", EventArgs.Empty);
        await click;
    }

    private sealed class FakeUriLocator : IFinanceSettingsUriLocator
    {
        public Uri GetEditPaymentTypeUri(PaymentTypeIdentification paymentTypeId)
            => new($"/{paymentTypeId.ProjectId.Value}/money/EditPaymentType?PaymentTypeId={paymentTypeId.PaymentTypeId}", UriKind.Relative);

        public Uri GetOnlinePaymentsInfoUri() => new("/OnlinePayments", UriKind.Relative);
    }

    private sealed class FakeUserLinkLocator : IUriLocator<UserLinkViewModel>
    {
        public Uri GetUri(UserLinkViewModel target) => new($"/user/{target.UserId!.Value}", UriKind.Relative);
    }

    private sealed class FakeMasterClient : IMasterClient
    {
        public Task<List<UserInfoHeader>> GetMasters(int projectId) => Task.FromResult(new List<UserInfoHeader>());
    }
}
