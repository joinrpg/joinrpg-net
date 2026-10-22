using System.Net;
using System.Text;
using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Finances;
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
/// Простановка ссылок на пользователя и проект в списке платежей и списки тех, кого найти
/// не удалось (ADR025).
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
        result.UnresolvedUsers.ShouldBeEmpty();
        result.UnresolvedProjects.ShouldBeEmpty();
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
        result.UnresolvedUsers.ShouldBeEmpty();
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

    /// <summary>
    /// Кого не нашли ни по id, ни по email — отдельным списком, по одному разу на плательщика
    /// и со счётчиком его платежей.
    /// </summary>
    [Fact]
    public async Task CollectsThePayersThatWereNotFound()
    {
        var service = CreateService(
            new FakeSearchService(),
            new FakeUserRepository(),
            Payment("1", account: "999", details: null, email: "vasya@example.com"),
            Payment("2", account: "999", details: null, email: "vasya@example.com"),
            Payment("3", account: "not-a-number", details: null));

        var result = await service.GetPayments(Query());

        result.Payments.ShouldAllBe(row => row.UserUrl == null);

        var unresolved = result.UnresolvedUsers.ToList();
        unresolved.Count.ShouldBe(2);
        unresolved[0].UserId.ShouldBe("999");
        unresolved[0].Email.ShouldBe("vasya@example.com");
        unresolved[0].PaymentCount.ShouldBe(2);
        unresolved[1].UserId.ShouldBe("not-a-number");
        unresolved[1].Email.ShouldBeNull();
        unresolved[1].PaymentCount.ShouldBe(1);
    }

    /// <summary>
    /// id плательщика — число, поэтому и порядок в списке числовой: как текст «1181» встал бы
    /// раньше «126». Значения, которые числом не являются, идут в конце — даже те, что как текст
    /// встали бы первыми.
    /// </summary>
    [Fact]
    public async Task OrdersThePayersByTheirIdAsANumber()
    {
        var service = CreateService(
            new FakeSearchService(),
            new FakeUserRepository(),
            Payment("1", account: "1181", details: null),
            Payment("2", account: "126", details: null),
            Payment("3", account: "not-a-number", details: null),
            Payment("4", account: "7", details: null),
            Payment("5", account: "!broken", details: null));

        var result = await service.GetPayments(Query());

        result.UnresolvedUsers.Select(user => user.UserId).ShouldBe(["7", "126", "1181", "!broken", "not-a-number"]);
    }

    /// <summary>
    /// Названия, которые не удалось связать с проектом, — отдельным списком со счётчиком платежей.
    /// Сюда же попадает назначение, которое проектом не является вовсе (рекуррентная подписка).
    /// </summary>
    [Fact]
    public async Task CollectsThePurposesThatWereNotResolvedIntoAProject()
    {
        var search = new FakeSearchService();
        search.AddProject(7, "Проект");
        var service = CreateService(
            search,
            Users(42),
            Payment("1", account: "42", details: TicketPurpose("Проект")),
            Payment("2", account: "42", details: TicketPurpose("Другой")),
            Payment("3", account: "42", details: TicketPurpose("Другой")),
            Payment("4", account: "42", details: "Подписка пользователя на Сервисы JoinRpg"));

        var result = await service.GetPayments(Query());

        result.UnresolvedProjects.Count.ShouldBe(2);
        result.UnresolvedProjects.First().ProjectName.ShouldBe("Другой");
        result.UnresolvedProjects.First().PaymentCount.ShouldBe(2);
        result.UnresolvedProjects.Last().ProjectName.ShouldBe("Подписка пользователя на Сервисы JoinRpg");
        result.UnresolvedProjects.Last().PaymentCount.ShouldBe(1);
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
    /// При нескольких проектах с одним именем ссылка вела бы на случайный из них, поэтому её нет,
    /// а имя попадает в список неопознанных.
    /// </summary>
    [Fact]
    public async Task LeavesTheProjectLinkEmptyWhenTheNameIsAmbiguous()
    {
        var search = new FakeSearchService();
        search.AddProject(7, "Проект");
        search.AddProject(9, "Проект");
        var service = CreateService(search, Users(42), Payment("1", account: "42", details: TicketPurpose("Проект")));

        var result = await service.GetPayments(Query());

        var row = result.Payments.ShouldHaveSingleItem();
        row.ProjectUrl.ShouldBeNull();
        result.UnresolvedProjects.ShouldHaveSingleItem().ProjectName.ShouldBe("Проект");
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

    /// <summary>
    /// О возврате сообщает банк — массивом <c>refunds</c> в элементе списка (форма снята с боевого
    /// ответа за сентябрь 2026), — а ссылка на операцию возврата приходит из нашей базы по
    /// <c>orderId</c>, который и есть CommentId операции.
    /// </summary>
    [Fact]
    public async Task FillsTheRefundFromTheDatabase()
    {
        var finances = new FakeFinanceOperationsRepository();
        finances.AddRefund(637705, new DateTime(2026, 9, 8, 23, 30, 0));
        var service = CreateService(
            new FakeSearchService(),
            Users(258),
            finances,
            Payment(
                "0000637705",
                account: "258",
                details: null,
                refunds: """[{"refundId":"1115846166","state":"END","amount":13450.0}]"""));

        var row = (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem();

        var refund = row.Refunds.ShouldHaveSingleItem();
        refund.RefundId.ShouldBe("1115846166");
        refund.Status.ShouldBe("END");
        refund.Amount.ShouldBe(13450.0m);
        row.RefundUrl.ShouldBe("https://joinrpg.test/42/finance/9001");
    }

    /// <summary>
    /// Заброшенную попытку платежа банк переименовывает сам, и её <c>orderId</c> числом не
    /// является. Возврат относится к текущему платежу, а не к предыдущим попыткам.
    /// </summary>
    [Fact]
    public async Task LeavesTheRefundOutForTheOrderIdTheBankHasReplaced()
    {
        var finances = new FakeFinanceOperationsRepository();
        finances.AddRefund(644272, new DateTime(2026, 5, 12));
        var service = CreateService(
            new FakeSearchService(),
            Users(42),
            finances,
            Payment("__REPLACED_0000644272", account: "42", details: null),
            Payment("0000644272", account: "42", details: null));

        var rows = (await service.GetPayments(Query())).Payments.ToList();

        rows.Count.ShouldBe(2);
        rows[0].RefundUrl.ShouldBeNull();
        rows[1].RefundUrl.ShouldBe("https://joinrpg.test/42/finance/9001");
    }

    /// <summary>
    /// Возвраты ищутся одним запросом на весь список, а не по одному на платёж.
    /// </summary>
    [Fact]
    public async Task AsksForTheRefundsOnceForTheWholeList()
    {
        var finances = new FakeFinanceOperationsRepository();
        finances.AddRefund(2, new DateTime(2026, 5, 12));
        var service = CreateService(
            new FakeSearchService(),
            Users(42),
            finances,
            Payment("0000000001", account: "42", details: null),
            Payment("0000000002", account: "42", details: null));

        var result = await service.GetPayments(Query());

        result.Payments.Count.ShouldBe(2);
        finances.Requests.ShouldBe([1, 2]);
    }

    /// <summary>
    /// Повторный возврат <c>PaymentsService</c> создать не даёт, но восстановленные из архива
    /// операции могут дать несколько: ссылка ведёт на последнюю по дате.
    /// </summary>
    [Fact]
    public async Task LinksTheLatestRefundOfOneOperation()
    {
        var finances = new FakeFinanceOperationsRepository();
        finances.AddRefund(1, new DateTime(2026, 5, 1), refundCommentId: 9001);
        finances.AddRefund(1, new DateTime(2026, 6, 1), refundCommentId: 9002);
        var service = CreateService(new FakeSearchService(), Users(42), finances, Payment("0000000001", account: "42", details: null));

        var row = (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem();

        row.RefundUrl.ShouldBe("https://joinrpg.test/42/finance/9002");
    }

    private static string TicketPurpose(string projectName)
        => PscbPaymentViewModelBuilder.TicketPurposePrefix + projectName;

    private static PscbPaymentsQuery Query() => new(From, From.AddDays(5), null, null, false);

    private static string Payment(string orderId, string? account, string? details, string? email = null, string? refunds = null)
        => $$"""{"orderId":"{{orderId}}","paymentId":"11","amount":100.0,"state":"end","account":{{Quote(account)}},"email":{{Quote(email)}},"details":{{Quote(details)}},"refunds":{{refunds ?? "null"}}}""";

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
        => CreateService(search, users, new FakeFinanceOperationsRepository(), payments);

    private static PscbPaymentsViewService CreateService(
        FakeSearchService search,
        FakeUserRepository users,
        FakeFinanceOperationsRepository finances,
        params string[] payments)
        => new(
            new FakeBankSecrets(),
            new StubHttpClientFactory(payments),
            new FakeUriService(),
            new FakeFinanceOperationUri(),
            search,
            users,
            finances,
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

    /// <summary>
    /// Возвраты в базе: какие операции возвращены, на сколько и когда.
    /// </summary>
    private sealed class FakeFinanceOperationsRepository : IFinanceOperationsRepository
    {
        private readonly List<ApprovedRefund> refunds = [];

        /// <summary>
        /// Идентификаторы операций, по которым сервис спрашивал возвраты.
        /// </summary>
        public List<int> Requests { get; } = [];

        public void AddRefund(int refundedOperationId, DateTime operationDate, int refundCommentId = 9001)
            => refunds.Add(new ApprovedRefund(
                refundedOperationId,
                new FinanceOperationIdentification(42, 7, refundCommentId),
                operationDate));

        public Task<IReadOnlyCollection<ApprovedRefund>> GetApprovedRefunds(IReadOnlyCollection<int> refundedOperationIds)
        {
            Requests.AddRange(refundedOperationIds);

            IReadOnlyCollection<ApprovedRefund> found = [.. refunds.Where(refund => refundedOperationIds.Contains(refund.RefundedOperationId))];
            return Task.FromResult(found);
        }

        public Task<IReadOnlyCollection<FinanceOperationIdentification>> GetUnfinishedOperations(KeySetPagination? pagination = null)
            => throw new NotImplementedException();
    }

    private sealed class FakeFinanceOperationUri : IUriLocator<FinanceOperationIdentification>
    {
        public Uri GetUri(FinanceOperationIdentification target)
            => new($"https://joinrpg.test/{target.ProjectId.Value}/finance/{target.FinanceOperationId}", UriKind.Absolute);
    }
}
