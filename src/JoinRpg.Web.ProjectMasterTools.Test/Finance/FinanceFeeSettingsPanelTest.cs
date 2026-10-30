using Bunit;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
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
/// Панель «Взнос и сроки»: добавление и удаление взноса через API без перезагрузки страницы,
/// отказ сервера показывается мастеру.
/// </summary>
public class FinanceFeeSettingsPanelTest : BunitContext
{
    private static readonly DateOnly Tomorrow = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

    private readonly FakeFinanceSettingsClient client = new();

    public FinanceFeeSettingsPanelTest()
    {
        Services.AddSingleton<IFinanceSettingsClient>(client);
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        var dialogModule = JSInterop.SetupModule("/_content/JoinRpg.Common.WebComponents/component-interop.js");
        _ = dialogModule.SetupVoid("showModal", _ => true);
        _ = dialogModule.SetupVoid("closeModal", _ => true);
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
    }

    [Fact]
    public void WithoutFeesWarnsEvenInCollapsedHeader()
    {
        var cut = RenderPanel();

        cut.Find(".panel-heading").TextContent.ShouldContain("Взнос не настроен");
        cut.FindAll("table").ShouldBeEmpty();
    }

    [Fact]
    public async Task AddedFeeAppearsWithoutReload()
    {
        var cut = RenderPanel();

        var click = cut.FindAll("button").Single(b => b.TextContent.Contains("Добавить новый размер взноса")).ClickAsync(new MouseEventArgs());
        cut.Find("dialog input[type=number]").Change("1500");
        await PressDialogButton(cut, "Добавить");
        await click;

        var created = client.CreatedFee.ShouldNotBeNull();
        created.Fee.ShouldBe(1500);
        created.StartDate.ShouldBe(DateOnly.FromDateTime(DateTime.Today));
        cut.WaitForAssertion(() => cut.Find("table").TextContent.ShouldContain("1500"));
        cut.Find(".panel-heading").TextContent.ShouldNotContain("Взнос не настроен");
    }

    [Fact]
    public async Task CancelledDialogCanBeOpenedAgain()
    {
        var cut = RenderPanel();

        var click = AddButton(cut).ClickAsync(new MouseEventArgs());
        cut.Find("dialog input[type=number]").Change("1500");
        await PressDialogButton(cut, "Отменить");
        await click;

        client.CreatedFee.ShouldBeNull();
        AddButton(cut).HasAttribute("disabled").ShouldBeFalse();

        click = AddButton(cut).ClickAsync(new MouseEventArgs());
        // Новый показ — с чистой моделью, без введённого в прошлый раз
        cut.Find("dialog input[type=number]").GetAttribute("value").ShouldBe("0");
        await PressDialogButton(cut, "Добавить");
        await click;

        client.CreatedFee.ShouldNotBeNull().Fee.ShouldBe(0);
    }

    [Fact]
    public async Task WithoutPreferentialFeeOnlyMainFeeIsAsked()
    {
        var cut = RenderPanel();

        var click = AddButton(cut).ClickAsync(new MouseEventArgs());
        cut.FindAll("dialog input[type=number]").Count.ShouldBe(1);
        cut.Find("dialog input[type=number]").Change("1500");
        await PressDialogButton(cut, "Добавить");
        await click;

        client.CreatedFee.ShouldNotBeNull().PreferentialFee.ShouldBeNull();
    }

    [Fact]
    public async Task WithPreferentialFeeBothFeesAreSent()
    {
        client.PreferentialFeeEnabled = true;
        var cut = RenderPanel();

        var click = AddButton(cut).ClickAsync(new MouseEventArgs());
        var inputs = cut.FindAll("dialog input[type=number]");
        inputs.Count.ShouldBe(2);
        inputs[0].Change("1500");
        cut.FindAll("dialog input[type=number]")[1].Change("700");
        await PressDialogButton(cut, "Добавить");
        await click;

        var created = client.CreatedFee.ShouldNotBeNull();
        created.Fee.ShouldBe(1500);
        created.PreferentialFee.ShouldBe(700);
    }

    [Fact]
    public async Task DeleteRefusalIsShownAndFeeStays()
    {
        client.Fees.Add(new FinanceFeeSettingItemViewModel(7, 2000, null, Tomorrow, CanDelete: true));
        client.SaveError = "Нельзя менять взнос, который уже вступил в силу";
        var cut = RenderPanel();

        var click = cut.Find("table button").ClickAsync(new MouseEventArgs());
        await PressDialogButton(cut, "Удалить");
        await click;

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Нельзя менять взнос, который уже вступил в силу"));
        client.DeletedFeeId.ShouldBeNull();
        cut.Find("table").TextContent.ShouldContain("2000");
    }

    [Fact]
    public void FeeInForceCannotBeDeleted()
    {
        client.Fees.Add(new FinanceFeeSettingItemViewModel(7, 2000, null, Tomorrow, CanDelete: false));
        var cut = RenderPanel();

        cut.FindAll("table button").ShouldBeEmpty();
    }

    [Fact]
    public void WithoutMoneyPermissionNothingCanBeChanged()
    {
        client.HasEditAccess = false;
        client.Fees.Add(new FinanceFeeSettingItemViewModel(7, 2000, null, Tomorrow, CanDelete: true));
        var cut = RenderPanel();

        cut.FindAll("table button").ShouldBeEmpty();
        cut.FindAll("button").ShouldNotContain(b => b.TextContent.Contains("Добавить новый размер взноса"));
    }

    [Fact]
    public void ArchivedProjectShowsFeesReadOnly()
    {
        client.ProjectStatus = ProjectLifecycleStatus.Archived;
        client.Fees.Add(new FinanceFeeSettingItemViewModel(7, 2000, null, Tomorrow, CanDelete: true));
        var cut = RenderPanel();

        cut.Find("table").TextContent.ShouldContain("2000");
        cut.FindAll("table button").ShouldBeEmpty();
        cut.FindAll("button").ShouldNotContain(b => b.TextContent.Contains("Добавить новый размер взноса"));
    }

    private static AngleSharp.Dom.IElement AddButton(IRenderedComponent<FinanceFeeSettingsPanel> cut)
        => cut.FindAll("button").Single(b => b.TextContent.Contains("Добавить новый размер взноса"));

    private IRenderedComponent<FinanceFeeSettingsPanel> RenderPanel()
        => Render<FinanceFeeSettingsPanel>(p => p.Add(x => x.ProjectId, new ProjectIdentification(1)));

    /// <summary>Нажать кнопку в открытом диалоге и закрыть его так, как это сделал бы браузер.</summary>
    private static async Task PressDialogButton(IRenderedComponent<FinanceFeeSettingsPanel> cut, string buttonText)
    {
        var dialog = cut.FindAll("dialog").First(d => d.QuerySelectorAll("button").Any(b => b.TextContent.Trim() == buttonText));
        await dialog.QuerySelectorAll("button").First(b => b.TextContent.Trim() == buttonText).ClickAsync(new MouseEventArgs());
        await cut.FindAll("dialog").First(d => d.QuerySelectorAll("button").Any(b => b.TextContent.Trim() == buttonText))
            .TriggerEventAsync("onclose", EventArgs.Empty);
    }
}
