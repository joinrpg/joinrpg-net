using AngleSharp.Dom;
using Bunit;
using JoinRpg.Common.WebComponents;
using JoinRpg.Web.AdminTools.PscbPayments;
using JoinRpg.XGameApi.Contract.Pscb;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.AdminTools.Test.PscbPayments;

/// <summary>
/// Остров списка платежей ПСКБ: кнопка восстановления потерянных строк (ADR028).
/// </summary>
public class PscbPaymentsPanelTest : BunitContext
{
    private static readonly DateTimeOffset At = new(2026, 10, 10, 12, 0, 0, TimeSpan.FromHours(3));

    private readonly FakeListClient listClient = new();
    private readonly FakeRestoreClient restoreClient = new();

    public PscbPaymentsPanelTest()
    {
        Services.AddSingleton<IPscbPaymentsClient>(listClient);
        Services.AddSingleton<IPscbPaymentsRestoreClient>(restoreClient);
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        // Диалог подтверждения JoinButton грузит JS-модуль и открывает <dialog> через JS.
        var joinDialogModule = JSInterop.SetupModule("/_content/JoinRpg.Common.WebComponents/component-interop.js");
        joinDialogModule.SetupVoid("showModal", _ => true);
        joinDialogModule.SetupVoid("closeModal", _ => true);
    }

    private static PscbPaymentListRow Row(PscbPaymentMatchState? matchState, string orderId, string? blockReason = null)
        => new()
        {
            OrderId = orderId,
            Amount = 500,
            StatusDate = At,
            MatchState = matchState,
            MatchStateText = matchState?.ToString(),
            RestoreBlockReason = blockReason,
        };

    private static PscbPaymentsListResult List(params PscbPaymentListRow[] rows)
        => new() { IsSuccess = true, RequestCount = 1, Payments = rows };

    private IRenderedComponent<PscbPaymentsPanel> RenderPanel() => Render<PscbPaymentsPanel>();

    /// <summary>Нажимает «Запросить список» и ждёт, пока список загрузится.</summary>
    private void LoadList(IRenderedComponent<PscbPaymentsPanel> cut, int expectedRequests = 1)
    {
        Button(cut, "Запросить список").Click();
        cut.WaitForAssertion(() => listClient.Requests.ShouldBe(expectedRequests));
    }

    private static IElement Button(IRenderedComponent<PscbPaymentsPanel> cut, string text)
        => cut.FindAll("button").Single(b => b.TextContent.Contains(text) && b.Closest("dialog") is null);

    /// <summary>
    /// Нажимает кнопку в диалоге подтверждения и закрывает его — как в TypedDropdownButtonTest.
    /// Кнопка подтверждения носит ту же подпись, что и вызвавшая диалог кнопка.
    /// </summary>
    private async Task AnswerConfirmationAsync(IRenderedComponent<PscbPaymentsPanel> cut, string buttonText)
    {
        var dialog = cut.Find("dialog");
        await dialog.QuerySelectorAll("button").Single(b => b.TextContent.Contains(buttonText)).ClickAsync(new MouseEventArgs());
        await dialog.TriggerEventAsync("onclose", EventArgs.Empty);
    }

    /// <summary>
    /// Кнопка появляется только когда в загруженном списке есть строки Lost или RefundMissing —
    /// независимо от RestoreBlockReason: что восстанавливать, решает сервер.
    /// </summary>
    [Fact]
    public void NoLostRows_RestoreButtonHidden()
    {
        listClient.Lists.Enqueue(List(
            Row(PscbPaymentMatchState.Settled, "0000000001"),
            Row(PscbPaymentMatchState.NotSettled, "0000000002"),
            Row(null, "0000000003")));

        var cut = RenderPanel();
        LoadList(cut);

        cut.FindAll("button").ShouldNotContain(b => b.TextContent.Contains("Восстановить потерянные"));
    }

    [Fact]
    public void LostRows_RestoreButtonVisible_ConfirmationCarriesTheirCount()
    {
        listClient.Lists.Enqueue(List(
            Row(PscbPaymentMatchState.Lost, "0000000001"),
            Row(PscbPaymentMatchState.RefundMissing, "0000000002"),
            Row(PscbPaymentMatchState.Lost, "0000000003", blockReason: "Проект не определён"),
            Row(PscbPaymentMatchState.Settled, "0000000004")));

        var cut = RenderPanel();
        LoadList(cut);

        Button(cut, "Восстановить потерянные").ShouldNotBeNull();
        cut.Markup.ShouldContain("Восстановить строк: 3");
    }

    [Fact]
    public async Task ConfirmationDeclined_RestoreNotCalled()
    {
        listClient.Lists.Enqueue(List(Row(PscbPaymentMatchState.Lost, "0000000001")));

        var cut = RenderPanel();
        LoadList(cut);
        Button(cut, "Восстановить потерянные").Click();

        // До ответа в диалоге запрос не уходит.
        restoreClient.Requests.ShouldBeEmpty();
        await AnswerConfirmationAsync(cut, "Отменить");

        restoreClient.Requests.ShouldBeEmpty();
    }

