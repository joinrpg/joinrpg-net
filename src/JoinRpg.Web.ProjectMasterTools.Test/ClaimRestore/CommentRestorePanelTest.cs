using AngleSharp.Dom;
using Bunit;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.ProjectMasterTools.Test.ClaimRestore;

public class CommentRestorePanelTest : BunitContext
{
    private static readonly ProjectIdentification ProjectId = new(1);
    private static readonly DateTimeOffset At = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeClient client = new();

    public CommentRestorePanelTest()
    {
        Services.AddSingleton<ICommentRestoreClient>(client);
        Services.AddSingleton<IUriLocator<ClaimIdentification>>(new FakeLocator());
    }

    private static CommentRestoreCommentView Comment(string text)
        => new(100, At, "Мастер", IsCommentByPlayer: false, IsVisibleToPlayer: true, VisibilityQuestionable: false, null, $"<p>{text}</p>");

    private static CommentRestoreRowViewModel Row(int claimId, int comments, bool exists = true)
        => new(
            new ClaimIdentification(ProjectId, claimId),
            exists,
            exists ? "Персонаж" : null,
            exists ? "Игрок" : null,
            [.. Enumerable.Range(1, comments).Select(i => Comment($"Комментарий {claimId}-{i}"))],
            SkippedExisting: 0,
            Conflicts: [],
            SkippedFinance: 0,
            Unparsed: 0,
            exists ? [] : [CommentRestoreWarningView.ClaimNotFound]);

    private static CommentRestorePlanViewModel Plan(params CommentRestoreRowViewModel[] rows) => new(ProjectId, null, rows);

    private IRenderedComponent<CommentRestorePanel> RenderPanel() => Render<CommentRestorePanel>(p => p.Add(c => c.ProjectId, ProjectId));

    private static IElement Button(IRenderedComponent<CommentRestorePanel> cut, string text)
        => cut.FindAll("button").Single(b => b.TextContent.Contains(text));

    private static IElement Checkbox(IRenderedComponent<CommentRestorePanel> cut, int claimId)
        => cut.FindAll("tbody tr").Single(tr => tr.QuerySelector("a")?.TextContent == claimId.ToString())
            .QuerySelector("input[type=checkbox]")!;

    [Fact]
    public void ByDefault_AppliesClaimsWithCommentsOnly_ThenReloadsPlan()
    {
        client.Plans.Enqueue(Plan(Row(5, comments: 2), Row(6, comments: 0, exists: false), Row(7, comments: 1), Row(8, comments: 0)));
        client.Plans.Enqueue(Plan());

        var cut = RenderPanel();
        // Заявка 8 — без комментариев к восстановлению и без пометок — свёрнута в счётчик.
        cut.Markup.ShouldContain("Уже восстановлено заявок: 1");
        Button(cut, "Применить выбранные").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Нечего восстанавливать"));
        client.Applied.Select(c => c.ClaimId).ShouldBe([5, 7]);
        client.PlanRequests.ShouldBe(2);
    }

    [Fact]
    public void UncheckedClaim_NotApplied()
    {
        client.Plans.Enqueue(Plan(Row(5, comments: 2), Row(7, comments: 1)));
        client.Plans.Enqueue(Plan());

        var cut = RenderPanel();
        Checkbox(cut, 5).Change(false);
        Button(cut, "Применить выбранные").Click();

        cut.WaitForAssertion(() => client.PlanRequests.ShouldBe(2));
        client.Applied.Select(c => c.ClaimId).ShouldBe([7]);
    }

    [Fact]
    public void SelectNoneThenAll_SelectsOnlyApplicable()
    {
        client.Plans.Enqueue(Plan(Row(5, comments: 2), Row(6, comments: 3, exists: false), Row(7, comments: 1)));
        client.Plans.Enqueue(Plan());

        var cut = RenderPanel();
        Button(cut, "Снять все").Click();
        Button(cut, "Применить выбранные").HasAttribute("disabled").ShouldBeTrue();
        Button(cut, "Выбрать все").Click();
        Button(cut, "Применить выбранные").Click();

        cut.WaitForAssertion(() => client.PlanRequests.ShouldBe(2));
        client.Applied.Select(c => c.ClaimId).ShouldBe([5, 7]);
    }

    [Fact]
    public void FailedClaim_DoesNotStopOthers_ErrorShownAfterReload()
    {
        client.Plans.Enqueue(Plan(Row(5, comments: 2), Row(7, comments: 1)));
        client.Plans.Enqueue(Plan(Row(5, comments: 2)));
        client.Failing.Add(5);

        var cut = RenderPanel();
        Button(cut, "Применить выбранные").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось по заявкам: 1"));
        client.Applied.Select(c => c.ClaimId).ShouldBe([5, 7]);
        cut.Markup.ShouldContain("Сбой заявки 5");
    }

