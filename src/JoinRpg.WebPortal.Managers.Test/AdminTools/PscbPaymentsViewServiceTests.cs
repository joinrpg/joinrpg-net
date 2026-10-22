using System.Net;
using System.Text;
using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Users;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Interfaces;
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
/// На живой базе это не проверить: платежи приходят из боевого магазина, а проекты и пользователи
/// в локальной базе другие, поэтому поиск ничего не находит. Логика проверяется здесь на подставных
/// <see cref="ISearchService"/>, <see cref="IUriService"/> и <see cref="IUserRepository"/>, а банк
/// подменён тем же приёмом, что и в <c>PscbApi.Test</c> — к сети тесты не ходят.
/// </remarks>
public class PscbPaymentsViewServiceTests
{
    private static readonly DateTimeOffset From = new(2026, 10, 1, 0, 0, 0, TimeSpan.FromHours(3));

    [Fact]
    public async Task FillsTheUserAndProjectLinks()
    {
        var search = new FakeSearchService();
        search.AddProject(7, "Проект");
        var service = CreateService(search, Users(42), Payment("0000000042", account: "42", details: TicketPurpose("Проект")));

        var result = await service.GetPayments(Query());

        var row = result.Payments.ShouldHaveSingleItem();
        row.ProjectName.ShouldBe("Проект");
        row.UserUrl.ShouldBe("https://joinrpg.test/user/42");
        row.ProjectUrl.ShouldBe("https://joinrpg.test/7/home");
    }

