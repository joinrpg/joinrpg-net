using Bunit.TestDoubles;
using JoinRpg.DomainTypes;
using JoinRpg.Web.Claims.CaptainCabinet;
using JoinRpg.Web.Claims.UnifiedGrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Web.Claims.Test;

/// <summary>
/// Грид кабинета капитана держит фильтр и номер страницы в query string страницы (ADR027 §5):
/// страница передаёт их острову параметрами, а остров при смене переписывает URL. Переписывает,
/// а не добавляет запись в историю: перезагрузки при этом не происходит (см. комментарий в
/// <c>CaptainClaimsGrid.RewriteUrl</c>).
/// </summary>
public class CaptainClaimsGridTest : BunitContext
{
    private static readonly ProjectIdentification ProjectId = new(123);

    private readonly FakeGridClient client = new() { TotalPages = 3 };

    public CaptainClaimsGridTest()
    {
        Services.AddSingleton<IUnifiedGridClient>(client);
        Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        // Остров строит новый URL от текущего адреса страницы, а не от имени проекта.
        Navigation.NavigateTo($"http://localhost/{ProjectId.Value}/captain");
    }

    private BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

    private IRenderedComponent<CaptainClaimsGrid> RenderGrid(
        UgStatusFilterView initialStatus = UgStatusFilterView.Active,
        int initialPage = 1)
        => Render<CaptainClaimsGrid>(parameters => parameters
            .Add(grid => grid.ProjectId, ProjectId)
            .Add(grid => grid.InitialStatus, initialStatus)
            .Add(grid => grid.InitialPage, initialPage));

    /// <summary>Ждёт именно этой последовательности запросов к webapi: обработчики асинхронные.</summary>
    private void WaitCalls(IRenderedComponent<CaptainClaimsGrid> cut, params (UgStatusFilterView Status, int Page)[] expected)
        => cut.WaitForAssertion(() => client.Calls.ShouldBe(expected));

    /// <summary>Остров начинает с фильтра и страницы, которые страница прочитала из query string.</summary>
    [Fact]
    public void StartsFromStatusAndPagePassedByPage()
    {
        var cut = RenderGrid(UgStatusFilterView.OnHold, initialPage: 2);

        WaitCalls(cut, (UgStatusFilterView.OnHold, 2));
        // Стартовая загрузка адрес не трогает: он и так правильный (в истории — только настройка
        // стартового адреса).
        Navigation.History.ShouldHaveSingleItem();
    }

    /// <summary>
    /// Смена фильтра сбрасывает страницу на первую и переписывает URL: старый номер к новому
    /// фильтру не относится. Запись истории заменяется, страница не перезагружается.
    /// </summary>
    [Fact]
    public void FilterChangeResetsPageAndReplacesUrl()
    {
        var cut = RenderGrid(UgStatusFilterView.Approved, initialPage: 3);

        cut.Find("select").Change(UgStatusFilterView.OnHold.ToString());

        WaitCalls(cut, (UgStatusFilterView.Approved, 3), (UgStatusFilterView.OnHold, 1));
        var entry = Navigation.History.Last();
        entry.Uri.ShouldEndWith($"?status={UgStatusFilterView.OnHold}");
        entry.Options.ReplaceHistoryEntry.ShouldBeTrue();
        entry.Options.ForceLoad.ShouldBeFalse();
    }

    /// <summary>Значения по умолчанию в URL не пишутся: «Активные, первая страница» — адрес без query.</summary>
    [Fact]
    public void DefaultFilterAndPageAreOmittedFromUrl()
    {
        var cut = RenderGrid(UgStatusFilterView.OnHold);

        cut.Find("select").Change(UgStatusFilterView.Active.ToString());

        WaitCalls(cut, (UgStatusFilterView.OnHold, 1), (UgStatusFilterView.Active, 1));
        Navigation.History.Last().Uri.ShouldEndWith("/captain");
    }

    /// <summary>Смена страницы дописывает её номер к URL, сохраняя фильтр.</summary>
    [Fact]
    public void PageChangeWritesPageToUrl()
    {
        var cut = RenderGrid(UgStatusFilterView.Approved);

        cut.FindAll("a").Single(link => link.TextContent == "2").Click();

        WaitCalls(cut, (UgStatusFilterView.Approved, 1), (UgStatusFilterView.Approved, 2));
        var entry = Navigation.History.Last();
        entry.Uri.ShouldEndWith($"?status={UgStatusFilterView.Approved}&page=2");
        entry.Options.ReplaceHistoryEntry.ShouldBeTrue();
        entry.Options.ForceLoad.ShouldBeFalse();
    }

    private sealed class FakeGridClient : IUnifiedGridClient
    {
        public List<(UgStatusFilterView Status, int Page)> Calls { get; } = [];

        public int TotalPages { get; set; }

        public Task<UgPageViewModel> GetGrid(
            ProjectIdentification projectId,
            UgScopeView scope,
            UgFilterView filter,
            int page)
        {
            Calls.Add((filter.Status, page));
            return Task.FromResult(new UgPageViewModel(
                [],
                Math.Clamp(page, 1, Math.Max(TotalPages, 1)),
                TotalPages,
                TotalCharacters: 0));
        }
    }
}
