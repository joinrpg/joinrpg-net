using System.Net;
using System.Text;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Search;
using JoinRpg.Web.AdminTools.PscbPayments;
using JoinRpg.WebPortal.Managers.AdminTools;
using Microsoft.Extensions.Logging.Abstractions;
using PscbApi;

namespace JoinRpg.WebPortal.Managers.Test.AdminTools;

/// <summary>
/// Простановка ссылок на пользователя и проект в списке платежей (ADR025).
/// </summary>
/// <remarks>
/// На живой базе это не проверить: платежи приходят из боевого магазина, а проекты в локальной
/// базе другие, поэтому поиск по названию не находит ничего. Логика проверяется здесь на подставных
/// <see cref="ISearchService"/> и <see cref="IUriService"/>, а банк подменён тем же приёмом, что и
/// в <c>PscbApi.Test</c> — к сети тесты не ходят.
/// </remarks>
public class PscbPaymentsViewServiceTests
{
    private static readonly DateTimeOffset From = new(2026, 10, 1, 0, 0, 0, TimeSpan.FromHours(3));

    [Fact]
    public async Task FillsTheUserAndProjectLinks()
    {
        var search = new FakeSearchService();
        search.AddProject(7, "Проект");
        var service = CreateService(search, Payment("0000000042", account: "42", details: TicketPurpose("Проект")));

        var result = await service.GetPayments(Query());

        var row = result.Payments.ShouldHaveSingleItem();
        row.ProjectName.ShouldBe("Проект");
        row.UserUrl.ShouldBe("https://joinrpg.test/user/42");
        row.ProjectUrl.ShouldBe("https://joinrpg.test/7/home");
    }