    [Fact]
    public void AfterRestore_SameClaimsWithNothingLeft_CollapsedIntoCounter()
    {
        client.Plans.Enqueue(Plan(Row(5, comments: 2), Row(7, comments: 1)));
        // Реалистичная перезагрузка: те же заявки, но восстанавливать в них больше нечего.
        client.Plans.Enqueue(Plan(Row(5, comments: 0) with { SkippedExisting = 2 }, Row(7, comments: 0) with { SkippedExisting = 1 }));
        client.Plans.Enqueue(Plan(Row(5, comments: 0) with { SkippedExisting = 2 }, Row(7, comments: 0) with { SkippedExisting = 1 }));

        var cut = RenderPanel();
        Button(cut, "Применить выбранные").Click();

        // Сразу после применения строки видны — с итогом по каждой.
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Восстановлено комментариев: 2"));
        cut.FindAll("tbody tr").Count.ShouldBe(2);

        Button(cut, "Обновить план").Click();

        cut.WaitForAssertion(() => client.PlanRequests.ShouldBe(3));
        cut.Markup.ShouldContain("Нечего восстанавливать");
        cut.Markup.ShouldContain("Уже восстановлено заявок: 2");
        cut.FindAll("table").ShouldBeEmpty();
    }

    [Fact]
    public void RefreshPlan_PicksUpClaimsRestoredByNeighbourIsland()
    {
        client.Plans.Enqueue(Plan(Row(6, comments: 0, exists: false)));
        client.Plans.Enqueue(Plan(Row(6, comments: 2)));
        client.Plans.Enqueue(Plan());

        var cut = RenderPanel();
        Button(cut, "Применить выбранные").HasAttribute("disabled").ShouldBeTrue();

        Button(cut, "Обновить план").Click();
        cut.WaitForAssertion(() => client.PlanRequests.ShouldBe(2));
        Button(cut, "Применить выбранные").Click();

        cut.WaitForAssertion(() => client.Applied.Select(c => c.ClaimId).ShouldBe([6]));
    }

    [Fact]
    public void ReloadFailsAfterApply_ResultsKeptAndAskedToRefresh()
    {
        client.Plans.Enqueue(Plan(Row(5, comments: 2)));
        // Второго плана нет — перезагрузка после применения упадёт.

        var cut = RenderPanel();
        Button(cut, "Применить выбранные").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Обновите страницу"));
        cut.Markup.ShouldContain("Восстановлено комментариев: 1");
        cut.Markup.ShouldContain("Восстановлено: 1");
    }

    [Fact]
    public void ConflictsInResult_ShownInRow()
    {
        client.Plans.Enqueue(Plan(Row(5, comments: 2)));
        client.Plans.Enqueue(Plan(Row(5, comments: 0) with { Conflicts = [42], Warnings = [CommentRestoreWarningView.CommentIdConflict] }));
        client.Results[5] = new CommentRestoreApplyResult(1, 0, [42]);

        var cut = RenderPanel();
        Button(cut, "Применить выбранные").Click();

        cut.WaitForAssertion(() => client.PlanRequests.ShouldBe(2));
        cut.Markup.ShouldContain("Восстановлено: 1, не восстановлены из-за занятых номеров: 42");
    }

    [Fact]
    public void Expand_ShowsComments()
    {
        client.Plans.Enqueue(Plan(Row(5, comments: 2)));

        var cut = RenderPanel();
        cut.Markup.ShouldNotContain("Комментарий 5-1");
        Button(cut, "Показать").Click();

        cut.Markup.ShouldContain("Комментарий 5-1");
        cut.Markup.ShouldContain("Комментарий 5-2");
    }

    [Fact]
    public void ProjectNotBlocked_ShowsExplanation()
    {
        client.Plans.Enqueue(new CommentRestorePlanViewModel(ProjectId, ClaimRestoreUnavailableReason.ProjectNotBlocked, []));

        var cut = RenderPanel();

        cut.Markup.ShouldContain("только пока проект заблокирован");
        cut.FindAll("table").ShouldBeEmpty();
    }

    private sealed class FakeClient : ICommentRestoreClient
    {
        public Queue<CommentRestorePlanViewModel> Plans { get; } = new();
        public List<ClaimIdentification> Applied { get; } = [];
        public HashSet<int> Failing { get; } = [];
        public Dictionary<int, CommentRestoreApplyResult> Results { get; } = [];
        public int PlanRequests { get; private set; }

        public Task<CommentRestorePlanViewModel> GetPlan(ProjectIdentification projectId)
        {
            PlanRequests++;
            return Plans.TryDequeue(out var plan) ? Task.FromResult(plan) : throw new InvalidOperationException("нет связи");
        }

        public Task<CommentRestoreApplyResult> Apply(ClaimIdentification claimId)
        {
            Applied.Add(claimId);
            return Failing.Contains(claimId.ClaimId)
                ? throw new InvalidOperationException($"Сбой заявки {claimId.ClaimId}")
                : Task.FromResult(Results.GetValueOrDefault(claimId.ClaimId) ?? new CommentRestoreApplyResult(1, 0, []));
        }
    }

    private sealed class FakeLocator : IUriLocator<ClaimIdentification>
    {
        public Uri GetUri(ClaimIdentification target) => new($"/claim/{target.ClaimId}", UriKind.Relative);
    }
}
