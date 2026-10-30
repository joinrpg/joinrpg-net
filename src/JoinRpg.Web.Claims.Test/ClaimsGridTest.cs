using Bunit.TestDoubles;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.Web.Claims.UnifiedGrid;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectCommon.Projects;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Web.Claims.Test;

/// <summary>
/// Грид заявок держит фильтр и номер страницы в query string страницы (ADR027 §5): страница
/// передаёт их острову параметрами, а остров при смене переписывает URL. Переписывает, а не
/// добавляет запись в историю: перезагрузки при этом не происходит. Мастерский скоуп добавляет
/// фильтры группы, ответственного и проблем, кнопки массовых операций и колонку проблем — всё по
/// скоупу, а не по содержимому данных.
/// </summary>
public class ClaimsGridTest : BunitContext
{
    private static readonly ProjectIdentification ProjectId = new(123);

    private readonly FakeGridClient client = new() { TotalPages = 3 };

    public ClaimsGridTest()
    {
        Services.AddSingleton<IUnifiedGridClient>(client);
        Services.AddSingleton<ICharacterGroupsClient>(new FakeGroupsClient());
        Services.AddSingleton<IMasterClient>(new FakeMastersClient());
        Services.AddSingleton<IProjectUriLocator>(new FakeProjectUriLocator());
        Services.AddSingleton<ICharacterGroupUriLocator>(new FakeGroupUriLocator());
        Services.AddSingleton<IUriLocator<CharacterLinkSlimViewModel>>(new FakeCharacterLinkLocator());
        Services.AddSingleton<IUriLocator<UserLinkViewModel>>(new FakeUserLinkLocator());
        Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        _ = Services.AddJoinRpgWebComponents();
        _ = AddBunitPersistentComponentState();
        // Остров живёт в WebAssembly: от этого зависит, переносит ли SelectorListCache списки с пререндера.
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        // Остров строит новый URL от текущего адреса страницы, а не от имени проекта.
        Navigation.NavigateTo($"http://localhost/{ProjectId.Value}/claims");
    }

    private BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

    private IRenderedComponent<ClaimsGrid> RenderGrid(
        UgScopeView scope = UgScopeView.Captain,
        UgStatusFilterView initialStatus = UgStatusFilterView.Active,
        int initialPage = 1)
        => Render<ClaimsGrid>(parameters => parameters
            .Add(grid => grid.ProjectId, ProjectId)
            .Add(grid => grid.Scope, scope)
            .Add(grid => grid.InitialStatus, initialStatus)
            .Add(grid => grid.InitialPage, initialPage));

    /// <summary>Ждёт именно этой последовательности запросов к webapi: обработчики асинхронные.</summary>
    private void WaitCalls(
        IRenderedComponent<ClaimsGrid> cut,
        params (UgScopeView Scope, UgStatusFilterView Status, bool OnlyWithProblems, int Page)[] expected)
        => cut.WaitForAssertion(() => client.Calls.ShouldBe(expected));

    /// <summary>Остров начинает с фильтром и страницей, которые страница прочитала из query string.</summary>
    [Fact]
    public void StartsFromStatusAndPagePassedByPage()
    {
        var cut = RenderGrid(initialStatus: UgStatusFilterView.OnHold, initialPage: 2);

        WaitCalls(cut, (UgScopeView.Captain, UgStatusFilterView.OnHold, false, 2));
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
        var cut = RenderGrid(initialStatus: UgStatusFilterView.Approved, initialPage: 3);

        cut.Find("select").Change(UgStatusFilterView.OnHold.ToString());

        WaitCalls(cut, (UgScopeView.Captain, UgStatusFilterView.Approved, false, 3), (UgScopeView.Captain, UgStatusFilterView.OnHold, false, 1));
        var entry = Navigation.History.Last();
        entry.Uri.ShouldEndWith($"?{UgQueryParameterNames.Status}={UgStatusFilterView.OnHold}");
        entry.Options.ReplaceHistoryEntry.ShouldBeTrue();
        entry.Options.ForceLoad.ShouldBeFalse();
    }

    /// <summary>Значения по умолчанию в URL не пишутся: «Активные, первая страница» — адрес без query.</summary>
    [Fact]
    public void DefaultFilterAndPageAreOmittedFromUrl()
    {
        var cut = RenderGrid(initialStatus: UgStatusFilterView.OnHold);

        cut.Find("select").Change(UgStatusFilterView.Active.ToString());

        WaitCalls(cut, (UgScopeView.Captain, UgStatusFilterView.OnHold, false, 1), (UgScopeView.Captain, UgStatusFilterView.Active, false, 1));
        Navigation.History.Last().Uri.ShouldEndWith("/claims");
    }

