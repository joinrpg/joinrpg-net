using System.Globalization;
using JoinRpg.Data.Interfaces.Finances;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.XGameApi.Contract.Pscb;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PscbApi;
using PscbApi.Models;

namespace JoinRpg.WebPortal.Managers.AdminTools;

/// <summary>
/// Преобразование ответов банка в контракт шлюза (ADR025).
/// </summary>
/// <remarks>
/// <para>
/// Отдельный статический класс по паттерну Builder из <c>docs/ui-pages-common.md</c>:
/// преобразование проверяется тестами без банка и без репозиториев. Ссылки на пользователя и
/// проект здесь не строятся — им нужны <c>IUriService</c> и поиск, это делает
/// <see cref="PscbPaymentsViewService"/>.
/// </para>
/// <para>
/// Кодированные значения отдаются строками, а не перечислениями: словарь значений принадлежит
/// банку, и наше перечисление пришлось бы править каждый раз, когда банк добавляет значение.
/// Для вида записи и способа оплаты словарь нам известен, поэтому они переводятся в читаемый вид;
/// статус платежа остаётся банковским идентификатором.
/// </para>
/// <para>
/// Здесь же считается исход сверки строки с нашей базой (ADR026). Правило чистое — строка плюс
/// состояние и вид найденной операции, — поэтому оно живёт рядом с остальным преобразованием
/// и проверяется тестами без репозиториев.
/// </para>
/// </remarks>
internal static class PscbPaymentViewModelBuilder
{
    /// <summary>
    /// Префикс назначения платежа, который JoinRpg формирует сам.
    /// </summary>
    /// <remarks>
    /// Литерал задублирован намеренно: он живёт в <c>PaymentsService.GetPurpose</c>
    /// (<c>src/JoinRpg.Services.Impl/PaymentsService.cs</c>), а тот метод помечен
    /// <c>// TODO: We have to reimagine how we get payment purpose</c>. Выносить префикс в общую
    /// константу означало бы трогать боевой путь платежей ради админского шлюза. Если формулировка
    /// назначения платежа изменится, обрезка префикса просто перестанет срабатывать и колонка
    /// покажет текст целиком — данные не потеряются.
    /// </remarks>
    internal const string TicketPurposePrefix = "Билет (организационный взнос) участника на ";

    /// <summary>
    /// Маркер режима отладки, который <see cref="BankApi"/> ставит перед назначением платежа.
    /// </summary>
    /// <remarks>
    /// Задублирован по той же причине, что и <see cref="TicketPurposePrefix"/>: литерал живёт
    /// в <c>BankApi.PrepareMessage</c> (<c>src/PscbApi/BankApi.cs</c>) в боевом пути платежей.
    /// Маркер попадает в назначение только при <c>Debug=true</c>, то есть когда весь список
    /// приходит из песочницы банка, поэтому отдельно в модели он не отмечается.
    /// </remarks>
    internal const string DebugPurposePrefix = "[Debug mode] ";

    /// <summary>
    /// Вид записи по-русски.
    /// </summary>
    private static readonly Dictionary<PscbPaymentType, string> KindNames = new()
    {
        [PscbPaymentType.Payment] = "Платёж",
        [PscbPaymentType.Invoice] = "Счёт",
    };

    /// <summary>
    /// Способ оплаты по-русски, по XML-описаниям членов <see cref="PscbPaymentMethod"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="PscbPaymentMethod.BankCardsShpa"/> (<c>ac-shpa</c>) не переведён намеренно:
    /// в модели банка он описан как «Bank cards (WTF?)», то есть смысл значения неизвестен.
    /// Неизвестный идентификатор отдаётся как есть — банковский код честнее выдуманного названия.
    /// </remarks>
    private static readonly Dictionary<PscbPaymentMethod, string> MethodNames = new()
    {
        [PscbPaymentMethod.BankCards] = "Банковская карта",
        [PscbPaymentMethod.YandexMoney] = "Яндекс.Деньги",
        [PscbPaymentMethod.Qiwi] = "QIWI-кошелёк",
        [PscbPaymentMethod.WebMoney] = "WebMoney",
        [PscbPaymentMethod.AlfaClick] = "Альфа-Клик",
        [PscbPaymentMethod.PscbWebWallet] = "Кошелёк ПСБ",
        [PscbPaymentMethod.PscbTerminal] = "Терминал ПСБ",
        [PscbPaymentMethod.Mobile] = "Мобильный платёж",
        [PscbPaymentMethod.FastPaymentsSystem] = "СБП",
    };

