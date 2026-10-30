using JoinRpg.DomainTypes.ProjectMetadata.Payments;

namespace JoinRpg.Web.ProjectMasterTools.Finance;

public interface IFinanceSettingsUriLocator
{
    /// <summary>Изменение произвольного метода оплаты.</summary>
    Uri GetEditPaymentTypeUri(PaymentTypeIdentification paymentTypeId);
}
