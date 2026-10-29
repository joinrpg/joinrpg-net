using System.Globalization;
using JoinRpg.Data.Interfaces.Finances;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.Services.Interfaces;
using JoinRpg.WebPortal.Managers.AdminTools;
using JoinRpg.XGameApi.Contract.Pscb;
using Newtonsoft.Json.Linq;
using PscbApi.Models;

namespace JoinRpg.WebPortal.Managers.Test.AdminTools;

/// <summary>
/// Преобразование ответа банка в контракт шлюза (ADR025), исход сверки строки с нашей базой
/// и набор колонок CSV (ADR027).
/// </summary>
public class PscbPaymentViewModelBuilderTests
{
    private static readonly DateTimeOffset StatusDate = new(2026, 1, 5, 10, 0, 0, TimeSpan.FromHours(3));

    /// <summary>
    /// Ровно те заголовки и ровно в том порядке, что заданы таблицами в ADR025 и ADR027. Порядок
    /// важен: выгрузку сверяют с ними глазами.
    /// </summary>
    private static readonly string[] ExpectedCsvHeaders =
    [
        "Номер платежа",
        "Отображаемый номер платежа",
        "Номер в банке",
        "Номер транзакции",
        "Назначение платежа",
        "Сумма",
        "Комиссия",
        "Статус",
        "Сумма возврата",
        "Статус возврата",
        "Номер возврата в банке",
        "Ссылка на возврат",
        "Дата",
        "Вид",
        "Способ",
        "Пользователь",
        "Ссылка на пользователя",
        "Ссылка на заявку",
        "Email пользователя",
        "Комментарий плательщика",
        "Код ошибки",
        "Код процессинга",
        "Состояние",
        "Ссылка на операцию",
        "Сверка с JoinRpg",
        "Восстановление",
    ];

    [Fact]
    public void MapsPaymentFieldsAndUsesBankIdentifiersForCodedValues()
    {
        var dto = PscbPaymentViewModelBuilder.Build(new PaymentData
        {
            Id = "212157227",
            TransactionId = "TX-1",
            OrderId = "0000000001",
            OrderIdDisplayValue = "1",
            MarketPlace = "47607",
            Amount = 1250.00m,
            Fee = 12.50m,
            Status = PaymentStatus.Paid,
            SubStatus = PaymentSubStatus.HoldConfirmed,
            Type = PscbPaymentType.Payment,
            PaymentMethod = PscbPaymentMethod.BankCards,
            CurrentStatusDate = StatusDate,
            CustomerAccount = "42",
            CustomerComment = "за Васю",
            CustomerEmail = "payer@example.com",
            CustomerPhone = "79000000000",
            Details = "За участие в игре",
            MerchantData = JObject.Parse("""{"claimId":42}"""),
            RecurrentPaymentToken = "TOKEN",
            RegistryId = "R-77",
            RegistryDate = StatusDate,
        });

        dto.PaymentId.ShouldBe("212157227");
        dto.TransactionId.ShouldBe("TX-1");
        dto.OrderId.ShouldBe("0000000001");
        dto.DisplayOrderId.ShouldBe("1");
        dto.MarketPlace.ShouldBe("47607");
        dto.Amount.ShouldBe(1250.00m);
        dto.Fee.ShouldBe(12.50m);
        dto.Status.ShouldBe("end");
        dto.SubStatus.ShouldBe("hold_confirmed");
        dto.Kind.ShouldBe("payment");
        dto.PaymentMethod.ShouldBe("ac");
        dto.StatusDate.ShouldBe(StatusDate);
        dto.CustomerAccount.ShouldBe("42");
        dto.CustomerComment.ShouldBe("за Васю");
        dto.CustomerEmail.ShouldBe("payer@example.com");
        dto.CustomerPhone.ShouldBe("79000000000");
        dto.Details.ShouldBe("За участие в игре");
        dto.MerchantData.ShouldBe("""{"claimId":42}""");
        dto.RecurrentPaymentToken.ShouldBe("TOKEN");
        dto.RegistryId.ShouldBe("R-77");
        dto.RegistryDate.ShouldBe(StatusDate);
    }