    [Fact]
    public async Task LeavesTheUserLinkEmptyWhenTheAccountIsNotANumber()
    {
        var service = CreateService(new FakeSearchService(), Payment("1", account: "not-a-number", details: null));

        var row = (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem();

        row.UserId.ShouldBe("not-a-number");
        row.UserUrl.ShouldBeNull();
    }

    [Fact]
    public async Task LeavesTheProjectLinkEmptyWhenNothingWasFound()
    {
        var service = CreateService(new FakeSearchService(), Payment("1", account: "42", details: TicketPurpose("Проект")));

        var row = (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem();

        row.ProjectName.ShouldBe("Проект");
        row.ProjectUrl.ShouldBeNull();
    }

    /// <summary>
    /// Провайдер ищет вхождением подстроки, поэтому совпадение обязательно сверять по полному
    /// имени: иначе платёж проекта «Проект» получил бы ссылку на «Проект Второй».
    /// </summary>
    [Fact]
    public async Task IgnoresSubstringMatchesFromTheSearch()
    {
        var search = new FakeSearchService();
        search.AddProject(8, "Проект Второй");
        var service = CreateService(search, Payment("1", account: "42", details: TicketPurpose("Проект")));

        var row = (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem();

        row.ProjectUrl.ShouldBeNull();
    }

    /// <summary>
    /// При нескольких проектах с одним именем ссылка вела бы на случайный из них, поэтому её нет.
    /// </summary>
    [Fact]
    public async Task LeavesTheProjectLinkEmptyWhenTheNameIsAmbiguous()
    {
        var search = new FakeSearchService();
        search.AddProject(7, "Проект");
        search.AddProject(9, "Проект");
        var service = CreateService(search, Payment("1", account: "42", details: TicketPurpose("Проект")));

        var row = (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem();

        row.ProjectUrl.ShouldBeNull();
    }

    /// <summary>
    /// В выгрузке за месяц сотни платежей и единицы названий, поэтому поиск идёт по distinct-названию.
    /// </summary>
    [Fact]
    public async Task SearchesEachDistinctProjectNameOnce()
    {
        var search = new FakeSearchService();
        search.AddProject(7, "Проект");
        var service = CreateService(
            search,
            Payment("1", account: "42", details: TicketPurpose("Проект")),
            Payment("2", account: "43", details: TicketPurpose("Проект")),
            Payment("3", account: "44", details: TicketPurpose("Другой")));

        var result = await service.GetPayments(Query());

        result.Payments.Count.ShouldBe(3);
        search.Requests.ShouldBe(["Проект", "Другой"]);
    }

    /// <summary>
    /// У рекуррентной подписки назначение другое и префикса в нём нет: текст остаётся как есть,
    /// а проект по нему не ищется осмысленно — и всё равно не должен ломать список.
    /// </summary>
    [Fact]
    public async Task KeepsThePurposeThatHasNoTicketPrefix()
    {
        var service = CreateService(new FakeSearchService(), Payment("1", account: "42", details: "Подписка пользователя на Сервисы JoinRpg"));

        var row = (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem();

        row.ProjectName.ShouldBe("Подписка пользователя на Сервисы JoinRpg");
        row.ProjectUrl.ShouldBeNull();
    }

    /// <summary>
    /// В режиме отладки <see cref="BankApi"/> добавляет перед назначением маркер
    /// <c>[Debug mode] </c>: в поиск должно уйти чистое название проекта, иначе ссылки не будет.
    /// </summary>
    [Fact]
    public async Task SearchesTheProjectByThePurposeWithoutTheDebugMarker()
    {
        var search = new FakeSearchService();
        search.AddProject(7, "Проект");
        var service = CreateService(
            search,
            Payment("1", account: "42", details: PscbPaymentViewModelBuilder.DebugPurposePrefix + TicketPurpose("Проект")));

        var row = (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem();

        row.ProjectName.ShouldBe("Проект");
        row.ProjectUrl.ShouldBe("https://joinrpg.test/7/home");
        search.Requests.ShouldBe(["Проект"]);
    }

    private static string TicketPurpose(string projectName)
        => PscbPaymentViewModelBuilder.TicketPurposePrefix + projectName;

    private static PscbPaymentsQuery Query() => new(From, From.AddDays(5), null, null, false);

    private static string Payment(string orderId, string? account, string? details)
        => $$"""{"orderId":"{{orderId}}","paymentId":"11","amount":100.0,"state":"end","account":{{Quote(account)}},"details":{{Quote(details)}}}""";

    private static string Quote(string? value) => value is null ? "null" : "\"" + value + "\"";

    private static PscbPaymentsViewService CreateService(FakeSearchService search, params string[] payments)
        => new(
            new FakeBankSecrets(),
            new StubHttpClientFactory(payments),
            new FakeUriService(),
            search,
            NullLogger<PscbPaymentsViewService>.Instance);

    private sealed class FakeBankSecrets : IBankSecretsProvider
    {
        public bool Debug => false;

        public bool DebugOutput => false;

        public string ApiEndpoint => "https://bank.example.invalid";

        public string ApiDebugEndpoint => "https://demo.bank.example.invalid";

        public string MerchantId => "47607";

        public string ApiKey => "secret-key";

        public string ApiDebugKey => "debug-secret-key";

        public string BankSystemPaymentUrl => "https://bank.example.invalid/payment/{0}";

        public string BankSystemDebugPaymentUrl => "https://demo.bank.example.invalid/payment/{0}";
    }

    private sealed class StubHttpClientFactory(string[] payments) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new SingleResponseHandler($$"""{"payments":[{{string.Join(",", payments)}}]}"""));
    }

    private sealed class SingleResponseHandler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
    }

    private sealed class FakeUriService : IUriService
    {
        public string Get(ILinkable link) => GetUri(link).AbsoluteUri;

        public Uri GetUri(ILinkable link) => link.LinkType switch
        {
            LinkType.ResultUser => new($"https://joinrpg.test/user/{link.Identification}", UriKind.Absolute),
            LinkType.Project => new($"https://joinrpg.test/{link.ProjectId}/home", UriKind.Absolute),
            _ => throw new ArgumentOutOfRangeException(nameof(link), link.LinkType, null),
        };
    }

    private sealed class FakeSearchService : ISearchService
    {
        private readonly List<(int ProjectId, string Name)> projects = [];

        public List<string> Requests { get; } = [];

        public void AddProject(int projectId, string name) => projects.Add((projectId, name));

        public Task<IReadOnlyCollection<SearchResult>> SearchAsync(
            int? currentUserId,
            string searchString,
            IReadOnlyCollection<LinkType>? linkTypes = null,
            ProjectIdentification? projectId = null)
        {
            Requests.Add(searchString);

            // Как и настоящий провайдер, ищем вхождением подстроки — тест обязан показать,
            // что сервис не полагается на точное совпадение со стороны поиска.
            IReadOnlyCollection<SearchResult> found = [.. projects
                .Where(p => p.Name.Contains(searchString, StringComparison.Ordinal))
                .Select(p => new SearchResult
                {
                    LinkType = LinkType.Project,
                    Name = p.Name,
                    Description = new MarkdownDbValue(""),
                    Identification = p.ProjectId.ToString(),
                    ProjectId = p.ProjectId,
                    IsPublic = true,
                    IsActive = true,
                })];

            return Task.FromResult(found);
        }
    }
}