    [Fact]
    public async Task LeavesTheUserLinkEmptyWhenTheAccountIsNotANumber()
    {
        var service = CreateService(new FakeSearchService(), new FakeUserRepository(), Payment("1", account: "not-a-number", details: null));

        var row = (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem();

        row.UserId.ShouldBe("not-a-number");
        row.UserUrl.ShouldBeNull();
    }

    /// <summary>
    /// id из платежа может вести в никуда: пользователь удалён или объединён, а платёж создан не
    /// через JoinRpg. Тогда ищем по email из того же платежа — и ссылка ведёт на найденного
    /// пользователя, даже если его id отличается от банковского: база первоисточник.
    /// </summary>
    [Fact]
    public async Task FindsTheUserByEmailWhenTheIdFromThePaymentIsNotFound()
    {
        var users = new FakeUserRepository();
        users.AddByEmail("vasya@example.com", 7);
        var service = CreateService(
            new FakeSearchService(),
            users,
            Payment("1", account: "999", details: null, email: "vasya@example.com"));

        var result = await service.GetPayments(Query());

        var row = result.Payments.ShouldHaveSingleItem();
        row.UserId.ShouldBe("999");
        row.UserUrl.ShouldBe("https://joinrpg.test/user/7");
    }

    /// <summary>
    /// Email — запасной путь, поэтому по нему не ищут, пока находится id из платежа.
    /// </summary>
    [Fact]
    public async Task DoesNotSearchByEmailWhenTheIdWasFound()
    {
        var users = Users(42);
        var service = CreateService(
            new FakeSearchService(),
            users,
            Payment("1", account: "42", details: null, email: "vasya@example.com"));

        await service.GetPayments(Query());

        users.EmailRequests.ShouldBeEmpty();
    }

    /// <summary>
    /// В выгрузке за месяц один плательщик встречается много раз, поэтому по email идёт один
    /// запрос на distinct-значение, а не на платёж. Регистр не разделяет и запрос, и словарь.
    /// </summary>
    [Fact]
    public async Task SearchesEachDistinctEmailOnce()
    {
        var users = new FakeUserRepository();
        users.AddByEmail("vasya@example.com", 7);
        var service = CreateService(
            new FakeSearchService(),
            users,
            Payment("1", account: "999", details: null, email: "vasya@example.com"),
            Payment("2", account: "998", details: null, email: "VASYA@example.com"));

        var result = await service.GetPayments(Query());

        users.EmailRequests.ShouldBe(["vasya@example.com"]);
        var rows = result.Payments.ToList();
        rows.Count.ShouldBe(2);
        rows[0].UserUrl.ShouldBe("https://joinrpg.test/user/7");
        rows[1].UserUrl.ShouldBe("https://joinrpg.test/user/7");
    }

    /// <summary>
    /// id плательщиков проверяются одним пачковым запросом на весь список, а не по одному на платёж.
    /// </summary>
    [Fact]
    public async Task AsksForThePayersOnceForTheWholeList()
    {
        var users = Users(42, 43);
        var service = CreateService(
            new FakeSearchService(),
            users,
            Payment("1", account: "42", details: null),
            Payment("2", account: "43", details: null),
            Payment("3", account: "42", details: null));

        var result = await service.GetPayments(Query());

        result.Payments.Count.ShouldBe(3);
        users.IdRequests.ShouldBe([42, 43]);
    }

    [Fact]
    public async Task LeavesTheProjectLinkEmptyWhenNothingWasFound()
    {
        var service = CreateService(new FakeSearchService(), Users(42), Payment("1", account: "42", details: TicketPurpose("Проект")));

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
        var service = CreateService(search, Users(42), Payment("1", account: "42", details: TicketPurpose("Проект")));

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
        var service = CreateService(search, Users(42), Payment("1", account: "42", details: TicketPurpose("Проект")));

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
            Users(42, 43, 44),
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
        var service = CreateService(new FakeSearchService(), Users(42), Payment("1", account: "42", details: "Подписка пользователя на Сервисы JoinRpg"));

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
            Users(42),
            Payment("1", account: "42", details: PscbPaymentViewModelBuilder.DebugPurposePrefix + TicketPurpose("Проект")));

        var row = (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem();

        row.ProjectName.ShouldBe("Проект");
        row.ProjectUrl.ShouldBe("https://joinrpg.test/7/home");
        search.Requests.ShouldBe(["Проект"]);
    }

    private static string TicketPurpose(string projectName)
        => PscbPaymentViewModelBuilder.TicketPurposePrefix + projectName;

    private static PscbPaymentsQuery Query() => new(From, From.AddDays(5), null, null, false);

    private static string Payment(string orderId, string? account, string? details, string? email = null)
        => $$"""{"orderId":"{{orderId}}","paymentId":"11","amount":100.0,"state":"end","account":{{Quote(account)}},"email":{{Quote(email)}},"details":{{Quote(details)}}}""";

    private static string Quote(string? value) => value is null ? "null" : "\"" + value + "\"";

    private static FakeUserRepository Users(params int[] existingIds)
    {
        var users = new FakeUserRepository();
        foreach (var id in existingIds)
        {
            users.AddExisting(id);
        }

        return users;
    }

    private static PscbPaymentsViewService CreateService(FakeSearchService search, FakeUserRepository users, params string[] payments)
        => new(
            new FakeBankSecrets(),
            new StubHttpClientFactory(payments),
            new FakeUriService(),
            search,
            users,
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

    /// <summary>
    /// Пользователи в базе: кто существует по id и кто находится по email. Остальные члены
    /// интерфейса шлюзу не нужны.
    /// </summary>
    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly HashSet<int> existingIds = [];
        private readonly Dictionary<string, int> idsByEmail = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Запрошенные id — в том порядке, в котором их прислал сервис.
        /// </summary>
        public List<int> IdRequests { get; } = [];

        /// <summary>
        /// Email'ы, по которым сервис искал пользователя.
        /// </summary>
        public List<string> EmailRequests { get; } = [];

        public void AddExisting(int userId) => existingIds.Add(userId);

        public void AddByEmail(string email, int userId) => idsByEmail[email] = userId;

        public Task<IReadOnlyCollection<UserInfoHeader>> GetUserInfoHeaders(IReadOnlyCollection<UserIdentification> userIds)
        {
            IdRequests.AddRange(userIds.Select(id => id.Value));

            IReadOnlyCollection<UserInfoHeader> found = [.. userIds
                .Where(id => existingIds.Contains(id.Value))
                .Select(id => new UserInfoHeader(id, new UserDisplayName($"Пользователь {id.Value}", null)))];

            return Task.FromResult(found);
        }

        public Task<UserIdentification?> FindByEmail(string email)
        {
            EmailRequests.Add(email);
            UserIdentification? found = idsByEmail.TryGetValue(email, out var userId) ? new UserIdentification(userId) : null;
            return Task.FromResult(found);
        }

        public Task<User> GetById(int id) => throw new NotImplementedException();
        public Task<User> WithProfile(int userId) => throw new NotImplementedException();
        public Task<User> GetWithSubscribe(int currentUserId) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<UserIdentification>> GetUsersWithoutSelectedAvatar(KeySetPagination pagination) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<User>> GetUsersWithAvatars(IReadOnlyCollection<UserIdentification> userIds) => throw new NotImplementedException();
        public Task<UserAvatar> LoadAvatar(AvatarIdentification userAvatarId) => throw new NotImplementedException();
        public Task<UserInfo?> GetUserInfo(UserIdentification userId) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<UserInfo>> GetUserInfos(IReadOnlyCollection<UserIdentification> userIds) => throw new NotImplementedException();
        public Task<IReadOnlyDictionary<UserIdentification, PhoneNumber>> GetPhoneNumbers(IReadOnlyCollection<UserIdentification> userIds) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<UserInfoHeader>> GetAdminUserInfoHeaders() => throw new NotImplementedException();
        public Task<UserIdentification?> FindByVk(string vkId) => throw new NotImplementedException();
        public Task<UserIdentification?> FindByTelegram(string telegramUsername) => throw new NotImplementedException();
    }
}
