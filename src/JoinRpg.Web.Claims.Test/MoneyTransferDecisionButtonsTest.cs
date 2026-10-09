using Bunit.TestDoubles;
using JoinRpg.DomainTypes;
using JoinRpg.Web.Claims.Finance;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Web.Claims.Test;

public class MoneyTransferDecisionButtonsTest : BunitContext
{
    private static readonly MoneyTransferIdentification TransferId = new(new ProjectIdentification(123), 45);

    private readonly FakeMoneyTransferClient client = new();

    public MoneyTransferDecisionButtonsTest()
    {
        Services.AddSingleton<IMoneyTransferClient>(client);
        Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        var joinDialogModule = JSInterop.SetupModule("/_content/JoinRpg.Common.WebComponents/component-interop.js");
        joinDialogModule.SetupVoid("showModal", _ => true);
        joinDialogModule.SetupVoid("closeModal", _ => true);
    }

    private IRenderedComponent<MoneyTransferDecisionButtons> RenderButtons()
        => Render<MoneyTransferDecisionButtons>(parameters => parameters.Add(c => c.TransferId, TransferId));

    /// <summary>
    /// Жмём кнопку на строке, затем такую же кнопку в окне подтверждения.
    /// </summary>
    private static async Task ClickAndConfirm(IRenderedComponent<MoneyTransferDecisionButtons> cut, string label)
    {
        cut.FindAll("button").First(b => b.TextContent.Contains(label)).Click();

        var dialog = cut.FindAll("dialog").Single(d => d.TextContent.Contains($"{label} перевод денег?"));
        var confirmButton = dialog.QuerySelectorAll("button").Single(b => b.TextContent.Contains(label));
        await confirmButton.ClickAsync(new MouseEventArgs());
        await dialog.TriggerEventAsync("onclose", EventArgs.Empty);
    }

    [Fact]
    public async Task ApproveShouldCallClientAndReloadPage()
    {
        var cut = RenderButtons();

        await ClickAndConfirm(cut, "Подтвердить");

        client.Approved.ShouldBe([TransferId]);
        client.Declined.ShouldBeEmpty();
        Services.GetRequiredService<BunitNavigationManager>().History.ShouldHaveSingleItem()
            .Options.ForceLoad.ShouldBeTrue();
        cut.Markup.ShouldNotContain("Не удалось");
    }

    [Fact]
    public async Task DeclineShouldCallClient()
    {
        var cut = RenderButtons();

        await ClickAndConfirm(cut, "Отклонить");

        client.Declined.ShouldBe([TransferId]);
        client.Approved.ShouldBeEmpty();
    }

    [Fact]
    public async Task ShouldShowErrorWhenServerFails()
    {
        client.FailWith = new HttpRequestException("500");
        var cut = RenderButtons();

        await ClickAndConfirm(cut, "Подтвердить");

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось сохранить решение по переводу"));
    }

    private class FakeMoneyTransferClient : IMoneyTransferClient
    {
        public List<MoneyTransferIdentification> Approved { get; } = [];
        public List<MoneyTransferIdentification> Declined { get; } = [];
        public Exception? FailWith { get; set; }

        public Task Approve(MoneyTransferIdentification transferId) => Record(Approved, transferId);

        public Task Decline(MoneyTransferIdentification transferId) => Record(Declined, transferId);

        private Task Record(List<MoneyTransferIdentification> list, MoneyTransferIdentification transferId)
        {
            if (FailWith is not null)
            {
                return Task.FromException(FailWith);
            }
            list.Add(transferId);
            return Task.CompletedTask;
        }
    }
}
