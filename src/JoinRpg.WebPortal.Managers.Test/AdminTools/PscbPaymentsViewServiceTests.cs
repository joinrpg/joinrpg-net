using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.Services.Interfaces;
using JoinRpg.WebPortal.Managers.AdminTools;
using JoinRpg.WebPortal.Managers.Test.AdminTools.Pscb;
using JoinRpg.XGameApi.Contract.Pscb;
using Microsoft.Extensions.Logging.Abstractions;
using PscbApi;
using static JoinRpg.WebPortal.Managers.Test.AdminTools.Pscb.PscbPaymentsTestData;

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
        var service = CreateService(new FakeSearchService(), new PscbFakeUserRepository(), Payment("1", account: "not-a-number", details: null));

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
        var users = new PscbFakeUserRepository();
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
        var users = new PscbFakeUserRepository();
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
            new PscbFakeUserRepository(),
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
            new PscbFakeUserRepository(),
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

    /// <summary>
    /// <c>orderId</c> банка это <c>CommentId</c> нашей операции, поэтому найденная операция даёт
    /// и исход сверки, и ссылку на себя, и ссылку на свою заявку (ADR028).
    /// </summary>
    [Fact]
    public async Task MarksTheRowWhoseOperationIsFoundAsSettled()
    {
        var finances = new FakeFinanceOperationsRepository();
        finances.AddOperation(643617);
        var service = CreateService(new FakeSearchService(), Users(42), finances, Payment("0000643617", account: "42", details: null));

        var row = (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem();

        row.MatchState.ShouldBe(PscbPaymentMatchState.Settled);
        row.MatchStateText.ShouldBe("проведён");
        row.OperationUrl.ShouldBe("https://joinrpg.test/42/finance/643617");
        row.ClaimUrl.ShouldBe("https://joinrpg.test/42/claim/7/edit");
    }

    /// <summary>
    /// Потерянный платёж — деньги у банка есть, а операции у нас нет. Заявки у такой строки тоже
    /// нет: сверка её не определяет, это делает восстановление.
    /// </summary>
    [Fact]
    public async Task MarksTheRowWithoutAnOperationAsLost()
    {
        var service = CreateService(new FakeSearchService(), Users(42), Payment("0000643617", account: "42", details: null));

        var row = (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem();

        row.MatchState.ShouldBe(PscbPaymentMatchState.Lost);
        row.MatchStateText.ShouldBe("потерян");
        row.OperationUrl.ShouldBeNull();
        row.ClaimUrl.ShouldBeNull();
    }

    /// <summary>
    /// Существующая операция не правится, но и молча не пропускается: <c>Declined</c> при
    /// оплаченном платеже банка — расхождение, которое админ должен видеть (ADR028).
    /// </summary>
    [Fact]
    public async Task MarksAnExistingButUnsettledOperationAsNotSettled()
    {
        var finances = new FakeFinanceOperationsRepository();
        finances.AddOperation(1, FinanceOperationState.Declined);
        var service = CreateService(new FakeSearchService(), Users(42), finances, Payment("0000000001", account: "42", details: null));

        var row = (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem();

        row.MatchState.ShouldBe(PscbPaymentMatchState.NotSettled);
        row.MatchStateText.ShouldBe("не проведён");
        row.OperationUrl.ShouldBe("https://joinrpg.test/42/finance/1");
        row.ClaimUrl.ShouldBe("https://joinrpg.test/42/claim/7/edit");
    }

    /// <summary>
    /// Возврат банк зафиксировал, а проведённой операции возврата у нас нет: восстанавливается
    /// только возврат, поэтому исход отдельный.
    /// </summary>
    [Fact]
    public async Task MarksTheRefundWeHaveNotRecordedAsRefundMissing()
    {
        var finances = new FakeFinanceOperationsRepository();
        finances.AddOperation(637705);
        var service = CreateService(
            new FakeSearchService(),
            Users(258),
            finances,
            Payment(
                "0000637705",
                account: "258",
                details: null,
                refunds: """[{"refundId":"1115846166","state":"END","amount":100.0}]""",
                state: "ref"));

        var row = (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem();

        row.RefundedByBank.ShouldBeTrue();
        row.RefundUrl.ShouldBeNull();
        row.MatchState.ShouldBe(PscbPaymentMatchState.RefundMissing);
        row.MatchStateText.ShouldBe("нет возврата");
    }

    [Fact]
    public async Task MarksTheRefundWeHaveRecordedAsSettled()
    {
        var finances = new FakeFinanceOperationsRepository();
        finances.AddOperation(637705);
        finances.AddRefund(637705, new DateTime(2026, 9, 8));
        var service = CreateService(
            new FakeSearchService(),
            Users(258),
            finances,
            Payment(
                "0000637705",
                account: "258",
                details: null,
                refunds: """[{"refundId":"1115846166","state":"END","amount":100.0}]""",
                state: "ref"));

        var row = (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem();

        row.RefundUrl.ShouldNotBeNull();
        row.MatchState.ShouldBe(PscbPaymentMatchState.Settled);
    }

    /// <summary>
    /// Просроченный платёж денег не принёс, поэтому потерянным не считается: восстанавливать
    /// нечего, даже если операции у нас нет.
    /// </summary>
    [Fact]
    public async Task MarksTheExpiredPaymentAsNoMoney()
    {
        var service = CreateService(new FakeSearchService(), Users(42), Payment("0000000001", account: "42", details: null, state: "exp"));

        var row = (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem();

        row.MatchState.ShouldBe(PscbPaymentMatchState.NoMoney);
        row.MatchStateText.ShouldBe("денег нет");
    }

    /// <summary>
    /// Заброшенную попытку банк переименовывает, и её <c>orderId</c> числом не является: операции
    /// с таким номером быть не может, и сверять нечего.
    /// </summary>
    [Fact]
    public async Task MarksTheReplacedOrderIdAsNoRecord()
    {
        var service = CreateService(new FakeSearchService(), Users(42), Payment("__REPLACED_0000644272", account: "42", details: null));

        var row = (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem();

        row.MatchState.ShouldBe(PscbPaymentMatchState.NoRecord);
        row.MatchStateText.ShouldBe("не платёж JoinRpg");
    }

    /// <summary>
    /// Сверка спрашивает операции одним запросом на весь список, а не по одному на платёж,
    /// и не спрашивает про номера, которые числом не являются.
    /// </summary>
    [Fact]
    public async Task AsksForTheOperationsOnceForTheWholeList()
    {
        var finances = new FakeFinanceOperationsRepository();
        finances.AddOperation(2);
        var service = CreateService(
            new FakeSearchService(),
            Users(42),
            finances,
            Payment("0000000001", account: "42", details: null),
            Payment("0000000002", account: "42", details: null),
            Payment("__REPLACED_0000000003", account: "42", details: null));

        var result = await service.GetPayments(Query());

        result.Payments.Count.ShouldBe(3);
        finances.OperationRequests.ShouldBe([1, 2]);
    }

    /// <summary>
    /// Потерянный платёж встанет на заявку с наименьшим номером среди всех заявок плательщика
    /// в проекте — заказчик разрешил брать и закрытые, потому что потерянный платёж бывает и на них.
    /// </summary>
    [Fact]
    public async Task PlansTheLostPaymentIntoTheFirstClaimOfThePayer()
    {
        var search = new FakeSearchService();
        search.AddProject(7, "Проект");
        var projects = new PscbFakeProjectMetadataRepository();
        projects.AddReadyProject(7);
        var claims = new PscbFakeClaimsRepository();
        claims.AddClaim(7, 42, claimId: 101);
        claims.AddClaim(7, 42, claimId: 55);
        var service = CreateService(search, Users(42), new FakeFinanceOperationsRepository(), claims, projects, Payment("0000000001", account: "42", details: TicketPurpose("Проект")));

        var row = (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem();

        row.MatchState.ShouldBe(PscbPaymentMatchState.Lost);
        row.RestoreBlockReason.ShouldBeNull();
        row.ClaimUrl.ShouldBe("https://joinrpg.test/7/claim/55/edit");
    }

    /// <summary>
    /// Комментарий с номером платежа уже лежит в обсуждении заявки (например, его вернули
    /// инструменты DRP без операции): обсуждение называет заявку достовернее эвристики
    /// наименьшего номера, поэтому план и ссылка выбирают именно её — даже когда у плательщика
    /// есть заявка с меньшим номером.
    /// </summary>
    [Fact]
    public async Task PlansTheLostPaymentIntoTheClaimOfTheExistingComment()
    {
        var search = new FakeSearchService();
        search.AddProject(7, "Проект");
        var projects = new PscbFakeProjectMetadataRepository();
        projects.AddReadyProject(7);
        var claims = new PscbFakeClaimsRepository();
        claims.AddClaim(7, 42, claimId: 55);
        claims.AddClaim(7, 42, claimId: 101);
        claims.AddClaimComment(commentId: 1, projectId: 7, claimId: 101);
        var service = CreateService(search, Users(42), new FakeFinanceOperationsRepository(), claims, projects, Payment("0000000001", account: "42", details: TicketPurpose("Проект")));

        var row = (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem();

        row.MatchState.ShouldBe(PscbPaymentMatchState.Lost);
        row.RestoreBlockReason.ShouldBeNull();
        row.ClaimUrl.ShouldBe("https://joinrpg.test/7/claim/101/edit");
        // Запрос о комментариях один и пачкой — по distinct-номерам потерянных строк.
        claims.CommentIdRequests.ShouldHaveSingleItem().ShouldBe([1]);
    }

    /// <summary>
    /// Комментарий найден в обсуждении заявки другого проекта: платёж туда вставать не должен
    /// (тип оплаты выбран для определённого проекта), поэтому план откатывается к заявке
    /// с наименьшим номером у плательщика.
    /// </summary>
    [Fact]
    public async Task IgnoresTheCommentClaimOfAnotherProject()
    {
        var search = new FakeSearchService();
        search.AddProject(7, "Проект");
        var projects = new PscbFakeProjectMetadataRepository();
        projects.AddReadyProject(7);
        var claims = new PscbFakeClaimsRepository();
        claims.AddClaim(7, 42, claimId: 55);
        claims.AddClaimComment(commentId: 1, projectId: 9, claimId: 101);
        var service = CreateService(search, Users(42), new FakeFinanceOperationsRepository(), claims, projects, Payment("0000000001", account: "42", details: TicketPurpose("Проект")));

        var row = (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem();

        row.MatchState.ShouldBe(PscbPaymentMatchState.Lost);
        row.RestoreBlockReason.ShouldBeNull();
        row.ClaimUrl.ShouldBe("https://joinrpg.test/7/claim/55/edit");
    }

    /// <summary>
    /// Возврат пишется туда же, откуда пришёл платёж: заявка берётся у найденной операции,
    /// поэтому ни плательщик, ни название проекта для такой строки не нужны.
    /// </summary>
    [Fact]
    public async Task PlansTheMissingRefundOntoTheClaimOfTheFoundOperation()
    {
        var finances = new FakeFinanceOperationsRepository();
        finances.AddOperation(637705, claimId: 9);
        var projects = new PscbFakeProjectMetadataRepository();
        projects.AddReadyProject(42);
        var service = CreateService(
            new FakeSearchService(),
            new PscbFakeUserRepository(),
            finances,
            new PscbFakeClaimsRepository(),
            projects,
            Payment(
                "0000637705",
                account: null,
                details: null,
                refunds: """[{"refundId":"1115846166","state":"END","amount":100.0}]""",
                state: "ref"));

        var row = (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem();

        row.MatchState.ShouldBe(PscbPaymentMatchState.RefundMissing);
        row.RestoreBlockReason.ShouldBeNull();
        row.ClaimUrl.ShouldBe("https://joinrpg.test/42/claim/9/edit");
    }

    [Fact]
    public async Task RefusesToRestoreWhenThePayerIsNotResolved()
    {
        var search = new FakeSearchService();
        search.AddProject(7, "Проект");
        var projects = new PscbFakeProjectMetadataRepository();
        projects.AddReadyProject(7);
        var claims = new PscbFakeClaimsRepository();
        claims.AddClaim(7, 42, claimId: 55);
        var service = CreateService(search, new PscbFakeUserRepository(), new FakeFinanceOperationsRepository(), claims, projects, Payment("0000000001", account: "42", details: TicketPurpose("Проект")));

        var row = (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem();

        row.RestoreBlockReason.ShouldBe("плательщик не определён");
        row.ClaimUrl.ShouldBeNull();
    }

    [Fact]
    public async Task RefusesToRestoreWhenTheProjectIsNotResolved()
    {
        var finances = new FakeFinanceOperationsRepository();
        var claims = new PscbFakeClaimsRepository();
        claims.AddClaim(7, 42, claimId: 55);
        var service = CreateService(new FakeSearchService(), Users(42), finances, claims, Payment("0000000001", account: "42", details: TicketPurpose("Проект")));

        (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem()
            .RestoreBlockReason.ShouldBe("проект не определён");
    }

    /// <summary>
    /// Заблокированный проект восстанавливают инструменты DRP, а не админская страница (ADR023).
    /// </summary>
    [Fact]
    public async Task RefusesToRestoreIntoABlockedProject()
    {
        var search = new FakeSearchService();
        search.AddProject(7, "Проект");
        var projects = new PscbFakeProjectMetadataRepository();
        projects.AddBlockedProject(7);
        var claims = new PscbFakeClaimsRepository();
        claims.AddClaim(7, 42, claimId: 55);
        var service = CreateService(search, Users(42), new FakeFinanceOperationsRepository(), claims, projects, Payment("0000000001", account: "42", details: TicketPurpose("Проект")));

        (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem()
            .RestoreBlockReason.ShouldBe("проект заблокирован, его восстанавливают инструментами DRP");
    }

    [Fact]
    public async Task RefusesToRestoreWhenTheProjectIsNotReadyForOnlinePayments()
    {
        var search = new FakeSearchService();
        search.AddProject(7, "Проект");
        var projects = new PscbFakeProjectMetadataRepository();
        projects.AddProjectWithoutOnlinePaymentType(7);
        var claims = new PscbFakeClaimsRepository();
        claims.AddClaim(7, 42, claimId: 55);
        var service = CreateService(search, Users(42), new FakeFinanceOperationsRepository(), claims, projects, Payment("0000000001", account: "42", details: TicketPurpose("Проект")));

        (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem()
            .RestoreBlockReason.ShouldBe("в проекте нет типа онлайн-оплаты");
    }

    /// <summary>
    /// Онлайн-оплату в проекте могли с тех пор выключить: тип оплаты удаляется только мягко,
    /// а платёж случился, когда он работал. Единственный выключенный тип восстановлению не мешает.
    /// </summary>
    [Fact]
    public async Task RestoresIntoADisabledOnlinePaymentType()
    {
        var search = new FakeSearchService();
        search.AddProject(7, "Проект");
        var projects = new PscbFakeProjectMetadataRepository();
        projects.AddProjectWithDisabledOnlinePaymentType(7);
        var claims = new PscbFakeClaimsRepository();
        claims.AddClaim(7, 42, claimId: 55);
        var service = CreateService(search, Users(42), new FakeFinanceOperationsRepository(), claims, projects, Payment("0000000001", account: "42", details: TicketPurpose("Проект")));

        var row = (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem();

        row.MatchState.ShouldBe(PscbPaymentMatchState.Lost);
        row.RestoreBlockReason.ShouldBeNull();
    }

    /// <summary>
    /// Два выключенных типа при ни одном активном — та же неоднозначность, что и два активных:
    /// угадывать, на какой из них пришёл платёж, нельзя.
    /// </summary>
    [Fact]
    public async Task RefusesToRestoreWhenTwoOnlinePaymentTypesAreDisabled()
    {
        var search = new FakeSearchService();
        search.AddProject(7, "Проект");
        var projects = new PscbFakeProjectMetadataRepository();
        projects.AddProjectWithTwoDisabledOnlinePaymentTypes(7);
        var claims = new PscbFakeClaimsRepository();
        claims.AddClaim(7, 42, claimId: 55);
        var service = CreateService(search, Users(42), new FakeFinanceOperationsRepository(), claims, projects, Payment("0000000001", account: "42", details: TicketPurpose("Проект")));

        (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem()
            .RestoreBlockReason.ShouldBe("в проекте несколько типов онлайн-оплаты и ни один не активен");
    }

    /// <summary>
    /// Боевой путь берёт единственный активный онлайн-тип через <c>SingleOrDefault</c>, поэтому
    /// два типа — та же неопределённость, что и ни одного.
    /// </summary>
    [Fact]
    public async Task RefusesToRestoreWhenTheProjectHasTwoOnlinePaymentTypes()
    {
        var search = new FakeSearchService();
        search.AddProject(7, "Проект");
        var projects = new PscbFakeProjectMetadataRepository();
        projects.AddProjectWithTwoOnlinePaymentTypes(7);
        var claims = new PscbFakeClaimsRepository();
        claims.AddClaim(7, 42, claimId: 55);
        var service = CreateService(search, Users(42), new FakeFinanceOperationsRepository(), claims, projects, Payment("0000000001", account: "42", details: TicketPurpose("Проект")));

        (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem()
            .RestoreBlockReason.ShouldBe("в проекте несколько активных типов онлайн-оплаты");
    }

    [Fact]
    public async Task RefusesToRestoreWhenThePayerHasNoClaimInTheProject()
    {
        var search = new FakeSearchService();
        search.AddProject(7, "Проект");
        var projects = new PscbFakeProjectMetadataRepository();
        projects.AddReadyProject(7);
        var service = CreateService(search, Users(42), new FakeFinanceOperationsRepository(), new PscbFakeClaimsRepository(), projects, Payment("0000000001", account: "42", details: TicketPurpose("Проект")));

        var row = (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem();

        row.RestoreBlockReason.ShouldBe("у плательщика нет заявки в проекте");
        row.ClaimUrl.ShouldBeNull();
    }

    /// <summary>
    /// <c>FinanceOperation.MoneyAmount</c> — целые рубли, а JoinRpg всегда отправляет банку целые:
    /// копейки означают чужой платёж, и округление исказило бы деньги.
    /// </summary>
    [Fact]
    public async Task RefusesToRestoreAnAmountWithKopecks()
    {
        var service = CreateService(new FakeSearchService(), Users(42), Payment("0000000001", account: "42", details: null, amount: "100.50"));

        (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem()
            .RestoreBlockReason.ShouldBe("сумма с копейками, а операция хранит целые рубли");
    }

    /// <summary>
    /// Частичных возвратов <c>PaymentsService</c> не реализует, и операция возврата у нас
    /// одна на платёж, поэтому возврат не на всю сумму уходит человеку.
    /// </summary>
    [Fact]
    public async Task RefusesToRestoreAPartialRefund()
    {
        var finances = new FakeFinanceOperationsRepository();
        finances.AddOperation(1);
        var service = CreateService(
            new FakeSearchService(),
            Users(42),
            finances,
            Payment(
                "0000000001",
                account: "42",
                details: null,
                refunds: """[{"refundId":"1115846166","state":"END","amount":40.0}]""",
                state: "ref"));

        (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem()
            .RestoreBlockReason.ShouldBe("частичный возврат");
    }

    /// <summary>
    /// Заявки и метаданные проектов спрашиваются пачками: один запрос на проект, а не на строку.
    /// </summary>
    [Fact]
    public async Task AsksForTheClaimsOncePerProject()
    {
        var search = new FakeSearchService();
        search.AddProject(7, "Проект");
        var projects = new PscbFakeProjectMetadataRepository();
        projects.AddReadyProject(7);
        var claims = new PscbFakeClaimsRepository();
        claims.AddClaim(7, 42, claimId: 55);
        claims.AddClaim(7, 43, claimId: 56);
        var service = CreateService(
            search,
            Users(42, 43),
            new FakeFinanceOperationsRepository(),
            claims,
            projects,
            Payment("0000000001", account: "42", details: TicketPurpose("Проект")),
            Payment("0000000002", account: "43", details: TicketPurpose("Проект")));

        var rows = (await service.GetPayments(Query())).Payments.ToList();

        rows.Count.ShouldBe(2);
        rows.ShouldAllBe(row => row.RestoreBlockReason == null);
        var request = claims.Requests.ShouldHaveSingleItem();
        request.ProjectId.ShouldBe(7);
        request.UserIds.ShouldBe([42, 43], ignoreOrder: true);
        projects.Requests.ShouldBe([7]);
    }

    /// <summary>
    /// Когда терять нечего, за заявками и метаданными проектов сервис не ходит вовсе: у обычного
    /// списка платежей, где всё проведено, эти запросы были бы лишними.
    /// </summary>
    [Fact]
    public async Task DoesNotAskForTheClaimsWhenNothingIsLost()
    {
        var search = new FakeSearchService();
        search.AddProject(7, "Проект");
        var finances = new FakeFinanceOperationsRepository();
        finances.AddOperation(1);
        var claims = new PscbFakeClaimsRepository();
        var projects = new PscbFakeProjectMetadataRepository();
        var service = CreateService(search, Users(42), finances, claims, projects, Payment("0000000001", account: "42", details: TicketPurpose("Проект")));

        var row = (await service.GetPayments(Query())).Payments.ShouldHaveSingleItem();

        row.MatchState.ShouldBe(PscbPaymentMatchState.Settled);
        claims.Requests.ShouldBeEmpty();
        claims.CommentIdRequests.ShouldBeEmpty();
        projects.Requests.ShouldBeEmpty();
    }

    /// <summary>
    /// Поломка обогащения не уносит список: строки банка остаются, исход сверки неизвестен,
    /// а сбой виден по <c>ReconciliationFailed</c> — восстановление по такому списку отказывает,
    /// иначе молчание прочиталось бы как «нечего восстанавливать».
    /// </summary>
    [Fact]
    public async Task ReportsTheReconciliationFailureWithoutLosingTheList()
    {
        var finances = new FakeFinanceOperationsRepository { Throw = new InvalidOperationException("База недоступна") };
        var service = CreateService(
            new FakeSearchService(),
            Users(42),
            finances,
            Payment("0000000001", account: "42", details: null));

        var result = await service.GetPayments(Query());

        result.IsSuccess.ShouldBeTrue();
        result.ReconciliationFailed.ShouldBeTrue();
        var row = result.Payments.ShouldHaveSingleItem();
        row.OrderId.ShouldBe("0000000001");
        row.MatchState.ShouldBeNull();
        row.MatchStateText.ShouldBeNull();
    }

    [Fact]
    public async Task ClearsPartialEnrichmentWhenBuildingTheRestorePlanFails()
    {
        var search = new FakeSearchService();
        search.AddProject(7, "Проект");
        var finances = new FakeFinanceOperationsRepository();
        finances.AddOperation(2);
        finances.AddRefund(2, new DateTime(2026, 5, 12));
        var projects = new PscbFakeProjectMetadataRepository { Throw = new InvalidOperationException("Метаданные недоступны") };
        var service = CreateService(
            search,
            Users(42),
            finances,
            new PscbFakeClaimsRepository(),
            projects,
            Payment("1", account: "42", details: TicketPurpose("Проект")),
            Payment("2", account: "42", details: TicketPurpose("Проект")));

        var (result, plans) = await service.GetPaymentsWithRestorePlan(Query());

        result.IsSuccess.ShouldBeTrue();
        result.ReconciliationFailed.ShouldBeTrue();
        result.Payments.Count.ShouldBe(2);
        plans.ShouldBeEmpty();
        foreach (var row in result.Payments)
        {
            row.Amount.ShouldBe(100);
            row.UserUrl.ShouldBeNull();
            row.ProjectUrl.ShouldBeNull();
            row.RefundUrl.ShouldBeNull();
            row.OperationUrl.ShouldBeNull();
            row.ClaimUrl.ShouldBeNull();
            row.MatchState.ShouldBeNull();
            row.MatchStateText.ShouldBeNull();
            row.RestoreBlockReason.ShouldBeNull();
        }
    }

    private static PscbPaymentsViewService CreateService(FakeSearchService search, PscbFakeUserRepository users, params string[] payments)
        => CreateService(search, users, new FakeFinanceOperationsRepository(), payments);

    private static PscbPaymentsViewService CreateService(
        FakeSearchService search,
        PscbFakeUserRepository users,
        FakeFinanceOperationsRepository finances,
        params string[] payments)
        => CreateService(search, users, finances, new PscbFakeClaimsRepository(), payments);

    private static PscbPaymentsViewService CreateService(
        FakeSearchService search,
        PscbFakeUserRepository users,
        FakeFinanceOperationsRepository finances,
        PscbFakeClaimsRepository claims,
        params string[] payments)
        => CreateService(search, users, finances, claims, new PscbFakeProjectMetadataRepository(), payments);

    private static PscbPaymentsViewService CreateService(
        FakeSearchService search,
        PscbFakeUserRepository users,
        FakeFinanceOperationsRepository finances,
        PscbFakeClaimsRepository claims,
        PscbFakeProjectMetadataRepository projects,
        params string[] payments)
        => new(
            new FakeBankSecrets(),
            new StubHttpClientFactory(payments),
            new FakeUriService(),
            new FakeFinanceOperationUri(),
            new FakeClaimUri(),
            search,
            users,
            finances,
            claims,
            projects,
            NullLogger<PscbPaymentsViewService>.Instance);
}