    [Fact]
    public void MapsCardRefundsAndReceipts()
    {
        var dto = PscbPaymentViewModelBuilder.Build(new PaymentData
        {
            Id = "11",
            OrderId = "0000000001",
            Card = new BankCardData
            {
                MaskedName = "427655++++++1084",
                Type = "VISA",
                Bank = "SBERBANK",
                Country = "Россия",
                CountryCode = "RUS",
                ExpirationDate = "2204",
                ReferenceRetrievalNumber = "7432467657657768",
                ProcessingStatus = "approved",
                CardholderEmail = "payer@example.com",
            },
            Refunds = [new RefundData { Id = "RF-1", Status = RefundStatus.Completed, Amount = 100m }],
            Receipts = [new ReceiptData { Id = "RC-1", Type = ReceiptType.Income, Amount = 1250m, DocumentNumber = "12" }],
        });

        dto.Card!.MaskedNumber.ShouldBe("427655++++++1084");
        dto.Card.CardType.ShouldBe("VISA");
        dto.Card.ReferenceRetrievalNumber.ShouldBe("7432467657657768");
        dto.Refunds.ShouldHaveSingleItem().RefundId.ShouldBe("RF-1");
        dto.Refunds.Single().Status.ShouldBe("END");
        dto.Receipts.ShouldHaveSingleItem().Type.ShouldBe("income");
    }

    /// <summary>
    /// Банк возвращает разный набор полей в зависимости от статуса платежа и флагов запроса,
    /// поэтому пустая модель обязана преобразовываться, а не падать.
    /// </summary>
    [Fact]
    public void ToleratesABarePayment()
    {
        var dto = PscbPaymentViewModelBuilder.Build(new PaymentData { Id = "11", OrderId = "0000000001" });

        dto.Card.ShouldBeNull();
        dto.MerchantData.ShouldBeNull();
        dto.SubStatus.ShouldBeNull();
        dto.Kind.ShouldBeNull();
        dto.RegistryDate.ShouldBeNull();
        dto.Refunds.ShouldBeEmpty();
        dto.Receipts.ShouldBeEmpty();
    }

    /// <summary>
    /// merchantData приходит не обязательно объектом: его структуру задаёт тот, кто создавал платёж,
    /// а не банк. Значение любого вида должно доехать до админа текстом.
    /// </summary>
    [Fact]
    public void CompactsAMerchantDataThatIsNotAnObject()
    {
        var dto = PscbPaymentViewModelBuilder.Build(new PaymentData
        {
            Id = "11",
            OrderId = "0000000001",
            MerchantData = JValue.CreateString("claimId=42"),
        });

        dto.MerchantData.ShouldBe("\"claimId=42\"");
    }

    [Fact]
    public void BuildsListRowFromTheBankPayment()
    {
        var row = PscbPaymentViewModelBuilder.BuildListRow(new PaymentsListItem
        {
            Id = "1128001119",
            TransactionId = "1128001119",
            OrderId = "0000643617",
            OrderIdDisplayValue = "0000643617",
            Amount = 15000.00m,
            Fee = 366.00m,
            Status = PaymentStatus.Paid,
            Type = PscbPaymentType.Payment,
            PaymentMethod = PscbPaymentMethod.BankCards,
            CurrentStatusDate = StatusDate,
            CustomerAccount = "2658",
            CustomerComment = "за Васю",
            CustomerEmail = "payer@example.com",
            Details = "Билет (организационный взнос) участника на Мастер-Блин 2026: участники",
            LastError = new BankResponseInfo
            {
                Description = "Платеж завершен",
                PaymentCode = PaymentsSystemResponseCode.Success,
                ProcessingCode = ProcessingCenterResponseCode.Approved,
            },
        });

        row.OrderId.ShouldBe("0000643617");
        row.DisplayOrderId.ShouldBe("0000643617");
        row.BankPaymentId.ShouldBe("1128001119");
        row.TransactionId.ShouldBe("1128001119");
        row.ProjectName.ShouldBe("Мастер-Блин 2026: участники");
        row.Amount.ShouldBe(15000.00m);
        row.Fee.ShouldBe(366.00m);
        row.Status.ShouldBe("end");
        row.RefundedByBank.ShouldBeFalse();
        row.StatusDate.ShouldBe(StatusDate);
        row.PaymentKind.ShouldBe("Платёж");
        row.PaymentMethod.ShouldBe("Банковская карта");
        row.UserId.ShouldBe("2658");
        row.UserEmail.ShouldBe("payer@example.com");
        row.PayerComment.ShouldBe("за Васю");
        row.ErrorCode.ShouldBe(0);
        row.ErrorSubCode.ShouldBe(0);
        row.ErrorDescription.ShouldBe("Платеж завершен");
    }

