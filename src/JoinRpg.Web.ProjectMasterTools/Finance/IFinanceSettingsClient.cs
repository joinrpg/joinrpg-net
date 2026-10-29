using JoinRpg.DomainTypes.ProjectMetadata.Payments;

namespace JoinRpg.Web.ProjectMasterTools.Finance;

/// <summary>
/// Панели страницы «Настройка финансов». Смотреть может любой мастер, менять — с правом на деньги проекта
/// (сервисы проверяют его сами).
/// </summary>
public interface IFinanceSettingsClient
{
    Task<FinanceGeneralSettingsViewModel> GetGeneralSettings(ProjectIdentification projectId);
    Task SaveGeneralSettings(FinanceGeneralSettingsViewModel model);

    Task<FinanceFeeSettingsViewModel> GetFeeSettings(ProjectIdentification projectId);
    Task CreateFeeSetting(ProjectIdentification projectId, CreateFeeSettingViewModel model);
    Task DeleteFeeSetting(ProjectIdentification projectId, int projectFeeSettingId);

    Task<FinancePaymentTypesViewModel> GetPaymentTypes(ProjectIdentification projectId);
    Task CreatePaymentType(ProjectIdentification projectId, NewPaymentTypeViewModel model);
    Task TogglePaymentType(PaymentTypeIdentification paymentTypeId);

    Task<FinanceGoogleDocsExportViewModel> GetGoogleDocsExport(ProjectIdentification projectId);
}