    /// <summary>
    /// Восстановление отправляет серверу ровно тот запрос, которым был загружен список, плюс
    /// orderId строки: сервер заново скачивает период и работает от свежих данных банка.
    /// </summary>
    [Fact]
    public async Task ConfirmationAccepted_SendsTheQueryTheListWasLoadedWith()
    {
        listClient.Lists.Enqueue(List(Row(PscbPaymentMatchState.Lost, "0000000001")));
        listClient.Lists.Enqueue(List(Row(PscbPaymentMatchState.Settled, "0000000001")));

        var cut = RenderPanel();
        LoadList(cut);
        Button(cut, "Восстановить потерянные").Click();
        await AnswerConfirmationAsync(cut, "Восстановить потерянные");

        cut.WaitForAssertion(() => restoreClient.Requests.Count.ShouldBe(1));
        // Список к этому моменту перезагружен после восстановления — сравниваем с первым запросом.
        restoreClient.Requests.ShouldBe([new PscbRestoreRowRequest(listClient.Queries[0], "0000000001")]);
    }

    /// <summary>
    /// Прогон идёт по строкам в порядке списка, по запросу на строку; заблокированные вердиктом
    /// строки не запрашиваются вовсе. Итог — сумма по строкам, список перезагружен.
    /// </summary>
    [Fact]
    public async Task RestoreSucceeded_SummaryShown_ListReloaded()
    {
        listClient.Lists.Enqueue(List(
            Row(PscbPaymentMatchState.Lost, "0000000001"),
            Row(PscbPaymentMatchState.RefundMissing, "0000000002"),
            Row(PscbPaymentMatchState.Lost, "0000000003"),
            Row(PscbPaymentMatchState.Lost, "0000000004", blockReason: "Проект не определён")));
        // После восстановления список перезагружается тем же запросом.
        listClient.Lists.Enqueue(List(
            Row(PscbPaymentMatchState.Settled, "0000000001"),
            Row(PscbPaymentMatchState.Settled, "0000000002"),
            Row(PscbPaymentMatchState.Settled, "0000000003"),
            Row(PscbPaymentMatchState.Lost, "0000000004", blockReason: "Проект не определён")));
        restoreClient.ResultsByOrderId["0000000001"] = new PscbRestoreRowResult
        {
            OrderId = "0000000001",
            Outcome = PscbRestoreRowOutcome.Restored,
            Message = "платёж восстановлен",
            RestoredPaymentsCount = 1,
            RestoredRefundsCount = 1,
        };
        restoreClient.ResultsByOrderId["0000000002"] = new PscbRestoreRowResult
        {
            OrderId = "0000000002",
            Outcome = PscbRestoreRowOutcome.Failed,
            Message = "номер занят другой операцией",
        };
        restoreClient.ResultsByOrderId["0000000003"] = new PscbRestoreRowResult
        {
            OrderId = "0000000003",
            Outcome = PscbRestoreRowOutcome.AlreadySettled,
            Message = "уже проведён",
        };

        var cut = RenderPanel();
        LoadList(cut);
        Button(cut, "Восстановить потерянные").Click();
        await AnswerConfirmationAsync(cut, "Восстановить потерянные");

        cut.WaitForAssertion(() => listClient.Requests.ShouldBe(2));
        var query = listClient.Queries[0];
        restoreClient.Requests.ShouldBe(
        [
            new PscbRestoreRowRequest(query, "0000000001"),
            new PscbRestoreRowRequest(query, "0000000002"),
            new PscbRestoreRowRequest(query, "0000000003"),
        ]);

        var summary = cut.Find(".pscb-restore-summary").TextContent;
        summary.ShouldContain("Восстановлено платежей: 1");
        summary.ShouldContain("возвратов: 1");
        summary.ShouldContain("Не восстановлено: 1");

        var rows = cut.FindAll("ul.pscb-restore-rows li").ToList();
        rows.Count.ShouldBe(3);
        rows[0].ClassName.ShouldBe("text-success");
        rows[0].TextContent.ShouldBe("0000000001: платёж восстановлен");
        rows[1].ClassName.ShouldBe("text-danger");
        rows[1].TextContent.ShouldBe("0000000002: номер занят другой операцией");
        rows[2].ClassName.ShouldBe("text-muted");
        rows[2].TextContent.ShouldBe("0000000003: уже проведён");
    }