    /// <summary>
    /// Полный возврат банк помечает статусом <c>ref</c> и кладёт в элемент списка массив
    /// <c>refunds</c> — с суммой, статусом и номером возврата (снято с боевого ответа за сентябрь
    /// 2026). При частичном возврате статус остаётся <c>end</c>, поэтому признак говорит только
    /// о полном возврате.
    /// </summary>
    [Fact]
    public void MarksTheRowTheBankReportsAsRefunded()
    {
        var row = PscbPaymentViewModelBuilder.BuildListRow(new PaymentsListItem
        {
            Id = "1115155872",
            OrderId = "0000637705",
            Amount = 13450.00m,
            Status = PaymentStatus.Refunded,
            PaymentMethod = PscbPaymentMethod.BankCards,
            CurrentStatusDate = StatusDate,
            Refunds =
            [
                new RefundData { Id = "1115846166", Status = RefundStatus.Completed, Amount = 13450.00m },
            ],
        });

        row.Status.ShouldBe("ref");
        row.RefundedByBank.ShouldBeTrue();
        var refund = row.Refunds.ShouldHaveSingleItem();
        refund.RefundId.ShouldBe("1115846166");
        refund.Status.ShouldBe("END");
        refund.Amount.ShouldBe(13450.00m);
    }

    /// <summary>
    /// Ссылки и исход сверки проставляет сервис, у которого есть база, IUriService и поиск
    /// по проектам, — Builder обязан оставить их пустыми, иначе их не отличить от настоящих.
    /// </summary>
    [Fact]
    public void LeavesUrlsAndMatchStateForTheServiceToFill()
    {
        var row = PscbPaymentViewModelBuilder.BuildListRow(new PaymentsListItem
        {
            Id = "11",
            OrderId = "0000000001",
            CustomerAccount = "2658",
            Details = "Билет (организационный взнос) участника на Проект",
        });

        row.UserUrl.ShouldBeNull();
        row.ProjectUrl.ShouldBeNull();
        row.RefundUrl.ShouldBeNull();
        row.Refunds.ShouldBeEmpty();
        row.OperationUrl.ShouldBeNull();
        row.ClaimUrl.ShouldBeNull();
        row.MatchState.ShouldBeNull();
        row.MatchStateText.ShouldBeNull();
        row.RestoreBlockReason.ShouldBeNull();
    }

    /// <summary>
    /// Отказы, которые видны по самим данным банка, до всякой базы (ADR027): сумма обязана быть
    /// целым положительным числом рублей, потому что <c>FinanceOperation.MoneyAmount</c> — <c>int</c>,
    /// а возврат — один и на всю сумму, потому что частичных возвратов у нас нет.
    /// </summary>
    [Theory]
    [InlineData("100", false, new double[0], null)]
    [InlineData("100.00", false, new double[0], null)]
    [InlineData("100.50", false, new double[0], "сумма с копейками, а операция хранит целые рубли")]
    [InlineData("0", false, new double[0], "сумма неположительная")]
    [InlineData("-100", false, new double[0], "сумма неположительная")]
    [InlineData("100", true, new[] { 100.0 }, null)]
    [InlineData("100", true, new[] { 40.0 }, "частичный возврат")]
    [InlineData("100", true, new double[0], "банк отметил возврат, но не прислал его данных")]
    [InlineData("100", true, new[] { 50.0, 50.0 }, "несколько возвратов у одного платежа")]
    public void ReportsTheRestoreBlockersVisibleInTheBankData(string amount, bool refundedByBank, double[] refundAmounts, string? expected)
    {
        var row = new PscbPaymentListRow
        {
            OrderId = "0000000001",
            Amount = decimal.Parse(amount, CultureInfo.InvariantCulture),
            RefundedByBank = refundedByBank,
            Refunds = [.. refundAmounts.Select(refundAmount => new PscbRefundInfo { RefundId = "1", Status = "END", Amount = (decimal)refundAmount })],
        };

        PscbPaymentViewModelBuilder.RestoreBlockReasonFromBankData(row).ShouldBe(expected);
    }

