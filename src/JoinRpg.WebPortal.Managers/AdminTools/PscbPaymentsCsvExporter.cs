using System.Globalization;
using JoinRpg.Services.Interfaces;
using JoinRpg.XGameApi.Contract.Pscb;

namespace JoinRpg.WebPortal.Managers.AdminTools;

/// <summary>
/// Колонки выгрузки списка платежей ПСБ в CSV (ADR025).
/// </summary>
/// <remarks>
/// <para>
/// Набор и порядок колонок заданы таблицей в ADR025: все 15 полей строки обязательны в выгрузке.
/// Два поля дают больше одной колонки — <c>account</c> даёт значение и ссылку на пользователя
/// рядом с ним, а <c>lastError</c> банка расходится на три колонки.
/// </para>
/// <para>
/// Сверх таблицы идут четыре колонки возврата: сумма, статус и номер — это данные банка из
/// <c>refunds[]</c>, — и ссылка на операцию возврата в JoinRpg. Отдельной колонки под признак
/// полного возврата нет: он и так виден в «Статусе» как <c>ref</c>.
/// </para>
/// <para>
/// Ещё четыре колонки — наши, из сверки списка с базой (ADR028): «Ссылка на заявку» рядом со ссылкой
/// на пользователя, а «Ссылка на операцию», «Сверка с JoinRpg» и «Восстановление» в конце. В «Сверке» —
/// исход словами, а не значение перечисления: выгрузку читают глазами. В «Восстановлении» — причина,
/// по которой строку нельзя восстановить автоматически; пустая ячейка при «потерян» означает,
/// что можно.
/// </para>
/// <para>
/// Заголовки — строки, видимые пользователю, поэтому они русские: так же сделано в
/// <c>CustomExporter</c>. Заголовок «Назначение платежа» сохранён по таблице, хотя значение
/// колонки — название проекта: JoinRpg формирует назначение как
/// «Билет (организационный взнос) участника на {проект}», и префикс обрезается.
/// </para>
/// </remarks>
public class PscbPaymentsCsvExporter : IGeneratorFrontend<PscbPaymentListRow>
{
    /// <summary>
    /// Генератор CSV поверх уже полученных платежей.
    /// </summary>
    /// <remarks>
    /// Строки передаются материализованным списком: <c>TableGenerator.Generate()</c> сначала
    /// строит колонки, а потом перечисляет данные, поэтому ленивая последовательность была бы
    /// перечислена дважды.
    /// </remarks>
    public static IExportGenerator CreateGenerator(IExportDataService exportDataService, IEnumerable<PscbPaymentListRow> payments)
    {
        ArgumentNullException.ThrowIfNull(exportDataService);
        ArgumentNullException.ThrowIfNull(payments);

        return exportDataService.GetGenerator(ExportType.Csv, payments.ToList(), new PscbPaymentsCsvExporter());
    }

    /// <inheritdoc />
    public IEnumerable<ITableColumn> ParseColumns() =>
    [
        new Column("Номер платежа", p => p.OrderId),
        new Column("Отображаемый номер платежа", p => p.DisplayOrderId),
        new Column("Номер в банке", p => p.BankPaymentId),
        new Column("Номер транзакции", p => p.TransactionId),
        new Column("Назначение платежа", p => p.ProjectName),
        new Column("Сумма", p => Money(p.Amount)),
        new Column("Комиссия", p => Money(p.Fee)),
        new Column("Статус", p => p.Status),
        new Column("Сумма возврата", p => RefundSum(p)),
        new Column("Статус возврата", p => Join(p.Refunds.Select(refund => refund.Status))),
        new Column("Номер возврата в банке", p => Join(p.Refunds.Select(refund => refund.RefundId))),
        new Column("Ссылка на возврат", p => p.RefundUrl),
        new Column("Дата", p => Moment(p.StatusDate)),
        new Column("Вид", p => p.PaymentKind),
        new Column("Способ", p => p.PaymentMethod),
        new Column("Пользователь", p => p.UserId),
        new Column("Ссылка на пользователя", p => p.UserUrl),
        new Column("Ссылка на заявку", p => p.ClaimUrl),
        new Column("Email пользователя", p => p.UserEmail),
        new Column("Комментарий плательщика", p => p.PayerComment),
        new Column("Код ошибки", p => p.ErrorCode),
        new Column("Код процессинга", p => p.ErrorSubCode),
        new Column("Состояние", p => p.ErrorDescription),
        new Column("Ссылка на операцию", p => p.OperationUrl),
        new Column("Сверка с JoinRpg", p => p.MatchStateText),
        new Column("Восстановление", p => p.RestoreBlockReason),
    ];

    private static string Money(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string? Money(decimal? value) => value is { } v ? Money(v) : null;

    private static string Moment(DateTimeOffset value)
        => value.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);

    /// <summary>
    /// Сумма возвратов по данным банка. Пустая ячейка, если возвратов нет: ноль означал бы
    /// «возврат на ноль рублей», а не «возврата не было».
    /// </summary>
    private static string? RefundSum(PscbPaymentListRow row)
        => row.Refunds.Count > 0 ? Money(row.Refunds.Sum(refund => refund.Amount)) : null;

    private static string? Join(IEnumerable<string?> values)
    {
        var joined = string.Join("; ", values.Where(value => !string.IsNullOrWhiteSpace(value)));
        return joined.Length > 0 ? joined : null;
    }

    private sealed class Column(string name, Func<PscbPaymentListRow, object?> getter) : ITableColumn
    {
        public string? Name => name;

        public object? ExtractValue(object row) => getter((PscbPaymentListRow)row);
    }
}
