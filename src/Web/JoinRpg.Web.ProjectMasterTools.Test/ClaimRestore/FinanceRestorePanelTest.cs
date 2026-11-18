using System.Text.Json;
using AngleSharp.Dom;
using Bunit;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.ProjectMasterTools.Test.ClaimRestore;

public class FinanceRestorePanelTest : BunitContext
{
    private static readonly ProjectIdentification ProjectId = new(1);
    private static readonly DateTimeOffset At = new(2026, 6, 10, 10, 0, 0, TimeSpan.Zero);

    private static readonly FinanceRestorePaymentTypeView Cash = new(10, "Наличные", "Мастер", IsActive: true);
    private static readonly FinanceRestorePaymentTypeView Card = new(11, "Карта", "Казначей", IsActive: true);
    private static readonly FinanceRestorePaymentTypeView MyCash = new(12, "Наличные", "Я", IsActive: true);

    private readonly FakeClient client = new();

    public FinanceRestorePanelTest()
    {
        Services.AddSingleton<IFinanceRestoreClient>(client);
        Services.AddSingleton<IUriLocator<ClaimIdentification>>(new FakeLocator());
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    private static FinanceRestoreOperationView Op(string key, int? suggested, bool canApply = true)
        => new(key, 100, IsRefund: false, 500, At, "Мастер", IsByPlayer: false, FinanceRestoreOperationStateView.Approved,
            suggested, [], canApply, canApply ? null : "Уже есть");

    private static FinanceRestoreRowViewModel Row(int claimId, bool exists = true, params FinanceRestoreOperationView[] operations)
        => new(
            new ClaimIdentification(ProjectId, claimId),
            exists,
            exists ? "Персонаж" : null,
            exists ? "Игрок" : null,
            exists ? operations : [],
            [],
            AlreadyExisting: 0,
            Conflicts: 0,
            Transfers: 0,
            OnlinePayments: 0,
            Unparsed: 0,
            exists ? [] : [FinanceRestoreWarningView.ClaimNotFound]);

    private static FinanceRestorePlanViewModel Plan(params FinanceRestoreRowViewModel[] rows)
        => new(ProjectId, null, [Cash, Card], rows, HasOwnCash: true);

    private IRenderedComponent<FinanceRestorePanel> RenderPanel() => Render<FinanceRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));

    private static IElement Button(IRenderedComponent<FinanceRestorePanel> cut, string text)
        => cut.FindAll("button").Single(b => b.TextContent.Contains(text));

    private static IElement ClaimRow(IRenderedComponent<FinanceRestorePanel> cut, int claimId)
        => cut.FindAll("tbody tr").Single(tr => tr.QuerySelector("a")?.TextContent == claimId.ToString());

    private static IElement Checkbox(IRenderedComponent<FinanceRestorePanel> cut, int claimId)
        => ClaimRow(cut, claimId).QuerySelector("input[type=checkbox]")!;

    /// <summary>Раскрывает операции заявки и возвращает select типа оплаты операции (по порядку).</summary>
    private static IElement TypeSelect(IRenderedComponent<FinanceRestorePanel> cut, int claimId, int index = 0)
    {
        var row = ClaimRow(cut, claimId);
        if (row.NextElementSibling?.QuerySelector("select") is null)
        {
            row.QuerySelector("button")!.Click();
        }
        return ClaimRow(cut, claimId).NextElementSibling!.QuerySelectorAll("select")[index];
    }

    [Fact]
    public void ByDefault_RowsWithoutTypeOrWithAmbiguousModeration_NotSelected()
    {
        client.Plans.Enqueue(Plan(
            Row(5, true, Op("a", Cash.PaymentTypeId), Op("b", Card.PaymentTypeId), Op("old", null, canApply: false)),
            Row(6, true, Op("c", Cash.PaymentTypeId), Op("d", null)),
            Row(7, true, Op("e", Cash.PaymentTypeId)) with { Warnings = [FinanceRestoreWarningView.ModerationAmbiguous] },
            Row(8, exists: false)));
        client.Plans.Enqueue(Plan());

        var cut = RenderPanel();
        Checkbox(cut, 6).HasAttribute("checked").ShouldBeFalse();
        Checkbox(cut, 7).HasAttribute("checked").ShouldBeFalse();
        Checkbox(cut, 8).HasAttribute("disabled").ShouldBeTrue();
        Button(cut, "Применить выбранные").Click();

        cut.WaitForAssertion(() => client.PlanRequests.ShouldBe(2));
        var request = client.Applied.ShouldHaveSingleItem();
        request.ClaimId.ClaimId.ShouldBe(5);
        // Неприменимая операция («уже есть») в запрос не попадает.
        request.PaymentTypes.ShouldBe(new Dictionary<string, int> { ["a"] = Cash.PaymentTypeId, ["b"] = Card.PaymentTypeId }, ignoreOrder: true);
    }

    [Fact]
    public void ManuallyCheckedRowWithoutType_SendsOnlyOperationsWithType()
    {
        client.Plans.Enqueue(Plan(Row(6, true, Op("c", Cash.PaymentTypeId), Op("d", null))));
        client.Plans.Enqueue(Plan());

        var cut = RenderPanel();
        Checkbox(cut, 6).Change(true);
        Button(cut, "Применить выбранные").Click();

        cut.WaitForAssertion(() => client.PlanRequests.ShouldBe(2));
        client.Applied.ShouldHaveSingleItem().PaymentTypes.ShouldBe(new Dictionary<string, int> { ["c"] = Cash.PaymentTypeId });
    }

    [Fact]
    public void UncheckedRow_NotApplied()
    {
        client.Plans.Enqueue(Plan(Row(5, true, Op("a", Cash.PaymentTypeId)), Row(7, true, Op("e", Cash.PaymentTypeId))));
        client.Plans.Enqueue(Plan());

        var cut = RenderPanel();
        Checkbox(cut, 5).Change(false);
        Button(cut, "Применить выбранные").Click();

        cut.WaitForAssertion(() => client.PlanRequests.ShouldBe(2));
        client.Applied.Select(r => r.ClaimId.ClaimId).ShouldBe([7]);
    }

    [Fact]
    public void SelectNoneThenAll_SelectsOnlyApplicable()
    {
        client.Plans.Enqueue(Plan(
            Row(5, true, Op("a", Cash.PaymentTypeId)),
            Row(6, true, Op("d", null)),
            Row(7, true, Op("old", Cash.PaymentTypeId, canApply: false)) with { Warnings = [FinanceRestoreWarningView.TransfersNotRestored] },
            Row(8, exists: false)));
        client.Plans.Enqueue(Plan());

        var cut = RenderPanel();
        Button(cut, "Снять все").Click();
        Button(cut, "Применить выбранные").HasAttribute("disabled").ShouldBeTrue();
        Button(cut, "Выбрать все").Click();
        Button(cut, "Применить выбранные").Click();

        cut.WaitForAssertion(() => client.PlanRequests.ShouldBe(2));
        client.Applied.Select(r => r.ClaimId.ClaimId).ShouldBe([5, 6]);
    }

    [Fact]
    public void TypeChosenInSelect_SentToApply()
    {
        client.Plans.Enqueue(Plan(Row(5, true, Op("a", Cash.PaymentTypeId)), Row(6, true, Op("d", null))));
        client.Plans.Enqueue(Plan());

        var cut = RenderPanel();
        TypeSelect(cut, 5).Change(Card.PaymentTypeId.ToString());
        // Выбрав тип у последней операции без типа, мастер получает строку отмеченной.
        TypeSelect(cut, 6).Change(Cash.PaymentTypeId.ToString());
        Checkbox(cut, 6).HasAttribute("checked").ShouldBeTrue();
        Button(cut, "Применить выбранные").Click();

        cut.WaitForAssertion(() => client.PlanRequests.ShouldBe(2));
        client.Applied.Select(r => (r.ClaimId.ClaimId, r.PaymentTypes.Single().Key, r.PaymentTypes.Single().Value))
            .ShouldBe([(5, "a", Card.PaymentTypeId), (6, "d", Cash.PaymentTypeId)]);
    }

    [Fact]
    public void TypeResetToNone_OperationNotSentAndRowUnchecked()
    {
        client.Plans.Enqueue(Plan(Row(5, true, Op("a", Cash.PaymentTypeId), Op("b", Cash.PaymentTypeId))));
        client.Plans.Enqueue(Plan());

        var cut = RenderPanel();
        TypeSelect(cut, 5, index: 1).Change("");
        Checkbox(cut, 5).HasAttribute("checked").ShouldBeFalse();
        Checkbox(cut, 5).Change(true);
        Button(cut, "Применить выбранные").Click();

        cut.WaitForAssertion(() => client.PlanRequests.ShouldBe(2));
        client.Applied.ShouldHaveSingleItem().PaymentTypes.Keys.ShouldBe(["a"]);
    }

    [Fact]
    public void CreateOwnCash_ReloadsPlan_KeepsMastersChoice()
    {
        client.Plans.Enqueue(Plan(Row(5, true, Op("a", null), Op("b", null))) with { HasOwnCash = false });
        // После создания: новый тип в списке, планировщик теперь предлагает его для обеих операций.
        client.Plans.Enqueue(Plan(Row(5, true, Op("a", MyCash.PaymentTypeId), Op("b", MyCash.PaymentTypeId))) with
        {
            PaymentTypes = [Cash, Card, MyCash],
        });
        client.Plans.Enqueue(Plan());

        var cut = RenderPanel();
        TypeSelect(cut, 5, index: 0).Change(Card.PaymentTypeId.ToString());
        Button(cut, "Создать «Наличные» на меня").Click();

        cut.WaitForAssertion(() => client.PlanRequests.ShouldBe(2));
        client.CashCreated.ShouldBe(1);
        cut.FindAll("button").ShouldNotContain(b => b.TextContent.Contains("Создать «Наличные»"));
        Checkbox(cut, 5).HasAttribute("checked").ShouldBeTrue();
        Button(cut, "Применить выбранные").Click();

        cut.WaitForAssertion(() => client.PlanRequests.ShouldBe(3));
        client.Applied.ShouldHaveSingleItem().PaymentTypes.ShouldBe(
            new Dictionary<string, int> { ["a"] = Card.PaymentTypeId, ["b"] = MyCash.PaymentTypeId }, ignoreOrder: true);
    }

    /// <summary>
    /// Повтор ключа в строке — случай «неоднозначный ключ»: строка раскрывается без исключения (повтор <c>@key</c>
    /// уронил бы остров), а операции с повтором неприменимы, даже если сервер их такими не пометил.
    /// </summary>
    [Fact]
    public void RepeatedKeys_RowExpands_OperationsNotApplicable()
    {
        var moderation = new FinanceRestoreModerationView("mod", 101, Approved: true, At, "Мастер", CanApply: false, "Неоднозначный ключ");
        client.Plans.Enqueue(Plan(Row(5, true, Op("dup", Cash.PaymentTypeId), Op("dup", Card.PaymentTypeId), Op("x", Cash.PaymentTypeId)) with
        {
            Moderations = [moderation, moderation],
        }));
        client.Plans.Enqueue(Plan());

        var cut = RenderPanel();
        var details = TypeSelect(cut, 5).ParentElement!.Closest("table")!;

        details.QuerySelectorAll("select").Length.ShouldBe(1);
        details.QuerySelectorAll("tbody tr").Count(tr => tr.TextContent.Contains("Не восстановится")).ShouldBe(2);
        cut.FindAll("table.restore-finance-moderations tbody tr").Count.ShouldBe(2);
        Button(cut, "Применить выбранные").Click();

        cut.WaitForAssertion(() => client.PlanRequests.ShouldBe(2));
        client.Applied.ShouldHaveSingleItem().PaymentTypes.ShouldBe(new Dictionary<string, int> { ["x"] = Cash.PaymentTypeId });
    }

    [Fact]
    public void FailedClaim_DoesNotStopOthers_ErrorShownAfterReload()
    {
        client.Plans.Enqueue(Plan(Row(5, true, Op("a", Cash.PaymentTypeId)), Row(7, true, Op("e", Cash.PaymentTypeId))));
        client.Plans.Enqueue(Plan(Row(5, true, Op("a", Cash.PaymentTypeId))));
        client.Failing.Add(5);

        var cut = RenderPanel();
        Button(cut, "Применить выбранные").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось по заявкам: 1"));
        client.Applied.Select(r => r.ClaimId.ClaimId).ShouldBe([5, 7]);
        cut.Markup.ShouldContain("Сбой заявки 5");
    }

    [Fact]
    public void ApplyFailedUnexpectedly_GenericTextInsteadOfExceptionMessage()
    {
        client.Plans.Enqueue(Plan(Row(5, true, Op("a", Cash.PaymentTypeId))));
        client.Plans.Enqueue(Plan(Row(5, true, Op("a", Cash.PaymentTypeId))));
        client.Broken.Add(5);

        var cut = RenderPanel();
        Button(cut, "Применить выбранные").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось по заявкам: 1"));
        cut.Markup.ShouldContain("Не удалось восстановить оплаты заявки — попробуйте ещё раз");
        cut.Markup.ShouldNotContain("Internal Server Error");
    }

    [Fact]
    public void CreateOwnCashRefused_ServerReasonShown()
    {
        client.Plans.Enqueue(Plan(Row(5, true, Op("a", null))) with { HasOwnCash = false });
        client.CashFailure = new ServerProblemException("Проект заблокирован, изменения запрещены");

        var cut = RenderPanel();
        Button(cut, "Создать «Наличные» на меня").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось создать тип оплаты «Наличные»: Проект заблокирован, изменения запрещены"));
    }

    [Fact]
    public void CreateOwnCashFailedUnexpectedly_GenericTextInsteadOfExceptionMessage()
    {
        client.Plans.Enqueue(Plan(Row(5, true, Op("a", null))) with { HasOwnCash = false });
        client.CashFailure = new HttpRequestException("Response status code does not indicate success: 500 (Internal Server Error).");

        var cut = RenderPanel();
        Button(cut, "Создать «Наличные» на меня").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось создать тип оплаты «Наличные»: произошла ошибка, попробуйте ещё раз"));
        cut.Markup.ShouldNotContain("Internal Server Error");
    }

    [Fact]
    public void AfterRestore_RowsWithNothingLeft_CollapsedIntoCounter()
    {
        client.Plans.Enqueue(Plan(Row(5, true, Op("a", Cash.PaymentTypeId)), Row(7, true, Op("e", Cash.PaymentTypeId))));
        var restored = Plan(
            Row(5, true, Op("a", Cash.PaymentTypeId, canApply: false)) with { AlreadyExisting = 1 },
            Row(7, true, Op("e", Cash.PaymentTypeId, canApply: false)) with { AlreadyExisting = 1 });
        client.Plans.Enqueue(restored);
        client.Plans.Enqueue(restored);

        var cut = RenderPanel();
        Button(cut, "Применить выбранные").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Восстановлено операций и подтверждений: 2"));
        cut.Markup.ShouldContain("Восстановлено: 1");

        Button(cut, "Обновить план").Click();

        cut.WaitForAssertion(() => client.PlanRequests.ShouldBe(3));
        cut.Markup.ShouldContain("Нечего восстанавливать");
        cut.Markup.ShouldContain("Уже восстановлено заявок: 2");
        cut.FindAll("table").ShouldBeEmpty();
    }

    /// <summary>
    /// Описания операций в пояснении к решению и в «Пропущено» собирает панель — время в них то же, что в
    /// таблице, в поясе браузера (#5647).
    /// </summary>
    [Fact]
    public void TextsWithTime_UseSameTimeAsTable()
    {
        var operationAt = new DateTimeOffset(2026, 6, 10, 22, 30, 0, TimeSpan.Zero);
        var shown = operationAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
        var moderation = new FinanceRestoreModerationView("mod", 101, Approved: true, At, "Мастер", CanApply: true, Note: null,
            new FinanceRestoreEntryRef(FinanceRestoreEntryKind.Payment, 500, operationAt));
        var row = Row(5, true, Op("a", Cash.PaymentTypeId) with { CreatedAt = operationAt }) with { Moderations = [moderation] };
        client.Plans.Enqueue(Plan(row));
        client.Plans.Enqueue(Plan(row));
        client.Result = new FinanceRestoreApplyResult(0, 0, 0, 0,
            [new FinanceRestoreSkippedView(new FinanceRestoreEntryRef(FinanceRestoreEntryKind.Refund, 200, operationAt), "не выбран тип оплаты")]);

        var cut = RenderPanel();
        var details = TypeSelect(cut, 5).ParentElement!.Closest("td[colspan]")!;
        details.QuerySelector("table.restore-finance-operations tbody tr")!.TextContent.ShouldContain(shown);
        details.QuerySelector("table.restore-finance-moderations tbody tr")!.TextContent
            .ShouldContain($"К операции «оплата 500 от {shown}» — восстановится вместе с ней");
        Button(cut, "Применить выбранные").Click();

        cut.WaitForAssertion(() => client.PlanRequests.ShouldBe(2));
        cut.Markup.ShouldContain($"Пропущено: возврат 200 от {shown}: не выбран тип оплаты");
    }

    /// <summary>Решение, чья операция не вставилась, — не «занятый номер»: итог называет их раздельно (#5647).</summary>
    [Fact]
    public void ApplyResult_ConflictsAndParentsNotRestored_ShownSeparately()
    {
        client.Plans.Enqueue(Plan(Row(5, true, Op("a", Cash.PaymentTypeId))));
        client.Plans.Enqueue(Plan(Row(5, true, Op("a", Cash.PaymentTypeId))));
        client.Result = new FinanceRestoreApplyResult(Inserted: 0, AlreadyExisted: 0, Conflicts: 1, ParentsNotRestored: 2, []);

        var cut = RenderPanel();
        Button(cut, "Применить выбранные").Click();

        cut.WaitForAssertion(() => client.PlanRequests.ShouldBe(2));
        cut.Markup.ShouldContain("не восстановлены из-за занятых номеров: 1");
        cut.Markup.ShouldContain("решения не восстановлены — их операция не восстановилась: 2");
    }

    [Fact]
    public void ProjectNotBlocked_ShowsExplanation()
    {
        client.Plans.Enqueue(new FinanceRestorePlanViewModel(ProjectId, ClaimRestoreUnavailableReason.ProjectNotBlocked, [], [], HasOwnCash: false));

        var cut = RenderPanel();

        cut.Markup.ShouldContain("только пока проект заблокирован");
        cut.FindAll("table").ShouldBeEmpty();
        cut.FindAll("button").ShouldNotContain(b => b.TextContent.Contains("Создать «Наличные»"));
    }

    [Fact]
    public void Models_SurviveJson()
    {
        var plan = Plan(Row(5, true, Op("id:100", Cash.PaymentTypeId) with { CandidatePaymentTypeIds = [10, 11] }) with
        {
            Moderations = [new FinanceRestoreModerationView("id:101", 101, Approved: true, At, "Мастер", CanApply: true, Note: null,
                new FinanceRestoreEntryRef(FinanceRestoreEntryKind.Payment, 500, At))],
            Warnings = [FinanceRestoreWarningView.ModerationAmbiguous],
        }) with
        { UnavailableReason = ClaimRestoreUnavailableReason.NoAccess };
        var request = new FinanceRestoreApplyRequest(new ClaimIdentification(ProjectId, 5), new Dictionary<string, int> { ["id:100"] = 10 });
        var result = new FinanceRestoreApplyResult(1, 2, 3, 4,
            [new FinanceRestoreSkippedView(new FinanceRestoreEntryRef(FinanceRestoreEntryKind.Payment, 500, At), "не выбран тип оплаты")]);

        RoundTrip(plan).ShouldBe(JsonSerializer.Serialize(plan));
        RoundTrip(request).ShouldBe(JsonSerializer.Serialize(request));
        RoundTrip(result).ShouldBe(JsonSerializer.Serialize(result));
        JsonSerializer.Deserialize<FinanceRestoreApplyRequest>(JsonSerializer.Serialize(request))!.PaymentTypes["id:100"].ShouldBe(10);

        static string RoundTrip<T>(T value) => JsonSerializer.Serialize(JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value)));
    }

    private sealed class FakeClient : IFinanceRestoreClient
    {
        public Queue<FinanceRestorePlanViewModel> Plans { get; } = new();
        public List<FinanceRestoreApplyRequest> Applied { get; } = [];
        public HashSet<int> Failing { get; } = [];
        public int PlanRequests { get; private set; }
        public int CashCreated { get; private set; }

        public Task<FinanceRestorePlanViewModel> GetPlan(ProjectIdentification projectId)
        {
            PlanRequests++;
            return Plans.TryDequeue(out var plan) ? Task.FromResult(plan) : throw new HttpRequestException("нет связи");
        }

        /// <summary>Заявки, применение которых упадёт не отказом, а сбоем (например, 500).</summary>
        public HashSet<int> Broken { get; } = [];

        /// <summary>Чем упадёт создание «Наличных»: отказ сервера или сбой.</summary>
        public Exception? CashFailure { get; set; }

        /// <summary>Итог применения вместо стандартного «вставлено по числу выбранных типов».</summary>
        public FinanceRestoreApplyResult? Result { get; set; }

        public Task<FinanceRestoreApplyResult> Apply(FinanceRestoreApplyRequest request)
        {
            Applied.Add(request);
            if (Broken.Contains(request.ClaimId.ClaimId))
            {
                throw new HttpRequestException("Response status code does not indicate success: 500 (Internal Server Error).");
            }
            return Failing.Contains(request.ClaimId.ClaimId)
                ? throw new ServerProblemException($"Сбой заявки {request.ClaimId.ClaimId}")
                : Task.FromResult(Result ?? new FinanceRestoreApplyResult(request.PaymentTypes.Count, 0, 0, 0, []));
        }

        public Task CreateOwnCash(ProjectIdentification projectId)
        {
            CashCreated++;
            return CashFailure is { } failure ? throw failure : Task.CompletedTask;
        }
    }

    private sealed class FakeLocator : IUriLocator<ClaimIdentification>
    {
        public Uri GetUri(ClaimIdentification target) => new($"/claim/{target.ClaimId}", UriKind.Relative);
    }
}