    /// <summary>
    /// Колонка «Восстановление» обязана донести причину до выгрузки: по ней админ понимает,
    /// что разбирать руками.
    /// </summary>
    [Fact]
    public void CsvRendersTheRestoreBlockReason()
    {
        var columns = new PscbPaymentsCsvExporter().ParseColumns().ToList();

        ByName(columns, "Восстановление")
            .ExtractValue(new PscbPaymentListRow { OrderId = "0000000001", RestoreBlockReason = "у плательщика нет заявки в проекте" })
            .ShouldBe("у плательщика нет заявки в проекте");
    }

    /// <summary>
    /// Тип оплаты для восстановленного платежа (ADR027): единственный активный подходит, активный
    /// побеждает выключенный, а единственный выключенный подходит тоже — платёж случился, когда
    /// онлайн-оплата работала, и тип оплаты удаляется только мягко.
    /// </summary>
    [Fact]
    public void ChoosesTheSingleOnlinePaymentTypeEvenWhenItIsDisabled()
    {
        var active = PscbPaymentViewModelBuilder.ChooseOnlinePaymentType(Readiness(new OnlinePaymentType(3, IsActive: true)));
        active.PaymentTypeId.ShouldBe(3);
        active.Reason.ShouldBeNull();

        var disabled = PscbPaymentViewModelBuilder.ChooseOnlinePaymentType(Readiness(new OnlinePaymentType(3, IsActive: false)));
        disabled.PaymentTypeId.ShouldBe(3);
        disabled.Reason.ShouldBeNull();

        var mixed = PscbPaymentViewModelBuilder.ChooseOnlinePaymentType(
            Readiness(new OnlinePaymentType(3, IsActive: true), new OnlinePaymentType(4, IsActive: false)));
        mixed.PaymentTypeId.ShouldBe(3);
        mixed.Reason.ShouldBeNull();
    }

    /// <summary>
    /// Неоднозначность восстанавливать нельзя: угадывать тип оплаты за админа инструмент не будет.
    /// </summary>
    [Fact]
    public void RefusesToChooseBetweenSeveralOnlinePaymentTypes()
    {
        PscbPaymentViewModelBuilder.ChooseOnlinePaymentType(
                Readiness(new OnlinePaymentType(3, IsActive: true), new OnlinePaymentType(4, IsActive: true)))
            .Reason.ShouldBe("в проекте несколько активных типов онлайн-оплаты");

        PscbPaymentViewModelBuilder.ChooseOnlinePaymentType(
                Readiness(new OnlinePaymentType(3, IsActive: false), new OnlinePaymentType(4, IsActive: false)))
            .Reason.ShouldBe("в проекте несколько типов онлайн-оплаты и ни один не активен");

        PscbPaymentViewModelBuilder.ChooseOnlinePaymentType(Readiness())
            .Reason.ShouldBe("в проекте нет типа онлайн-оплаты");

        // Проекта нет в ответе репозитория — восстанавливать некуда, как и при пустом наборе типов.
        PscbPaymentViewModelBuilder.ChooseOnlinePaymentType(null)
            .Reason.ShouldBe("в проекте нет типа онлайн-оплаты");
    }

    private static ProjectPaymentsReadiness Readiness(params OnlinePaymentType[] paymentTypes)
        => new(7, IsBlocked: false, paymentTypes);

