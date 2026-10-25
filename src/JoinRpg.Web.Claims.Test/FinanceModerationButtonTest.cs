using Bunit.TestDoubles;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Claims;
using JoinRpg.Web.Claims.Comments;
using JoinRpg.Web.Claims.Finance;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Web.Claims.Test;

public class FinanceModerationButtonTest : BunitContext
{
    private static readonly FinanceOperationIdentification OperationId =
        new(new ClaimIdentification(new ProjectIdentification(123), 45), 678);

    private readonly FakeFinanceModerationClient client = new();

    public FinanceModerationButtonTest()
    {
        Services.AddSingleton<IFinanceModerationClient>(client);
        Services.AddSingleton<IClaimCommentClient>(client);
        Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        var joinDialogModule = JSInterop.SetupModule("/_content/JoinRpg.Common.WebComponents/component-interop.js");
        joinDialogModule.SetupVoid("showModal", _ => true);
        joinDialogModule.SetupVoid("closeModal", _ => true);
    }

    private IRenderedComponent<FinanceModerationButton> RenderButton()
    {
        var cut = Render<FinanceModerationButton>(parameters => parameters.Add(
            c => c.Operation,
            new FinanceModerationOperationView(OperationId, "Оплата", 500, new DateOnly(2026, 10, 25), "Наличными — Мастер")));
        cut.FindAll("button").First(b => b.TextContent.Contains("Подтвердить / отклонить")).Click();
        return cut;
    }

    private static void Choose(IRenderedComponent<FinanceModerationButton> cut, FinanceModerationAction action)
        => cut.Find($"dialog input[type=radio][value={action}]").Change(action.ToString());

    private static void Type(IRenderedComponent<FinanceModerationButton> cut, string text)
        => cut.Find("dialog textarea").Change(text);

    private static void HideFromPlayer(IRenderedComponent<FinanceModerationButton> cut)
        => cut.Find("dialog input[type=checkbox]").Change(true);

    /// <summary>Кнопка отправки — последняя в футере, её подпись зависит от выбранного действия.</summary>
    private static AngleSharp.Dom.IElement SubmitButton(IRenderedComponent<FinanceModerationButton> cut)
        => cut.Find("dialog .join-dialog-footer").QuerySelectorAll("button").Last();

    private static Task Submit(IRenderedComponent<FinanceModerationButton> cut)
        => SubmitButton(cut).ClickAsync(new MouseEventArgs());

    private void ShouldReload()
        => Services.GetRequiredService<BunitNavigationManager>().History.ShouldHaveSingleItem().Options.ForceLoad.ShouldBeTrue();

    private void ShouldNotReload() => Services.GetRequiredService<BunitNavigationManager>().History.ShouldBeEmpty();

    [Fact]
    public void ShowsOperationSummary()
    {
        var cut = RenderButton();

        var dialog = cut.Find("dialog").TextContent;
        dialog.ShouldContain("Оплата");
        dialog.ShouldContain("25.10.2026");
        dialog.ShouldContain("Наличными — Мастер");
    }

    [Theory]
    [InlineData(FinanceModerationAction.Approve, "Подтвердить")]
    [InlineData(FinanceModerationAction.Decline, "Отклонить")]
    [InlineData(FinanceModerationAction.Comment, "Отправить комментарий")]
    public void SubmitButtonFollowsChosenAction(FinanceModerationAction action, string label)
    {
        var cut = RenderButton();

        Choose(cut, action);

        SubmitButton(cut).TextContent.ShouldContain(label);
    }

    /// <summary>
    /// Подтверждение и отклонение игрок видит всегда — галку «Только другим мастерам» предлагаем только для комментария.
    /// </summary>
    [Fact]
    public void HideFromPlayerIsOfferedOnlyForComment()
    {
        var cut = RenderButton();
        cut.FindAll("dialog input[type=checkbox]").ShouldBeEmpty();

        Choose(cut, FinanceModerationAction.Comment);

        cut.FindAll("dialog input[type=checkbox]").ShouldHaveSingleItem();
    }

    [Fact]
    public async Task ApproveIsDefault_AndWorksWithoutText()
    {
        var cut = RenderButton();

        await Submit(cut);

        client.Calls.ShouldBe([("approve", "")]);
        ShouldReload();
    }

    [Fact]
    public async Task DeclineShouldSendText()
    {
        var cut = RenderButton();
        Choose(cut, FinanceModerationAction.Decline);
        Type(cut, "  Денег не пришло ");

        await Submit(cut);

        client.Calls.ShouldBe([("decline", "Денег не пришло")]);
        ShouldReload();
    }

