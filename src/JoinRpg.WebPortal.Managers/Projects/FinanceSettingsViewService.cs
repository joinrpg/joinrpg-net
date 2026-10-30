using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.ProjectMasterTools.Finance;

namespace JoinRpg.WebPortal.Managers.Projects;

internal class FinanceSettingsViewService(
    IProjectMetadataRepository projectMetadataRepository,
    IProjectFinanceSettingsService financeSettingsService,
    ICurrentUserAccessor currentUserAccessor) : IFinanceSettingsClient
{
    private bool HasEditAccess(ProjectInfo project) => project.HasMasterAccess(currentUserAccessor, Permission.CanManageMoney);

    async Task<FinanceGeneralSettingsViewModel> IFinanceSettingsClient.GetGeneralSettings(ProjectIdentification projectId)
    {
        // Условия льготного взноса — в ProjectDetails, а не в метаданных проекта.
        var details = await projectMetadataRepository.GetProjectDetails(projectId);
        var project = details.ProjectInfo;
        return new FinanceGeneralSettingsViewModel()
        {
            ProjectId = projectId,
            ProjectStatus = project.ProjectStatus,
            HasEditAccess = HasEditAccess(project),
            WarnOnOverPayment = project.ProjectFinanceSettings.WarnOnOverPayment,
            PreferentialFeeEnabled = project.ProjectFinanceSettings.PreferentialFeeEnabled,
            PreferentialFeeConditions = details.PreferentialFeeConditions.Value,
        };
    }

    async Task IFinanceSettingsClient.SaveGeneralSettings(FinanceGeneralSettingsViewModel model)
        => await financeSettingsService.SaveGlobalSettings(new SetFinanceSettingsRequest
        {
            ProjectId = model.ProjectId,
            WarnOnOverPayment = model.WarnOnOverPayment,
            PreferentialFeeEnabled = model.PreferentialFeeEnabled,
            PreferentialFeeConditions = model.PreferentialFeeConditions,
        });

    async Task<FinanceFeeSettingsViewModel> IFinanceSettingsClient.GetFeeSettings(ProjectIdentification projectId)
    {
        var project = await projectMetadataRepository.GetProjectMetadata(projectId);
        var now = DateTime.UtcNow;
        return new FinanceFeeSettingsViewModel()
        {
            ProjectId = projectId,
            ProjectStatus = project.ProjectStatus,
            HasEditAccess = HasEditAccess(project),
            PreferentialFeeEnabled = project.ProjectFinanceSettings.PreferentialFeeEnabled,
            Fees = [.. project.ProjectFinanceSettings.FeeScheduleOrdered
                .Select(fee => new FinanceFeeSettingItemViewModel(
                    fee.ProjectFeeSettingId,
                    fee.Fee,
                    fee.PreferentialFee,
                    DateOnly.FromDateTime(fee.StartDate),
                    // Сервис удаляет только ещё не вступивший в силу взнос
                    CanDelete: fee.StartDate > now))],
        };
    }

    async Task IFinanceSettingsClient.CreateFeeSetting(ProjectIdentification projectId, CreateFeeSettingViewModel model)
        => await financeSettingsService.CreateFeeSetting(new CreateFeeSettingRequest()
        {
            ProjectId = projectId,
            Fee = model.Fee,
            PreferentialFee = model.PreferentialFee,
            StartDate = (model.StartDate ?? throw new ArgumentException("Не указана дата начала взноса", nameof(model)))
                .ToDateTime(TimeOnly.MinValue),
        });

    async Task IFinanceSettingsClient.DeleteFeeSetting(ProjectIdentification projectId, int projectFeeSettingId)
        => await financeSettingsService.DeleteFeeSetting(projectId, projectFeeSettingId);
}