    /// <summary>
    /// Исход сверки (ADR027): строка банка плюс то, что нашлось у нас по <c>orderId</c>.
    /// Проведённой считается онлайн-операция в состоянии <c>Approved</c> — тот же критерий,
    /// по которому ночная сверка перестаёт спрашивать банк про платёж.
    /// </summary>
    [Theory]
    // Операции нет, а деньги банк получил — это и есть потерянный платёж.
    [InlineData("0000000001", PaymentStatus.Paid, null, null, PscbPaymentMatchState.Lost)]
    // Операция есть и проведена.
    [InlineData("0000000001", PaymentStatus.Paid, FinanceOperationState.Approved, FinanceOperationType.Online, PscbPaymentMatchState.Settled)]
    // Операция есть, но не проведена: инструмент её не трогает, а расхождение показывает.
    [InlineData("0000000001", PaymentStatus.Paid, FinanceOperationState.Proposed, FinanceOperationType.Online, PscbPaymentMatchState.NotSettled)]
    [InlineData("0000000001", PaymentStatus.Paid, FinanceOperationState.Declined, FinanceOperationType.Online, PscbPaymentMatchState.NotSettled)]
    [InlineData("0000000001", PaymentStatus.Paid, FinanceOperationState.Invalid, FinanceOperationType.Online, PscbPaymentMatchState.NotSettled)]
    [InlineData("0000000001", PaymentStatus.Paid, FinanceOperationState.Expired, FinanceOperationType.Online, PscbPaymentMatchState.NotSettled)]
    // Проведённая, но не онлайн-операция: взнос, принятый мастером руками, платежом банка не является.
    [InlineData("0000000001", PaymentStatus.Paid, FinanceOperationState.Approved, FinanceOperationType.Submit, PscbPaymentMatchState.NotSettled)]
    // Денег банк не получил: сверять нечего, даже если операции у нас нет.
    [InlineData("0000000001", PaymentStatus.Expired, null, null, PscbPaymentMatchState.NoMoney)]
    [InlineData("0000000001", PaymentStatus.Error, null, null, PscbPaymentMatchState.NoMoney)]
    [InlineData("0000000001", PaymentStatus.New, null, null, PscbPaymentMatchState.NoMoney)]
    [InlineData("0000000001", PaymentStatus.Hold, null, null, PscbPaymentMatchState.NoMoney)]
    // orderId не число: операции с таким номером быть не может.
    [InlineData("__REPLACED_0000000001", PaymentStatus.Paid, null, null, PscbPaymentMatchState.NoRecord)]
    public void ClassifiesTheBankRowAgainstOurOperation(
        string orderId,
        PaymentStatus bankStatus,
        FinanceOperationState? state,
        FinanceOperationType? type,
        PscbPaymentMatchState expected)
        => PscbPaymentViewModelBuilder.Classify(
                PscbPaymentViewModelBuilder.BuildListRow(new PaymentsListItem { Id = "11", OrderId = orderId, Status = bankStatus }),
                state,
                type)
            .ShouldBe(expected);

    /// <summary>
    /// Пропущенный возврат виден по отсутствию ссылки на нашу операцию возврата: её проставляет
    /// сервис по проведённым возвратам из базы, поэтому сверка идёт после него.
    /// </summary>
    [Fact]
    public void ClassifiesTheRefundTheBankHasButWeHaveNot()
    {
        var row = PscbPaymentViewModelBuilder.BuildListRow(new PaymentsListItem
        {
            Id = "11",
            OrderId = "0000637705",
            Status = PaymentStatus.Refunded,
        });

        PscbPaymentViewModelBuilder.Classify(row, FinanceOperationState.Approved, FinanceOperationType.Online)
            .ShouldBe(PscbPaymentMatchState.RefundMissing);

        row.RefundUrl = "https://joinrpg.test/42/finance/9001";

        PscbPaymentViewModelBuilder.Classify(row, FinanceOperationState.Approved, FinanceOperationType.Online)
            .ShouldBe(PscbPaymentMatchState.Settled);
    }

    /// <summary>
    /// Имя есть у каждого исхода сверки: значение без перевода попало бы в таблицу и в выгрузку
    /// значением перечисления. Приём тот же, что в <c>ProjectRestorePlanViewBuilderTest</c>.
    /// </summary>
    [Fact]
    public void NamesEveryMatchState()
    {
        foreach (var state in Enum.GetValues<PscbPaymentMatchState>())
        {
            var name = PscbPaymentViewModelBuilder.MatchStateName(state);

            name.ShouldNotBeNullOrWhiteSpace();
            // Совпадение со значением перечисления означало бы, что перевода нет и сработал
            // запасной путь.
            name.ShouldNotBe(state.ToString());
        }
    }