    /// <summary>
    /// Исход сверки по-русски (ADR026). Перечисление наше, а не банковское, поэтому переведены
    /// все значения: тест обходит их и требует непустое имя для каждого.
    /// </summary>
    private static readonly Dictionary<PscbPaymentMatchState, string> MatchStateNames = new()
    {
        [PscbPaymentMatchState.NoRecord] = "не платёж JoinRpg",
        [PscbPaymentMatchState.NoMoney] = "денег нет",
        [PscbPaymentMatchState.Settled] = "проведён",
        [PscbPaymentMatchState.NotSettled] = "не проведён",
        [PscbPaymentMatchState.Lost] = "потерян",
        [PscbPaymentMatchState.RefundMissing] = "нет возврата",
    };

    public static PscbPaymentsListResult BuildList(PaymentsListInfo info, int requestCount)
        => new()
        {
            IsSuccess = IsSuccess(info),
            ErrorCode = IdentifierOf(info.ErrorCode),
            ErrorDescription = info.ErrorDescription,
            RequestId = info.RequestId,
            RequestCount = requestCount,
            Payments = info.Payments?.Select(payment => BuildListRow(payment)).ToList() ?? [],
        };

    public static PscbPaymentsListResult ListFailure(string message, int requestCount)
        => new() { IsSuccess = false, ErrorDescription = message, RequestCount = requestCount };

    public static PscbPaymentDetailsResult BuildDetails(PaymentInfo info)
        => new()
        {
            IsSuccess = IsSuccess(info),
            ErrorCode = IdentifierOf(info.ErrorCode),
            ErrorDescription = info.ErrorDescription,
            RequestId = info.RequestId,
            Description = info.Description,
            Payment = info.Payment is { } payment ? Build(payment) : null,
            LastErrors = info.Errors?.Select(error => Build(error)).ToList() ?? [],
        };

    public static PscbPaymentDetailsResult DetailsFailure(string message)
        => new() { IsSuccess = false, ErrorDescription = message };

    /// <summary>
    /// Строка списка платежей. Ссылки на пользователя и проект, а также сумма, дата и ссылка
    /// возврата остаются незаполненными — их проставляет сервис, у которого есть база и
    /// <c>IUriService</c>.
    /// </summary>
    public static PscbPaymentListRow BuildListRow(PaymentsListItem payment)
        => new()
        {
            OrderId = payment.OrderId,
            DisplayOrderId = payment.OrderIdDisplayValue,
            BankPaymentId = payment.Id,
            TransactionId = payment.TransactionId,
            ProjectName = ProjectNameFromPurpose(payment.Details),
            Amount = payment.Amount,
            Fee = payment.Fee,
            Status = Identifier(payment.Status),
            RefundedByBank = payment.Status == PaymentStatus.Refunded,
            Refunds = payment.Refunds?.Select(refund => Build(refund)).ToList() ?? [],
            StatusDate = payment.CurrentStatusDate,
            PaymentKind = KindName(payment.Type),
            PaymentMethod = MethodName(payment.PaymentMethod),
            UserId = payment.CustomerAccount,
            UserEmail = payment.CustomerEmail,
            PayerComment = payment.CustomerComment,
            ErrorCode = payment.LastError?.PaymentCode is { } code ? (int)code : null,
            ErrorSubCode = payment.LastError?.ProcessingCode is { } subCode ? (int)subCode : null,
            ErrorDescription = payment.LastError?.Description,
        };

    /// <summary>
    /// Название проекта из назначения платежа: без маркера режима отладки и без билетного
    /// префикса. Если префикса нет (например, у рекуррентной подписки), текст возвращается как есть.
    /// </summary>
    public static string? ProjectNameFromPurpose(string? details)
        => StripPrefix(StripPrefix(details, DebugPurposePrefix), TicketPurposePrefix);

    private static string? StripPrefix(string? value, string prefix)
        => value is not null && value.StartsWith(prefix, StringComparison.Ordinal)
            ? value[prefix.Length..]
            : value;

