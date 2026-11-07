using Bunit.TestDoubles;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.Web.Claims.Finance;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Web.Claims.Test;

public class ChangeFeeButtonTest : BunitContext
{
    private static readonly ClaimIdentification ClaimId = new(new ProjectIdentification(123), 45);

    private readonly FakeChangeFeeClient client = new();
    private readonly BunitJSModuleInterop joinDialogModule;

    public ChangeFeeButtonTest()
    {
        Services.AddSingleton<IChangeFeeClient>(client);
        Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        joinDialogModule = JSInterop.SetupModule("/_content/JoinRpg.Common.WebComponents/component-interop.js");
        joinDialogModule.SetupVoid("showModal", _ => true);
        joinDialogModule.SetupVoid("closeModal", _ => true);
    }

    private static ChangeFeeView CreateView(bool hasFieldsWithFee = false)
        => new(ClaimId, 500, [300, 500, 800], hasFieldsWithFee);

    private IRenderedComponent<ChangeFeeButton> RenderButton(bool hasFieldsWithFee = false)
    {
        var cut = Render<ChangeFeeButton>(parameters => parameters.Add(c => c.View, CreateView(hasFieldsWithFee)));
        cut.FindAll("button").First(b => b.TextContent.Contains("Изменить вручную")).Click();
        return cut;
    }

    private static void Type(IRenderedComponent<ChangeFeeButton> cut, string text)
        => cut.Find("dialog input").Change(text);

    private static AngleSharp.Dom.IElement SaveButton(IRenderedComponent<ChangeFeeButton> cut)
        => cut.FindAll(".join-dialog-footer button").First(b => b.TextContent.Contains("Изменить"));

    /// <summary>
    /// Клик по стандартной кнопке диалога: кнопка закрывает окно (closeModal),
    /// но событие close у dialog в bUnit не стреляет — дергаем его вручную.
    /// </summary>
    private async Task ConfirmAsync(IRenderedComponent<ChangeFeeButton> cut)
    {
        await SaveButton(cut).ClickAsync(new MouseEventArgs());
        await cut.Find("dialog").TriggerEventAsync("onclose", EventArgs.Empty);
    }

    private void ShouldReload()
        => Services.GetRequiredService<BunitNavigationManager>().History.ShouldHaveSingleItem().Options.ForceLoad.ShouldBeTrue();

    private void ShouldNotReload() => Services.GetRequiredService<BunitNavigationManager>().History.ShouldBeEmpty();

    [Fact]
    public void ShowsDialogContent()
    {
        var cut = RenderButton();

        var dialog = cut.Find("dialog");
        dialog.TextContent.ShouldContain("Изменение суммы взноса");
        dialog.TextContent.ShouldContain("будет назначен вручную");
        dialog.TextContent.ShouldContain("Новый размер взноса");
        cut.Find("dialog input").GetAttribute("value").ShouldBe("500");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WarningShownOnlyWithFieldsWithFee(bool hasFieldsWithFee)
    {
        var cut = RenderButton(hasFieldsWithFee);

        if (hasFieldsWithFee)
        {
            cut.Find("dialog").TextContent.ShouldContain("модификаторов от полей");
        }
        else
        {
            cut.Find("dialog").TextContent.ShouldNotContain("модификаторов от полей");
        }
    }

    [Fact]
    public async Task SubmitSendsParsedValue()
    {
        var cut = RenderButton();
        Type(cut, "750");

        await ConfirmAsync(cut);

        cut.WaitForAssertion(() => client.Calls.ShouldBe([750]));
        ShouldReload();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("-50")]
    public async Task InvalidInputShouldNotCallClient(string value)
    {
        var cut = RenderButton();
        Type(cut, value);

        await ConfirmAsync(cut);

        client.Calls.ShouldBeEmpty();
        cut.WaitForAssertion(() =>
        {
            cut.Find("dialog").TextContent.ShouldContain("Введите корректную сумму");
            // Диалог закрыла кнопка и тут же переоткрылся с ошибкой
            joinDialogModule.Invocations["showModal"].Count.ShouldBe(2);
        });
        ShouldNotReload();
    }

    /// <summary>При отказе сервера диалог переоткрывается: введённое значение не теряется.</summary>
    [Fact]
    public async Task ShouldKeepValueAfterServerRejection()
    {
        client.FailWith = new ServerProblemException("Недостаточно прав для этого действия");
        var cut = RenderButton();
        Type(cut, "750");

        await ConfirmAsync(cut);

        cut.WaitForAssertion(() =>
        {
            cut.Find("dialog").TextContent.ShouldContain("Недостаточно прав");
            // Диалог закрыла кнопка и тут же переоткрылся с ошибкой
            joinDialogModule.Invocations["showModal"].Count.ShouldBe(2);
        });
        cut.Find("dialog input").GetAttribute("value").ShouldBe("750");
        ShouldNotReload();
    }

    [Fact]
    public async Task ShouldNotShowMessageOfUnexpectedError()
    {
        client.FailWith = new HttpRequestException("Response status code does not indicate success: 502 (Bad Gateway).");
        var cut = RenderButton();

        await ConfirmAsync(cut);

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось сохранить"));
        cut.Markup.ShouldNotContain("502");
    }

    /// <summary>По умолчанию кнопка XS — так она стоит в плотных финансовых таблицах рядом с btn-xs.</summary>
    [Fact]
    public void DefaultSizeIsExtraSmall()
    {
        var cut = Render<ChangeFeeButton>(parameters => parameters.Add(c => c.View, CreateView()));

        cut.Find(".join-btn").ClassList.ShouldContain("join-btn--xs");
    }

    private class FakeChangeFeeClient : IChangeFeeClient
    {
        public List<int> Calls { get; } = [];
        public Exception? FailWith { get; set; }

        public Task ChangeFee(ClaimIdentification claimId, ChangeFeeRequest request)
        {
            claimId.ShouldBe(ClaimId);
            if (FailWith is not null)
            {
                throw FailWith;
            }
            Calls.Add(request.FeeValue);
            return Task.CompletedTask;
        }
    }
}
