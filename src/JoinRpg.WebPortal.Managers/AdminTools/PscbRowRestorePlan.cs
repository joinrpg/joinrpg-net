using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.XGameApi.Contract.Pscb;

namespace JoinRpg.WebPortal.Managers.AdminTools;

/// <summary>
/// План восстановления одной строки списка ПСКБ: на что писать потерянный платёж или пропущенный
/// возврат (ADR026).
/// </summary>
/// <remarks>
/// План заполняет <c>PscbPaymentsViewService</c> вместе с вердиктом восстановления — для строк,
/// которым разрешено автоматическое восстановление. Сама строка списка несёт только ссылки,
/// а записи операции нужны номера заявки и типа оплаты. Ключ плана — ссылка на строку: строки
/// не переопределяют равенство, и <c>orderId</c> уникальным ключом не является.
/// </remarks>
/// <param name="ClaimId">Заявка, на которую ляжет запись. У потерянного платежа — заявка с
/// наименьшим номером среди всех заявок плательщика в проекте; у пропущенного возврата — заявка
/// исходной операции.</param>
/// <param name="PaymentTypeId">Тип оплаты записи. У потерянного платежа — единственный онлайн-тип
/// проекта, активный либо выключенный; у пропущенного возврата — тип исходной операции.</param>
/// <param name="RefundedOperationDate">Дата денег исходного платежа — идёт в текст комментария
/// восстановленного возврата. Заполнена только у строк с пропущенным возвратом: у потерянного
/// платежа дата возврата либо равна дате самого платежа, либо возврат отсутствует.</param>
internal sealed record RowRestorePlan(
    ClaimIdentification ClaimId,
    PaymentTypeIdentification PaymentTypeId,
    DateTime? RefundedOperationDate);

/// <summary>
/// Список платежей банка вместе с планами восстановления строк (ADR026). Планы пусты, если
/// запрос к банку не прошёл или дополнение списка нашими данными не удалось.
/// </summary>
internal sealed record PscbPaymentsListWithRestorePlan(
    PscbPaymentsListResult Result,
    IReadOnlyDictionary<PscbPaymentListRow, RowRestorePlan> Plans);
