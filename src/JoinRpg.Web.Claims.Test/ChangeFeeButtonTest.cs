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

    public ChangeFeeButtonTest()
    {
        Services.AddSingleton<IChangeFeeClient>(client);
        Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        var joinDialogModule = JSInterop.SetupModule("/_content/JoinRpg.Common.WebComponents/component-interop.js");
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

    /// <summary>Кнопка отправки — последняя в футере.</summary>
    private static AngleSharp.Dom.IElement SubmitButton(IRenderedComponent<ChangeFeeButton> cut)
        => cut.Find("dialog .join-dialog-footer").QuerySelectorAll("button").Last();

    private static Task Submit(IRenderedComponent<ChangeFeeButton> cut)
        => SubmitButton(cut).ClickAsync(new MouseEventArgs());

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

        await Submit(cut);

        client.Calls.ShouldBe([750]);
        ShouldReload();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("-50")]
    public async Task InvalidInputShouldNotCallClient(string value)
    {
        var cut = RenderButton();
        Type(cut, value);

        await Submit(cut);

        client.Calls.ShouldBeEmpty();
        cut.Find("dialog").TextContent.ShouldContain("Введите корректную сумму");
        ShouldNotReload();
    }

    /// <summary>Пока запрос в полёте, второй клик не должен отправить значение повторно.</summary>
    [Fact]
    public async Task ButtonsAreDisabledWhileRequestIsInFlight()
    {
        var pending = new TaskCompletionSource();
        client.Pending = pending.Task;
        var cut = RenderButton();
        Type(cut, "750");

        var submit = Submit(cut);

        cut.WaitForAssertion(() => cut.Find("dialog .join-dialog-footer").QuerySelectorAll("button")
            .ShouldAllBe(b => b.HasAttribute("disabled")));
        pending.SetResult();
        await submit;
        client.Calls.ShouldBe([750]);
    }

    /// <summary>Ради этого кнопка диалога своя: при отказе окно не закрывается и введённое значение не теряется.</summary>
    [Fact]
    public async Task ShouldKeepValueAfterServerRejection()
    {
        client.FailWith = new ServerProblemException("Недостаточно прав для этого действия");
        var cut = RenderButton();
        Type(cut, "750");

        await Submit(cut);

        cut.WaitForAssertion(() => cut.Find("dialog").TextContent.ShouldContain("Недостаточно прав"));
        cut.Find("dialog input").GetAttribute("value").ShouldBe("750");
        SubmitButton(cut).HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public async Task ShouldNotShowMessageOfUnexpectedError()
    {
        client.FailWith = new HttpRequestException("Response status code does not indicate success: 502 (Bad Gateway).");
        var cut = RenderButton();

        await Submit(cut);

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
        public Task? Pending { get; set; }

        public async Task ChangeFee(ClaimIdentification claimId, ChangeFeeRequest request)
        {
            claimId.ShouldBe(ClaimId);
            if (Pending is not null)
            {
                await Pending;
            }
            if (FailWith is not null)
            {
                throw FailWith;
            }
            Calls.Add(request.FeeValue);
        }
    }
}