    /// <summary>
    /// Строка, которую не сверяли, не выдаёт себя за какой-нибудь из исходов.
    /// </summary>
    [Fact]
    public void LeavesTheMatchStateNameEmptyWhenTheRowWasNotReconciled()
        => PscbPaymentViewModelBuilder.MatchStateName(null).ShouldBeNull();

    [Fact]
    public void SplitsTheBankLastErrorInThreeAndToleratesItsAbsence()
    {
        var withError = PscbPaymentViewModelBuilder.BuildListRow(new PaymentsListItem
        {
            Id = "11",
            OrderId = "0000000001",
            LastError = new BankResponseInfo
            {
                Description = "Платеж обрабатывается",
                PaymentCode = PaymentsSystemResponseCode.Success,
                ProcessingCode = null,
            },
        });

        withError.ErrorCode.ShouldBe(0);
        withError.ErrorSubCode.ShouldBeNull();
        withError.ErrorDescription.ShouldBe("Платеж обрабатывается");

        var withoutError = PscbPaymentViewModelBuilder.BuildListRow(new PaymentsListItem { Id = "11", OrderId = "0000000001" });

        withoutError.ErrorCode.ShouldBeNull();
        withoutError.ErrorSubCode.ShouldBeNull();
        withoutError.ErrorDescription.ShouldBeNull();
    }

    [Theory]
    [InlineData("Билет (организационный взнос) участника на Мастер-Блин 2026: участники", "Мастер-Блин 2026: участники")]
    [InlineData("Билет (организационный взнос) участника на ", "")]
    // В режиме отладки BankApi ставит перед назначением свой маркер.
    [InlineData("[Debug mode] Билет (организационный взнос) участника на Мастер-Блин 2026", "Мастер-Блин 2026")]
    [InlineData("[Debug mode] Подписка пользователя на Сервисы JoinRpg", "Подписка пользователя на Сервисы JoinRpg")]
    // У рекуррентной подписки другое назначение, префикса в нём нет — текст остаётся как есть.
    [InlineData("Подписка пользователя на Сервисы JoinRpg", "Подписка пользователя на Сервисы JoinRpg")]
    [InlineData("", "")]
    public void StripsThePrefixesFromThePurpose(string details, string expected)
        => PscbPaymentViewModelBuilder.ProjectNameFromPurpose(details).ShouldBe(expected);

    [Fact]
    public void StripsNothingFromAMissingPurpose()
        => PscbPaymentViewModelBuilder.ProjectNameFromPurpose(null).ShouldBeNull();

    [Theory]
    [InlineData(PscbPaymentType.Payment, "Платёж")]
    [InlineData(PscbPaymentType.Invoice, "Счёт")]
    public void TranslatesTheRecordKind(PscbPaymentType type, string expected)
        => PscbPaymentViewModelBuilder.BuildListRow(new PaymentsListItem { Id = "11", OrderId = "1", Type = type })
            .PaymentKind.ShouldBe(expected);

    [Fact]
    public void LeavesTheRecordKindEmptyWhenTheBankOmitsIt()
        => PscbPaymentViewModelBuilder.BuildListRow(new PaymentsListItem { Id = "11", OrderId = "1" })
            .PaymentKind.ShouldBeNull();

    [Theory]
    [InlineData(PscbPaymentMethod.BankCards, "Банковская карта")]
    [InlineData(PscbPaymentMethod.FastPaymentsSystem, "СБП")]
    [InlineData(PscbPaymentMethod.PscbWebWallet, "Кошелёк ПСБ")]
    [InlineData(PscbPaymentMethod.PscbTerminal, "Терминал ПСБ")]
    [InlineData(PscbPaymentMethod.Mobile, "Мобильный платёж")]
    [InlineData(PscbPaymentMethod.AlfaClick, "Альфа-Клик")]
    [InlineData(PscbPaymentMethod.Qiwi, "QIWI-кошелёк")]
    [InlineData(PscbPaymentMethod.YandexMoney, "Яндекс.Деньги")]
    [InlineData(PscbPaymentMethod.WebMoney, "WebMoney")]
    public void TranslatesTheKnownPaymentMethods(PscbPaymentMethod method, string expected)
        => PscbPaymentViewModelBuilder.BuildListRow(new PaymentsListItem { Id = "11", OrderId = "1", PaymentMethod = method })
            .PaymentMethod.ShouldBe(expected);