    /// <summary>Смена страницы дописывает её номер к URL, сохраняя фильтр.</summary>
    [Fact]
    public void PageChangeWritesPageToUrl()
    {
        var cut = RenderGrid(initialStatus: UgStatusFilterView.Approved);

        cut.FindAll("a").Single(link => link.TextContent == "2").Click();

        WaitCalls(cut, (UgScopeView.Captain, UgStatusFilterView.Approved, false, 1), (UgScopeView.Captain, UgStatusFilterView.Approved, false, 2));
        var entry = Navigation.History.Last();
        entry.Uri.ShouldEndWith($"?{UgQueryParameterNames.Status}={UgStatusFilterView.Approved}&{UgQueryParameterNames.Page}=2");
        entry.Options.ReplaceHistoryEntry.ShouldBeTrue();
        entry.Options.ForceLoad.ShouldBeFalse();
    }

    /// <summary>Скоуп передаётся серверу: правами на него занимается сервер, а не компонент.</summary>
    [Fact]
    public void MasterScopeIsPassedToServer()
    {
        var cut = RenderGrid(UgScopeView.Master);

        WaitCalls(cut, (UgScopeView.Master, UgStatusFilterView.Active, false, 1));
    }

    /// <summary>
    /// Колонка и фильтр проблем задаются скоупом, а не наличием проблем в данных страницы: у мастера
    /// колонка есть и на списке без проблем.
    /// </summary>
    [Theory]
    [InlineData(UgScopeView.Master, true)]
    [InlineData(UgScopeView.Captain, false)]
    public void ProblemsColumnComesFromScope(UgScopeView scope, bool expected)
    {
        var cut = RenderGrid(scope);

        cut.WaitForAssertion(() => cut.FindAll("th").Any(th => th.TextContent == "Проблемы").ShouldBe(expected));
    }

    /// <summary>
    /// Галочка «только с проблемами» перезагружает список с первой страницы и попадает в URL —
    /// вместе с ней ссылка на выгрузку относится к тому же фильтру.
    /// </summary>
    [Fact]
    public void OnlyWithProblemsChangesFilterUrlAndExportLink()
    {
        var cut = RenderGrid(UgScopeView.Master, initialStatus: UgStatusFilterView.All);

        cut.Find("input[type=checkbox]").Change(true);

        WaitCalls(cut,
            (UgScopeView.Master, UgStatusFilterView.All, false, 1),
            (UgScopeView.Master, UgStatusFilterView.All, true, 1));
        Navigation.History.Last().Uri.ShouldEndWith(
            $"?{UgQueryParameterNames.Status}={UgStatusFilterView.All}&{UgQueryParameterNames.OnlyWithProblems}=true");

        var exportLink = cut.FindAll("a").Single(link => link.TextContent.Contains("Скачать в Excel"));
        exportLink.GetAttribute("href").ShouldEndWith(
            $"?{UgQueryParameterNames.Status}={UgStatusFilterView.All}&{UgQueryParameterNames.OnlyWithProblems}=true&export=xlsx");
    }

    /// <summary>
    /// Бейдж непрочитанных комментариев — только в мастерском скоупе (ADR027 §4), с числом из ответа сервера.
    /// </summary>
    [Fact]
    public void UnreadCommentsBadgeShowsCountInClaimRow()
    {
        client.Item = NewItem(unreadCommentsCount: 3);

        var cut = RenderGrid(UgScopeView.Master);

        cut.WaitForAssertion(() => cut.Find("span.badge[title='Непрочитанные комментарии']").TextContent.Trim().ShouldBe("3"));
    }

    /// <summary>Счётчик над списком считает заявки всего результата фильтра, а не текущей страницы.</summary>
    [Fact]
    public void CounterCountsClaimsOfWholeFilter()
    {
        client.ClaimsInResult = 5;

        var cut = RenderGrid(UgScopeView.Master);

        cut.WaitForAssertion(() => cut.FindAll("button").Single(button => button.TextContent.Contains("заявок")).TextContent
            .ShouldContain("5"));
    }

