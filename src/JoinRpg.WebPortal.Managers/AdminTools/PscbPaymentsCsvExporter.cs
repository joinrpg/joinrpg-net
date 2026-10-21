using System.Globalization;
using JoinRpg.Services.Interfaces;
using JoinRpg.XGameApi.Contract.Pscb;

namespace JoinRpg.WebPortal.Managers.AdminTools;

/// <summary>
/// Колонки выгрузки списка платежей ПСКБ в CSV (ADR025).
/// </summary>
/// <remarks>
/// Заголовки колонок — строки, видимые пользователю, поэтому они русские: так же сделано в
/// <c>CustomExporter</c>. Вложенные коллекции (возвраты и чеки) попадают в ячейку количеством
/// и списком идентификаторов — другого места в строке CSV у них нет. Пустую коллекцию колонки
/// обязаны терпеть: банк возвращает чеки только по флагу <c>requestFiscalData</c>.
/// </remarks>
public class PscbPaymentsCsvExporter : IGeneratorFrontend<PscbPaymentInfo>
{
    /// <summary>
    /// Генератор CSV поверх уже полученных платежей.
    /// </summary>
    /// <remarks>
    /// Строки передаются материализованным списком: <c>TableGenerator.Generate()</c> сначала
    /// строит колонки, а потом перечисляет данные, поэтому ленивая последовательность была бы
    /// перечислена дважды.
    /// </remarks>
    public static IExportGenerator CreateGenerator(IExportDataService exportDataService, IEnumerable<PscbPaymentInfo> payments)
    {
        ArgumentNullException.ThrowIfNull(exportDataService);
        ArgumentNullException.ThrowIfNull(payments);

        return exportDataService.GetGenerator(ExportType.Csv, payments.ToList(), new PscbPaymentsCsvExporter());
    }

    /// <inheritdoc />
    public IEnumerable<ITableColumn> ParseColumns() =>
    [
        new Column("orderId", p => p.OrderId),
        new Column("Показываемый orderId", p => p.DisplayOrderId),
        new Column("paymentId", p => p.PaymentId),
        new Column("transactionId", p => p.TransactionId),
        new Column("Магазин", p => p.MarketPlace),
        new Column("Сумма", p => Money(p.Amount)),
        new Column("Комиссия плательщика", p => Money(p.Fee)),
        new Column("Статус", p => p.Status),
        new Column("Подстатус", p => p.SubStatus),
        new Column("Описание состояния", p => p.LastAction?.Description),
        new Column("Вид", p => p.Kind),
        new Column("Метод", p => p.PaymentMethod),
        new Column("Дата статуса", p => Moment(p.StatusDate)),
        new Column("Плательщик", p => p.CustomerAccount),
        new Column("Email", p => p.CustomerEmail),
        new Column("Телефон", p => p.CustomerPhone),
        new Column("Комментарий плательщика", p => p.CustomerComment),
        new Column("Детали", p => p.Details),
        new Column("Данные магазина", p => p.MerchantData),
        new Column("Токен рекуррента", p => p.RecurrentPaymentToken),
        new Column("Реестр", p => p.RegistryId),
        new Column("Дата реестра", p => Moment(p.RegistryDate)),
        new Column("Номер карты", p => p.Card?.MaskedNumber),
        new Column("Платёжная система", p => p.Card?.CardType),
        new Column("Банк карты", p => p.Card?.Bank),
        new Column("Страна карты", p => p.Card?.Country),
        new Column("Код страны карты", p => p.Card?.CountryCode),
        new Column("Срок действия карты", p => p.Card?.ExpirationDate),
        new Column("RRN", p => p.Card?.ReferenceRetrievalNumber),
        new Column("Статус в процессинге", p => p.Card?.ProcessingStatus),
        new Column("Email держателя карты", p => p.Card?.CardholderEmail),
        new Column("Возвратов", p => p.Refunds.Count),
        new Column("Возвраты", p => Ids(p.Refunds.Select(r => r.RefundId))),
        new Column("Чеков", p => p.Receipts.Count),
        new Column("Чеки", p => Ids(p.Receipts.Select(r => r.ReceiptId))),
    ];

    private static string Money(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string? Money(decimal? value) => value is { } v ? Money(v) : null;

    private static string Moment(DateTimeOffset value)
        => value.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);

    private static string? Moment(DateTimeOffset? value) => value is { } v ? Moment(v) : null;

    private static string Ids(IEnumerable<string?> ids)
        => string.Join(" | ", ids.Where(id => !string.IsNullOrWhiteSpace(id)));

    private sealed class Column(string name, Func<PscbPaymentInfo, object?> getter) : ITableColumn
    {
        public string? Name => name;

        public object? ExtractValue(object row) => getter((PscbPaymentInfo)row);
    }
}