    /// <summary>
    /// Отказ прогона (BadRequest контроллера — <see cref="ServerProblemException"/>) обрывает
    /// цикл: следующие строки не запрашиваются, текст виден в JoinAlert, загруженный список цел,
    /// а перезапроса списка нет — восстанавливать было нечего.
    /// </summary>
    [Fact]
    public async Task ServerProblemAbortsTheRun_ListIntact()
    {
        listClient.Lists.Enqueue(List(
            Row(PscbPaymentMatchState.Lost, "0000000001"),
            Row(PscbPaymentMatchState.Lost, "0000000002")));
        restoreClient.Failure = new ServerProblemException("Банк не вернул список платежей");

        var cut = RenderPanel();
        LoadList(cut);
        Button(cut, "Восстановить потерянные").Click();
        await AnswerConfirmationAsync(cut, "Восстановить потерянные");

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Банк не вернул список платежей"));
        restoreClient.Requests.Count.ShouldBe(1);
        listClient.Requests.ShouldBe(1);
        cut.Markup.ShouldContain("0000000001");
        cut.FindAll(".pscb-restore-summary").ShouldBeEmpty();
    }

    /// <summary>
    /// Сбой одной строки (не отказ прогона) не уносит остальные: исход попадает в отчёт,
    /// следующая строка запрашивается, а список перезагружается.
    /// </summary>
    [Fact]
    public async Task RowFailureDoesNotStopTheNextRow()
    {
        listClient.Lists.Enqueue(List(
            Row(PscbPaymentMatchState.Lost, "0000000001"),
            Row(PscbPaymentMatchState.Lost, "0000000002")));
        listClient.Lists.Enqueue(List(
            Row(PscbPaymentMatchState.Settled, "0000000002")));
        restoreClient.FailuresByOrderId["0000000001"] = new InvalidOperationException("База недоступна");

        var cut = RenderPanel();
        LoadList(cut);
        Button(cut, "Восстановить потерянные").Click();
        await AnswerConfirmationAsync(cut, "Восстановить потерянные");

        cut.WaitForAssertion(() => restoreClient.Requests.Count.ShouldBe(2));
        cut.WaitForAssertion(() => listClient.Requests.ShouldBe(2));

        var rows = cut.FindAll("ul.pscb-restore-rows li").ToList();
        rows.Count.ShouldBe(2);
        rows[0].ClassName.ShouldBe("text-danger");
        rows[0].TextContent.ShouldBe("0000000001: База недоступна");
        rows[1].ClassName.ShouldBe("text-success");
        rows[1].TextContent.ShouldBe("0000000002: платёж восстановлен");
    }

    private sealed class FakeListClient : IPscbPaymentsClient
    {
        public Queue<PscbPaymentsListResult> Lists { get; } = new();

        public List<PscbPaymentsQuery> Queries { get; } = [];

        public int Requests { get; private set; }

        public Task<PscbPaymentsListResult> GetPayments(PscbPaymentsQuery query)
        {
            Requests++;
            Queries.Add(query);
            return Task.FromResult(Lists.TryDequeue(out var list)
                ? list
                : new PscbPaymentsListResult { IsSuccess = true, RequestCount = 1 });
        }

        public Task<PscbPaymentDetailsResult> GetPaymentInfo(PscbPaymentDetailsQuery query)
            => throw new NotSupportedException();
    }

    private sealed class FakeRestoreClient : IPscbPaymentsRestoreClient
    {
        /// <summary>Результат по умолчанию для строк, чей исход тест не задал.</summary>
        public PscbRestoreRowResult Result { get; set; } = new()
        {
            Outcome = PscbRestoreRowOutcome.Restored,
            Message = "платёж восстановлен",
            RestoredPaymentsCount = 1,
        };

        /// <summary>Результаты отдельных строк — по orderId.</summary>
        public Dictionary<string, PscbRestoreRowResult> ResultsByOrderId { get; } = new(StringComparer.Ordinal);

        /// <summary>Исключение на каждый запрос — отказ всего прогона.</summary>
        public Exception? Failure { get; set; }

        /// <summary>Исключения отдельных строк — по orderId.</summary>
        public Dictionary<string, Exception> FailuresByOrderId { get; } = new(StringComparer.Ordinal);

        public List<PscbRestoreRowRequest> Requests { get; } = [];

        public Task<PscbRestoreRowResult> RestoreRow(PscbRestoreRowRequest request)
        {
            Requests.Add(request);
            if (Failure is { } failure)
            {
                throw failure;
            }

            if (FailuresByOrderId.TryGetValue(request.OrderId, out var rowFailure))
            {
                throw rowFailure;
            }

            var result = ResultsByOrderId.TryGetValue(request.OrderId, out var rowResult) ? rowResult : Result;
            return Task.FromResult(new PscbRestoreRowResult
            {
                OrderId = result.OrderId ?? request.OrderId,
                Outcome = result.Outcome,
                Message = result.Message,
                RestoredPaymentsCount = result.RestoredPaymentsCount,
                RestoredRefundsCount = result.RestoredRefundsCount,
            });
        }
    }
}