    [Theory]
    [InlineData(FinanceModerationAction.Decline)]
    [InlineData(FinanceModerationAction.Comment)]
    public async Task WithoutTextShouldNotCallClient(FinanceModerationAction action)
    {
        var cut = RenderButton();
        Choose(cut, action);

        await Submit(cut);

        client.Calls.ShouldBeEmpty();
        cut.Find("dialog").TextContent.ShouldContain("Напишите");
        ShouldNotReload();
    }

    [Fact]
    public async Task CommentShouldPassVisibility()
    {
        var cut = RenderButton();
        Choose(cut, FinanceModerationAction.Comment);
        Type(cut, "Проверю выписку");
        HideFromPlayer(cut);

        await Submit(cut);

        client.Calls.ShouldBe([("comment", "Проверю выписку")]);
        client.HiddenFromPlayer.ShouldBe(true);
        ShouldReload();
    }

    [Fact]
    public async Task CommentShouldBeVisibleToPlayerByDefault()
    {
        var cut = RenderButton();
        Choose(cut, FinanceModerationAction.Comment);
        Type(cut, "Проверю выписку");

        await Submit(cut);

        client.HiddenFromPlayer.ShouldBe(false);
    }

    /// <summary>
    /// Пока запрос в полёте, второй клик не должен отправить решение повторно.
    /// </summary>
    [Fact]
    public async Task ButtonsAreDisabledWhileRequestIsInFlight()
    {
        var pending = new TaskCompletionSource();
        client.Pending = pending.Task;
        var cut = RenderButton();

        var submit = Submit(cut);

        cut.WaitForAssertion(() => cut.Find("dialog .join-dialog-footer").QuerySelectorAll("button")
            .ShouldAllBe(b => b.HasAttribute("disabled")));
        pending.SetResult();
        await submit;
        client.Calls.ShouldBe([("approve", "")]);
    }

    /// <summary>
    /// Ради этого кнопка диалога своя: при отказе окно не закрывается и введённый текст не теряется.
    /// </summary>
    [Fact]
    public async Task ShouldKeepTextAfterServerRejection()
    {
        client.FailWith = new ServerProblemException("Недостаточно прав для этого действия");
        var cut = RenderButton();
        Choose(cut, FinanceModerationAction.Decline);
        Type(cut, "Денег не пришло");

        await Submit(cut);

        cut.WaitForAssertion(() => cut.Find("dialog").TextContent.ShouldContain("Недостаточно прав"));
        cut.Find("dialog textarea").TextContent.ShouldBe("Денег не пришло");
        SubmitButton(cut).HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public async Task ShouldShowServerRejectionReasonInDialog()
    {
        client.FailWith = new ServerProblemException("Эта операция уже подтверждена или отклонена. Обновите страницу.");
        var cut = RenderButton();

        await Submit(cut);

        cut.WaitForAssertion(() => cut.Find("dialog").TextContent.ShouldContain("уже подтверждена или отклонена"));
        ShouldNotReload();
    }

    [Fact]
    public async Task ShouldNotShowMessageOfUnexpectedError()
    {
        client.FailWith = new HttpRequestException("Response status code does not indicate success: 502 (Bad Gateway).");
        var cut = RenderButton();

        await Submit(cut);

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось сохранить"));
        cut.Markup.ShouldNotContain("Bad Gateway");
    }

    /// <summary>Оба клиента диалога: решение по операции и простой комментарий к ней.</summary>
    private class FakeFinanceModerationClient : IFinanceModerationClient, IClaimCommentClient
    {
        public List<(string Action, string? Text)> Calls { get; } = [];
        public bool? HiddenFromPlayer { get; private set; }
        public Exception? FailWith { get; set; }
        public Task? Pending { get; set; }

        public Task AddComment(AddClaimCommentRequest request)
        {
            request.ClaimId.ShouldBe(OperationId.ClaimId);
            // Комментарий к операции — ответ на комментарий, в котором она заведена
            request.ParentCommentId.ShouldBe(new ClaimCommentIdentification(OperationId.ClaimId, OperationId.FinanceOperationId));
            HiddenFromPlayer = !request.IsVisibleToPlayer;
            return Record(OperationId, "comment", request.CommentText);
        }

        public Task Approve(FinanceOperationIdentification operationId, FinanceModerationDecisionRequest request)
            => Record(operationId, "approve", request.CommentText);

        public Task Decline(FinanceOperationIdentification operationId, FinanceModerationDecisionRequest request)
            => Record(operationId, "decline", request.CommentText);

        private async Task Record(FinanceOperationIdentification operationId, string action, string? text)
        {
            operationId.ShouldBe(OperationId);
            if (Pending is not null)
            {
                await Pending;
            }
            if (FailWith is not null)
            {
                throw FailWith;
            }
            Calls.Add((action, text));
        }
    }
}
