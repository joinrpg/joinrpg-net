using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
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

    /// <summary>Если задано — GetFeeSettings бросает (тест падения одной панели страницы).</summary>
    public Exception? GetFeeSettingsError { get; set; }

    public Task<FinanceFeeSettingsViewModel> GetFeeSettings(ProjectIdentification projectId)
        => GetFeeSettingsError is not null
            ? Task.FromException<FinanceFeeSettingsViewModel>(GetFeeSettingsError)
            : Task.FromResult(new FinanceFeeSettingsViewModel
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

    public bool HasFeeSettings { get; set; }
    public List<FinancePaymentTypeItemViewModel> PaymentTypes { get; } = [];
    public NewPaymentTypeViewModel? CreatedPaymentType { get; private set; }
    public PaymentTypeIdentification? ToggledPaymentType { get; private set; }

    public Task<FinancePaymentTypesViewModel> GetPaymentTypes(ProjectIdentification projectId)
        => Task.FromResult(new FinancePaymentTypesViewModel
        {
            ProjectId = projectId,
            ProjectStatus = ProjectStatus,
            HasEditAccess = HasEditAccess,
            HasFeeSettings = HasFeeSettings,
            PaymentTypes = [.. PaymentTypes],
        });

    public Task CreatePaymentType(ProjectIdentification projectId, NewPaymentTypeViewModel model)
    {
        ThrowIfRefused();
        CreatedPaymentType = model;
        var index = PaymentTypes.FindIndex(pt => pt.PaymentTypeId is null && pt.Kind == model.Kind && (model.MasterId is null || pt.Master.UserId == model.MasterId));
        var created = new FinancePaymentTypeItemViewModel(new PaymentTypeIdentification(projectId, PaymentTypes.Count + 100), model.Kind,
            model.Name ?? model.Kind.ToString(), IsActive: true, IsDefault: false,
            index >= 0 ? PaymentTypes[index].Master : new UserLinkViewModel(model.MasterId, "Мастер", ViewMode.Show), CanToggle: true);
        if (index >= 0)
        {
            PaymentTypes[index] = created;
        }
        else
        {
            PaymentTypes.Add(created);
        }
        return Task.CompletedTask;
    }

    public Task TogglePaymentType(PaymentTypeIdentification paymentTypeId)
    {
        ThrowIfRefused();
        ToggledPaymentType = paymentTypeId;
        var index = PaymentTypes.FindIndex(pt => pt.PaymentTypeId == paymentTypeId);
        PaymentTypes[index] = PaymentTypes[index] with { IsActive = !PaymentTypes[index].IsActive };
        return Task.CompletedTask;
    }

    public Task<FinanceGoogleDocsExportViewModel> GetGoogleDocsExport(ProjectIdentification projectId)
        => Task.FromResult(new FinanceGoogleDocsExportViewModel(new Uri($"https://joinrpg.ru/{projectId.Value}/money/SummaryByMaster?token=0123")));

    private void ThrowIfRefused()
    {
        if (SaveError is { } error)
        {
            SaveError = null;
            throw new ServerProblemException(error);
        }
    }
}
