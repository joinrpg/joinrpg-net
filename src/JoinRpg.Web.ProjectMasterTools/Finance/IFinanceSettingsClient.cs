namespace JoinRpg.Web.ProjectMasterTools.Finance;

/// <summary>
/// Панели страницы «Настройка финансов». Смотреть может любой мастер, менять — с правом на деньги проекта
/// (сервисы проверяют его сами).
/// </summary>
public interface IFinanceSettingsClient
{
    Task<FinanceGeneralSettingsViewModel> GetGeneralSettings(ProjectIdentification projectId);
    Task SaveGeneralSettings(FinanceGeneralSettingsViewModel model);
}