    /// <summary>
    /// Исход сверки строки банка с нашей базой (ADR026).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Состояние и вид операции — <c>null</c>, когда операции с таким <c>orderId</c> в базе нет:
    /// это и есть потерянный платёж. Проведённой считается онлайн-операция в состоянии
    /// <see cref="FinanceOperationState.Approved"/> — тот же критерий, по которому ночная сверка
    /// перестаёт спрашивать банк про платёж.
    /// </para>
    /// <para>
    /// Пропущенный возврат виден по уже проставленной <see cref="PscbPaymentListRow.RefundUrl"/>:
    /// ссылку сервис заполняет по проведённым возвратам из базы, то есть ровно по тому критерию,
    /// по которому здесь считается проведённость платежа.
    /// </para>
    /// </remarks>
    public static PscbPaymentMatchState Classify(
        PscbPaymentListRow row,
        FinanceOperationState? operationState,
        FinanceOperationType? operationType)
    {
        // Значение вроде __REPLACED_0000644272 числом не является: так банк помечает заброшенную
        // попытку, чей orderId магазин использовал повторно (ADR025).
        if (OperationIdFromOrderId(row.OrderId) is null)
        {
            return PscbPaymentMatchState.NoRecord;
        }

        // Деньги банк получил только при end и ref: exp — платёж просрочен, err — не прошёл.
        if (!MoneyReceived(row))
        {
            return PscbPaymentMatchState.NoMoney;
        }

        if (operationState is null || operationType is null)
        {
            return PscbPaymentMatchState.Lost;
        }

        if (operationType != FinanceOperationType.Online || operationState != FinanceOperationState.Approved)
        {
            return PscbPaymentMatchState.NotSettled;
        }

        return row.RefundedByBank && row.RefundUrl is null
            ? PscbPaymentMatchState.RefundMissing
            : PscbPaymentMatchState.Settled;
    }

    /// <summary>
    /// Исход сверки по-русски. Неизвестное значение отдаётся как есть — по тому же правилу, что
    /// и банковские идентификаторы: код честнее выдуманного названия.
    /// </summary>
    public static string? MatchStateName(PscbPaymentMatchState? matchState)
        => matchState is { } state ? MatchStateNames.GetValueOrDefault(state, state.ToString()) : null;

    /// <summary>
    /// Почему строку нельзя восстановить — по самим данным банка, без базы (ADR026).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Сумма обязана быть целым положительным числом рублей: <c>FinanceOperation.MoneyAmount</c> —
    /// <c>int</c>, а JoinRpg всегда отправляет банку целые рубли, поэтому копейки означают чужой
    /// платёж, а округление исказило бы деньги.
    /// </para>
    /// <para>
    /// Возврат обязан быть один и на всю сумму: частичных возвратов <c>PaymentsService</c>
    /// не реализует (<c>NotImplementedException</c>), и операция возврата у нас одна на платёж.
    /// </para>
    /// </remarks>
    public static string? RestoreBlockReasonFromBankData(PscbPaymentListRow row)
    {
        if (row.Amount <= 0)
        {
            return "сумма неположительная";
        }

        if (row.Amount != decimal.Truncate(row.Amount))
        {
            return "сумма с копейками, а операция хранит целые рубли";
        }

        if (!row.RefundedByBank)
        {
            return null;
        }

        return row.Refunds.Count switch
        {
            0 => "банк отметил возврат, но не прислал его данных",
            1 when row.Refunds.Single().Amount != row.Amount => "частичный возврат",
            > 1 => "несколько возвратов у одного платежа",
            _ => null,
        };
    }

    /// <summary>
    /// Тип оплаты, на который ляжет восстановленный платёж, и причина отказа, если выбрать
    /// однозначно нельзя (ADR026).
    /// </summary>
    /// <remarks>
    /// Боевой путь берёт единственный **активный** онлайн-тип через <c>SingleOrDefault</c>. Для
    /// восстановления этого мало: платёж случился, когда онлайн-оплата в проекте была включена,
    /// и тип мог быть с тех пор удалён, — а удаляется тип оплаты только мягко, флагом
    /// <c>IsActive</c>. Поэтому единственный выключенный тип подходит: деньги возвращаются туда,
    /// где они были. Отказывает только неоднозначность — несколько активных типов, либо несколько
    /// выключенных при ни одном активном.
    /// </remarks>
    public static (int? PaymentTypeId, string? Reason) ChooseOnlinePaymentType(ProjectPaymentsReadiness? readiness)
    {
        List<OnlinePaymentType> all = readiness?.OnlinePaymentTypes.ToList() ?? [];

        var active = all.Where(paymentType => paymentType.IsActive).ToList();
        if (active.Count == 1)
        {
            return (active[0].PaymentTypeId, null);
        }

        if (active.Count > 1)
        {
            return (null, "в проекте несколько активных типов онлайн-оплаты");
        }

        if (all.Count == 1)
        {
            return (all[0].PaymentTypeId, null);
        }

        return all.Count == 0
            ? (null, "в проекте нет типа онлайн-оплаты")
            : (null, "в проекте несколько типов онлайн-оплаты и ни один не активен");
    }

    /// <summary>
    /// Номер финансовой операции из <c>orderId</c> банка: это <c>CommentId</c>, дополненный нулями
    /// до десяти знаков (<c>PaymentsService.GetOrderId</c>).
    /// </summary>
    public static int? OperationIdFromOrderId(string? orderId)
        => int.TryParse(orderId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var operationId)
            ? operationId
            : null;

