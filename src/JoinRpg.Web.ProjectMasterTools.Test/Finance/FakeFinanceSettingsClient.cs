using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.ProjectMasterTools.Finance;

namespace JoinRpg.Web.ProjectMasterTools.Test.Finance;

internal class FakeFinanceSettingsClient : IFinanceSettingsClient
{
    public bool HasEditAccess { get; set; } = true;
    public ProjectLifecycleStatus ProjectStatus { get; set; } = ProjectLifecycleStatus.ActiveClaimsOpen;
    /// <summary>Следующее изменение откажет с этим текстом.</summary>
    public string? SaveError { get; set; }

    public FinanceGeneralSettingsViewModel? SavedGeneralSettings { get; private set; }

    public bool PreferentialFeeEnabled { get; set; }
    public List<FinanceFeeSettingItemViewModel> Fees { get; } = [];
    public CreateFeeSettingViewModel? CreatedFee { get; private set; }
    public int? DeletedFeeId { get; private set; }

    public Task<FinanceGeneralSettingsViewModel> GetGeneralSettings(ProjectIdentification projectId)
        => Task.FromResult(new FinanceGeneralSettingsViewModel
        {
            ProjectId = projectId,
            ProjectStatus = ProjectStatus,
            HasEditAccess = HasEditAccess,
            WarnOnOverPayment = true,
            PreferentialFeeEnabled = false,
            PreferentialFeeConditions = "",
        });

    public Task SaveGeneralSettings(FinanceGeneralSettingsViewModel model)
    {
        ThrowIfRefused();
        SavedGeneralSettings = model;
        return Task.CompletedTask;
    }

    public Task<FinanceFeeSettingsViewModel> GetFeeSettings(ProjectIdentification projectId)
        => Task.FromResult(new FinanceFeeSettingsViewModel
        {
            ProjectId = projectId,
            ProjectStatus = ProjectStatus,
            HasEditAccess = HasEditAccess,
            PreferentialFeeEnabled = PreferentialFeeEnabled,
            Fees = [.. Fees],
        });

    public Task CreateFeeSetting(ProjectIdentification projectId, CreateFeeSettingViewModel model)
    {
        ThrowIfRefused();
        CreatedFee = model;
        Fees.Add(new FinanceFeeSettingItemViewModel(Fees.Count + 1, model.Fee, model.PreferentialFee, model.StartDate!.Value, CanDelete: true));
        return Task.CompletedTask;
    }

    public Task DeleteFeeSetting(ProjectIdentification projectId, int projectFeeSettingId)
    {
        ThrowIfRefused();
        DeletedFeeId = projectFeeSettingId;
        _ = Fees.RemoveAll(fee => fee.ProjectFeeSettingId == projectFeeSettingId);
        return Task.CompletedTask;
    }

    private void ThrowIfRefused()
    {
        if (SaveError is { } error)
        {
            SaveError = null;
            throw new ServerProblemException(error);
        }
    }
}
