using JoinRpg.DomainTypes.ProjectMetadata.Payments;

namespace JoinRpg.Web.ProjectMasterTools.Finance;

public interface IFinanceSettingsUriLocator
{
    /// <summary>Изменение произвольного метода оплаты.</summary>
    Uri GetEditPaymentTypeUri(PaymentTypeIdentification paymentTypeId);

    /// <summary>Сводка по деньгам мастеров в CSV для IMPORTDATA в Google Таблицах; доступ — по токену мастера, без входа.</summary>
    Uri GetMasterSummaryExportUri(ProjectIdentification projectId, string token);
}