    /// <summary>
    /// Дошёл ли платёж до денег: банк считает завершёнными и оплаченный <c>end</c>, и возвращённый
    /// <c>ref</c>, а просроченный и ошибочный — нет.
    /// </summary>
    private static bool MoneyReceived(PscbPaymentListRow row)
        => row.RefundedByBank || string.Equals(row.Status, Identifier(PaymentStatus.Paid), StringComparison.Ordinal);

    public static PscbPaymentInfo Build(PaymentData payment)
        => new()
        {
            PaymentId = payment.Id,
            TransactionId = payment.TransactionId,
            OrderId = payment.OrderId,
            DisplayOrderId = payment.OrderIdDisplayValue,
            MarketPlace = payment.MarketPlace,
            Amount = payment.Amount,
            Fee = payment.Fee,
            Status = Identifier(payment.Status),
            SubStatus = IdentifierOf(payment.SubStatus),
            Kind = IdentifierOf(payment.Type),
            PaymentMethod = Identifier(payment.PaymentMethod),
            StatusDate = payment.CurrentStatusDate,
            CustomerAccount = payment.CustomerAccount,
            CustomerComment = payment.CustomerComment,
            CustomerEmail = payment.CustomerEmail,
            CustomerPhone = payment.CustomerPhone,
            Details = payment.Details,
            MerchantData = CompactJson(payment.MerchantData),
            RecurrentPaymentToken = payment.RecurrentPaymentToken,
            RegistryId = payment.RegistryId,
            RegistryDate = payment.RegistryDate,
            Card = payment.Card is { } card ? Build(card) : null,
            Refunds = payment.Refunds?.Select(refund => Build(refund)).ToList() ?? [],
            Receipts = payment.Receipts?.Select(receipt => Build(receipt)).ToList() ?? [],
        };

    private static string? KindName(PscbPaymentType? kind)
        => kind is { } k ? KindNames.GetValueOrDefault(k, k.GetIdentifier()) : null;

    private static string MethodName(PscbPaymentMethod method)
        => MethodNames.GetValueOrDefault(method, method.GetIdentifier());

    private static PscbBankCardInfo Build(BankCardData card)
        => new()
        {
            Bank = card.Bank,
            ProcessingStatus = card.ProcessingStatus,
            Country = card.Country,
            CountryCode = card.CountryCode,
            ExpirationDate = card.ExpirationDate,
            CardType = card.Type,
            MaskedNumber = card.MaskedName,
            ReferenceRetrievalNumber = card.ReferenceRetrievalNumber,
            CardholderEmail = card.CardholderEmail,
        };

    private static PscbRefundInfo Build(RefundData refund)
        => new()
        {
            RefundId = refund.Id,
            Status = Identifier(refund.Status),
            Amount = refund.Amount,
        };

    private static PscbReceiptInfo Build(ReceiptData receipt)
        => new()
        {
            ReceiptId = receipt.Id,
            Type = Identifier(receipt.Type),
            DocumentNumber = receipt.DocumentNumber,
            Date = receipt.Date,
            Amount = receipt.Amount,
            RegisterNumber = receipt.RegisterNumber,
            FiscalNumber = receipt.FiscalNumber,
            ShiftNumber = receipt.ShiftNumber,
            ReceiptNumber = receipt.ReceiptNumber,
        };

    private static PscbBankErrorInfo Build(BankResponseInfo error)
        => new()
        {
            Description = error.Description,
            PaymentsSystemCode = error.PaymentCode is { } paymentCode ? (int)paymentCode : null,
            ProcessingCenterCode = error.ProcessingCode is { } processingCode ? (int)processingCode : null,
        };

    /// <summary>
    /// Банк отвечает кодом HTTP 200 и на успех, и на отказ, поэтому статус смотрим в теле ответа.
    /// </summary>
    /// <remarks>
    /// При успехе банк ключ <c>status</c> не присылает вовсе — тело ответа начинается сразу с
    /// <c>payments</c> (проверено на боевом API 2026-10-21). Отсутствие статуса поэтому означает
    /// успех: <see cref="PaymentInfoQueryStatus.Success"/> — нулевое значение перечисления.
    /// </remarks>
    private static bool IsSuccess(PaymentInfoBase info) => info.Status == PaymentInfoQueryStatus.Success;

    private static string Identifier<TEnum>(TEnum value) where TEnum : struct, Enum => value.GetIdentifier();

    private static string? IdentifierOf<TEnum>(TEnum? value) where TEnum : struct, Enum => value?.GetIdentifier();

    private static string? CompactJson(JToken? merchantData)
        => merchantData?.ToString(Formatting.None);
}
