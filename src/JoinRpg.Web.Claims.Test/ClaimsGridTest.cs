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
        // URL переписывается через history.replaceState (без NavigationManager: он в острове без
        // Router перезагружает страницу) — мокаем и собираем адреса вызовов.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

    /// <summary>Последний адрес, записанный <c>history.replaceState</c>.</summary>
    private string LastReplacedUrl
        => JSInterop.Invocations["history.replaceState"].Last().Arguments[2].ToString()!;

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
        params (UgScopeView Scope, UgFilterView Filter, int Page)[] expected)
        => cut.WaitForAssertion(() => client.Calls.ShouldBe(expected));

    /// <summary>Остров начинает с фильтром и страницей, которые страница прочитала из query string.</summary>
    [Fact]
    public void StartsFromStatusAndPagePassedByPage()
    {
        var cut = RenderGrid(initialStatus: UgStatusFilterView.OnHold, initialPage: 2);

        WaitCalls(cut, (UgScopeView.Captain, new UgFilterView(UgStatusFilterView.OnHold), 2));
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

        cut.Find("input[value='OnHold']").Change(true);

        WaitCalls(cut, (UgScopeView.Captain, new UgFilterView(UgStatusFilterView.Approved), 3), (UgScopeView.Captain, new UgFilterView(UgStatusFilterView.OnHold), 1));
        LastReplacedUrl.ShouldEndWith($"?{UgQueryParameterNames.Status}={UgStatusFilterView.OnHold}");
        // Записей навигации не прибавилось: адрес заменён, а не открыт заново.
        Navigation.History.ShouldHaveSingleItem();
    }

    /// <summary>Значения по умолчанию в URL не пишутся: «Активные, первая страница» — адрес без query.</summary>
    [Fact]
    public void DefaultFilterAndPageAreOmittedFromUrl()
    {
        var cut = RenderGrid(initialStatus: UgStatusFilterView.OnHold);

        cut.Find("input[value='Active']").Change(true);

        WaitCalls(cut, (UgScopeView.Captain, new UgFilterView(UgStatusFilterView.OnHold), 1), (UgScopeView.Captain, new UgFilterView(UgStatusFilterView.Active), 1));
        LastReplacedUrl.ShouldEndWith("/claims");
    }

    /// <summary>Смена страницы дописывает её номер к URL, сохраняя фильтр.</summary>
    [Fact]
    public void PageChangeWritesPageToUrl()
    {
        var cut = RenderGrid(initialStatus: UgStatusFilterView.Approved);

        cut.FindAll("a").Single(link => link.TextContent == "2").Click();

        WaitCalls(cut, (UgScopeView.Captain, new UgFilterView(UgStatusFilterView.Approved), 1), (UgScopeView.Captain, new UgFilterView(UgStatusFilterView.Approved), 2));
        LastReplacedUrl.ShouldEndWith($"?{UgQueryParameterNames.Status}={UgStatusFilterView.Approved}&{UgQueryParameterNames.Page}=2");
    }

    /// <summary>Скоуп передаётся серверу: правами на него занимается сервер, а не компонент.</summary>
    [Fact]
    public void MasterScopeIsPassedToServer()
    {
        var cut = RenderGrid(UgScopeView.Master);

        WaitCalls(cut, (UgScopeView.Master, new UgFilterView(UgStatusFilterView.Active), 1));
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
            (UgScopeView.Master, new UgFilterView(UgStatusFilterView.All), 1),
            (UgScopeView.Master, new UgFilterView(UgStatusFilterView.All, OnlyWithProblems: true), 1));
        LastReplacedUrl.ShouldEndWith(
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

        // JoinBadge: span.label + подсказка компонентом Tooltip (не title-атрибут).
        cut.WaitForAssertion(() =>
        {
            cut.FindAll("span.label").Single(label => label.TextContent.Trim() == "3");
            cut.FindAll(".join-tooltip").Single(t => t.TextContent.Contains("Непрочитанные"));
        });
    }

    /// <summary>
    /// Смена одного фильтра не сбрасывает остальные: каждый запрос несёт полный набор — группа,
    /// ответственный и статус накапливаются, а не затираются «не менявшимися» null-ами.
    /// </summary>
    [Fact]
    public void ChangingOneFilterKeepsOthersApplied()
    {
        var cut = RenderGrid(UgScopeView.Master, initialStatus: UgStatusFilterView.All);
        var group = new CharacterGroupIdentification(ProjectId, 10);
        var master = new UserIdentification(1);

        cut.Find($"input[value='{group}']").Change(true);
        cut.WaitForAssertion(() => client.Calls.Count.ShouldBe(2));

        cut.Find($"input[value='{master}']").Change(true);
        cut.WaitForAssertion(() => client.Calls.Count.ShouldBe(3));

        cut.Find("input[value='OnHold']").Change(true);
        cut.WaitForAssertion(() => client.Calls.Count.ShouldBe(4));

        client.Calls[1].ShouldBe((UgScopeView.Master, new UgFilterView(UgStatusFilterView.All, Group: group), 1));
        client.Calls[2].ShouldBe((UgScopeView.Master, new UgFilterView(UgStatusFilterView.All, Group: group, Responsible: master), 1));
        client.Calls[3].ShouldBe((UgScopeView.Master, new UgFilterView(UgStatusFilterView.OnHold, Group: group, Responsible: master), 1));
    }

    /// <summary>
    /// Счётчик над списком считает заявки всего результата фильтра, а не текущей страницы.
    /// Заголовок и счётчик — в группе с центровкой по flex: при разных кеглях строчное
    /// выравнивание по baseline оставляло бейдж ниже оптического центра заголовка.
    /// </summary>
    [Fact]
    public void CounterShowsCharactersAndClaimsOfWholeFilter()
    {
        // Персонаж один, заявок в результате пять — дробь «персонажи/заявки» с расшифровкой.
        client.Item = NewItem(0);
        client.ClaimsInResult = 5;

        var cut = RenderGrid(UgScopeView.Master);

        cut.WaitForAssertion(() =>
        {
            cut.FindAll("span.label").Single(label => label.TextContent.Trim() == "1/5");
            cut.Find(".join-tooltip").TextContent.ShouldContain("Персонажи/Заявки");
        });

        // Группа «заголовок + счётчик» центрует бейдж flex'ом, а не baseline'ом: align-items
        // без inline-flex не работает, поэтому проверяем и то, и другое.
        var counter = cut.FindAll("span.label").Single(label => label.TextContent.Trim() == "1/5");
        var group = counter.ParentElement!.ParentElement!.ParentElement!;
        group.GetAttribute("style")!.ShouldContain("display:inline-flex");
        group.GetAttribute("style")!.ShouldContain("align-items:center");

        // Поровну — одна цифра без tooltip.
        client.Calls.Clear();
        client.ClaimsInResult = 1;
        var equal = RenderGrid(UgScopeView.Master);

        equal.WaitForAssertion(() => equal.FindAll("span.label").Single(label => label.TextContent.Trim() == "1"));
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
        public List<(UgScopeView Scope, UgFilterView Filter, int Page)> Calls { get; } = [];

        public int TotalPages { get; set; }

        public UgItemViewModel? Item { get; set; }

        public int ClaimsInResult { get; set; }

        public Task<UgPageViewModel> GetGrid(
            ProjectIdentification projectId,
            UgScopeView scope,
            UgFilterView filter,
            int page)
        {
            Calls.Add((scope, filter, page));
            IReadOnlyList<UgItemViewModel> items = Item is null ? [] : [Item];
            return Task.FromResult(new UgPageViewModel(
                items,
                Math.Clamp(page, 1, Math.Max(TotalPages, 1)),
                TotalPages,
                TotalCharacters: items.Count,
                TotalClaims: ClaimsInResult,
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