    private static UgItemViewModel NewItem(int unreadCommentsCount)
        => new(
            new UgCharacterViewModel(
                new CharacterLinkSlimViewModel(new CharacterIdentification(ProjectId, 1), "Персонаж", true, ViewMode.Show),
                new CharacterApplyViewModel(new CharacterIdentification(ProjectId, 1), default, null, false, false),
                []),
            [
                new UgClaimViewModel(
                    new UserLinkViewModel(new(1), "Игрок", ViewMode.Show),
                    new ClaimFullStatusView(ClaimStatusView.AddedByUser, null),
                    null, null, null,
                    new UserLinkViewModel(new(1), "Мастер", ViewMode.Show),
                    new ClaimBalance(0, 0),
                    new ClaimIdentification(ProjectId, 1),
                    null,
                    null,
                    unreadCommentsCount),
            ]);

    private sealed class FakeGridClient : IUnifiedGridClient
    {
        public List<(UgScopeView Scope, UgStatusFilterView Status, bool OnlyWithProblems, int Page)> Calls { get; } = [];

        public int TotalPages { get; set; }

        public UgItemViewModel? Item { get; set; }

        public int ClaimsInResult { get; set; }

        public Task<UgPageViewModel> GetGrid(
            ProjectIdentification projectId,
            UgScopeView scope,
            UgFilterView filter,
            int page)
        {
            Calls.Add((scope, filter.Status, filter.OnlyWithProblems, page));
            IReadOnlyList<UgItemViewModel> items = Item is null ? [] : [Item];
            return Task.FromResult(new UgPageViewModel(
                items,
                Math.Clamp(page, 1, Math.Max(TotalPages, 1)),
                TotalPages,
                TotalCharacters: items.Count,
                OperationClaimIds: [.. Enumerable.Range(1, ClaimsInResult).Select(i => new ClaimIdentification(projectId, i))],
                OperationCharacterIds: []));
        }
    }

    private sealed class FakeGroupsClient : ICharacterGroupsClient
    {
        public Task<List<CharacterGroupDto>> GetRealCharacterGroups(int projectId) => Groups();

        public Task<List<CharacterGroupDto>> GetCharacterGroupsWithSpecial(int projectId) => Groups();

        public Task<List<CharacterGroupDto>> GetValidParentGroups(CharacterGroupIdentification groupId) => Groups();

        private static Task<List<CharacterGroupDto>> Groups()
            => Task.FromResult(new List<CharacterGroupDto>
            {
                new(new CharacterGroupIdentification(ProjectId, 10), "Группа", ["Корень", "Группа"], IsPublic: true, IsSpecial: false),
            });
    }

    private sealed class FakeMastersClient : IMasterClient
    {
        public Task<List<UserInfoHeader>> GetMasters(int projectId)
            => Task.FromResult(new List<UserInfoHeader> { new(new(1), new UserDisplayName("Мастер", null), null) });
    }

    private sealed class FakeCharacterLinkLocator : IUriLocator<CharacterLinkSlimViewModel>
    {
        public Uri GetUri(CharacterLinkSlimViewModel target) => new($"https://example.org/character/{target.CharacterId.CharacterId}");
    }

    private sealed class FakeUserLinkLocator : IUriLocator<UserLinkViewModel>
    {
        public Uri GetUri(UserLinkViewModel target) => new($"https://example.org/user/{target.UserId}");
    }

    private sealed class FakeGroupUriLocator : ICharacterGroupUriLocator
    {
        public Uri GetClaimListUri(CharacterGroupIdentification groupId) => new($"https://example.org/{groupId}/claims");

        public Uri GetDiscussingClaimListUri(CharacterGroupIdentification groupId) => new($"https://example.org/{groupId}/discussing");

        public Uri GetCharacterListUri(CharacterGroupIdentification groupId) => new($"https://example.org/{groupId}/characters");

        public Uri GetReportUri(CharacterGroupIdentification groupId) => new($"https://example.org/{groupId}/report");

        public Uri GetSubscribeUri(CharacterGroupIdentification groupId) => new($"https://example.org/{groupId}/subscribe");

        public Uri GetEditUri(CharacterGroupIdentification groupId) => new($"https://example.org/{groupId}/edit");

        public Uri GetDeleteUri(CharacterGroupIdentification groupId) => new($"https://example.org/{groupId}/delete");

        public Uri GetCreateCharacterUri(CharacterGroupIdentification groupId) => new($"https://example.org/{groupId}/create-character");

        public Uri GetAddGroupUri(CharacterGroupIdentification groupId) => new($"https://example.org/{groupId}/add-group");
    }
}