    /// <summary>
    /// Смысл <c>ac-shpa</c> неизвестен — в модели банка он описан как «Bank cards (WTF?)».
    /// Выдуманное название хуже банковского кода, поэтому неизвестный способ проходит насквозь.
    /// </summary>
    [Fact]
    public void PassesAnUnknownPaymentMethodThroughAsTheBankIdentifier()
        => PscbPaymentViewModelBuilder.BuildListRow(new PaymentsListItem
        {
            Id = "11",
            OrderId = "1",
            PaymentMethod = PscbPaymentMethod.BankCardsShpa,
        }).PaymentMethod.ShouldBe("ac-shpa");

    [Fact]
    public void BuildsSuccessfulList()
    {
        var result = PscbPaymentViewModelBuilder.BuildList(
            new PaymentsListInfo
            {
                Status = PaymentInfoQueryStatus.Success,
                RequestId = "TEST-1",
                Payments = [new PaymentsListItem { Id = "11", OrderId = "0000000001" }],
            },
            requestCount: 2);

        result.IsSuccess.ShouldBeTrue();
        result.RequestId.ShouldBe("TEST-1");
        result.RequestCount.ShouldBe(2);
        result.Payments.ShouldHaveSingleItem().OrderId.ShouldBe("0000000001");
    }

    [Fact]
    public void BuildsFailedListWithoutPayments()
    {
        var result = PscbPaymentViewModelBuilder.BuildList(
            new PaymentsListInfo
            {
                Status = PaymentInfoQueryStatus.Failure,
                ErrorCode = ApiErrorCode.ServerError,
                ErrorDescription = "oops",
            },
            requestCount: 1);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("SERVER_ERROR");
        result.ErrorDescription.ShouldBe("oops");
        result.Payments.ShouldBeEmpty();
    }

    [Fact]
    public void BuildsDetailsWithTheBankErrors()
    {
        var result = PscbPaymentViewModelBuilder.BuildDetails(new PaymentInfo
        {
            Status = PaymentInfoQueryStatus.Success,
            Description = "Платеж завершен",
            Payment = new PaymentData { Id = "11", OrderId = "0000000001" },
            Errors = [new BankResponseInfo
            {
                Description = "Declined",
                PaymentCode = PaymentsSystemResponseCode.DeclinedByProcessing,
                ProcessingCode = ProcessingCenterResponseCode.NotSufficientFunds,
            }],
        });

        result.IsSuccess.ShouldBeTrue();
        result.Description.ShouldBe("Платеж завершен");
        result.Payment!.OrderId.ShouldBe("0000000001");
        var error = result.LastErrors.ShouldHaveSingleItem();
        error.Description.ShouldBe("Declined");
        error.PaymentsSystemCode.ShouldBe((int)PaymentsSystemResponseCode.DeclinedByProcessing);
        error.ProcessingCenterCode.ShouldBe((int)ProcessingCenterResponseCode.NotSufficientFunds);
    }

    /// <summary>
    /// Все 15 полей строки обязательны в выгрузке, причём <c>account</c> даёт две колонки
    /// (значение и ссылка рядом), а <c>lastError</c> — три.
    /// </summary>
    [Fact]
    public void CsvColumnsMatchTheAgreedHeadersAndOrder()
    {
        var columns = new PscbPaymentsCsvExporter().ParseColumns().ToList();

        columns.Select(c => c.Name).ShouldBe(ExpectedCsvHeaders);
    }

    /// <summary>
    /// Банк возвращает разный набор полей, поэтому ни одна колонка не имеет права падать на пустой
    /// строке — и не должна терять данные, если заполнено только обязательное.
    /// </summary>
    [Fact]
    public void CsvColumnsTolerateAnEmptyRow()
    {
        var columns = new PscbPaymentsCsvExporter().ParseColumns().ToList();
        var row = new PscbPaymentListRow { OrderId = "0000000001", Amount = 100m };

        foreach (var column in columns)
        {
            Should.NotThrow(() => column.ExtractValue(row));
        }

        ByName(columns, "Комиссия").ExtractValue(row).ShouldBeNull();
        ByName(columns, "Ссылка на пользователя").ExtractValue(row).ShouldBeNull();
        ByName(columns, "Состояние").ExtractValue(row).ShouldBeNull();
        ByName(columns, "Код ошибки").ExtractValue(row).ShouldBeNull();
        ByName(columns, "Сумма возврата").ExtractValue(row).ShouldBeNull();
        ByName(columns, "Статус возврата").ExtractValue(row).ShouldBeNull();
        ByName(columns, "Номер возврата в банке").ExtractValue(row).ShouldBeNull();
        ByName(columns, "Ссылка на возврат").ExtractValue(row).ShouldBeNull();
        ByName(columns, "Ссылка на заявку").ExtractValue(row).ShouldBeNull();
        ByName(columns, "Ссылка на операцию").ExtractValue(row).ShouldBeNull();
        ByName(columns, "Сверка с JoinRpg").ExtractValue(row).ShouldBeNull();
        ByName(columns, "Восстановление").ExtractValue(row).ShouldBeNull();
    }

    [Fact]
    public void CsvColumnsRenderValuesInTheInvariantFormat()
    {
        var columns = new PscbPaymentsCsvExporter().ParseColumns().ToList();
        var row = new PscbPaymentListRow
        {
            OrderId = "0000643617",
            BankPaymentId = "1128001119",
            ProjectName = "Мастер-Блин 2026: участники",
            ProjectUrl = "https://joinrpg.ru/42/home",
            Amount = 1250.50m,
            Fee = 30.05m,
            StatusDate = StatusDate,
            Refunds = [new PscbRefundInfo { RefundId = "1115846166", Status = "END", Amount = 1250.50m }],
            RefundUrl = "https://joinrpg.ru/42/claim/7/finance/9001",
            PaymentKind = "Платёж",
            PaymentMethod = "Банковская карта",
            UserId = "2658",
            UserUrl = "https://joinrpg.ru/user/2658",
            ClaimUrl = "https://joinrpg.ru/42/claim/7/edit",
            OperationUrl = "https://joinrpg.ru/42/claim/7/finance/643617",
            MatchState = PscbPaymentMatchState.Settled,
            MatchStateText = "проведён",
            ErrorCode = 0,
            ErrorSubCode = 51,
            ErrorDescription = "Платеж завершен",
        };

        ByName(columns, "Сумма").ExtractValue(row).ShouldBe("1250.5");
        ByName(columns, "Комиссия").ExtractValue(row).ShouldBe("30.05");
        ByName(columns, "Сумма возврата").ExtractValue(row).ShouldBe("1250.5");
        ByName(columns, "Статус возврата").ExtractValue(row).ShouldBe("END");
        ByName(columns, "Номер возврата в банке").ExtractValue(row).ShouldBe("1115846166");
        ByName(columns, "Ссылка на возврат").ExtractValue(row).ShouldBe("https://joinrpg.ru/42/claim/7/finance/9001");
        ByName(columns, "Дата").ExtractValue(row).ShouldBe("2026-01-05 10:00:00 +03:00");
        ByName(columns, "Пользователь").ExtractValue(row).ShouldBe("2658");
        ByName(columns, "Ссылка на пользователя").ExtractValue(row).ShouldBe("https://joinrpg.ru/user/2658");
        ByName(columns, "Ссылка на заявку").ExtractValue(row).ShouldBe("https://joinrpg.ru/42/claim/7/edit");
        ByName(columns, "Ссылка на операцию").ExtractValue(row).ShouldBe("https://joinrpg.ru/42/claim/7/finance/643617");
        ByName(columns, "Сверка с JoinRpg").ExtractValue(row).ShouldBe("проведён");
        ByName(columns, "Код ошибки").ExtractValue(row).ShouldBe(0);
        ByName(columns, "Код процессинга").ExtractValue(row).ShouldBe(51);
        ByName(columns, "Состояние").ExtractValue(row).ShouldBe("Платеж завершен");
    }

    private static ITableColumn ByName(IReadOnlyCollection<ITableColumn> columns, string name)
        => columns.Single(c => c.Name == name);
}
