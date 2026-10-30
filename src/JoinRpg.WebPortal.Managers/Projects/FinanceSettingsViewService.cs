using JoinRpg.Common.WebComponents;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Models;
using JoinRpg.Web.ProjectMasterTools.Finance;

namespace JoinRpg.WebPortal.Managers.Projects;

internal class FinanceSettingsViewService(
    IProjectMetadataRepository projectMetadataRepository,
    IProjectFinanceSettingsService financeSettingsService,
    ICurrentUserAccessor currentUserAccessor,
    IVirtualUsersService virtualUsersService,
    IProjectRepository projectRepository,
    IFinanceSettingsUriLocator uriLocator) : IFinanceSettingsClient
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

    async Task<FinancePaymentTypesViewModel> IFinanceSettingsClient.GetPaymentTypes(ProjectIdentification projectId)
    {
        var project = await projectMetadataRepository.GetProjectMetadata(projectId);
        var paymentTypes = project.ProjectFinanceSettings.PaymentTypes;

        // Онлайн-оплату включает только админ сайта; выключить её сервис разрешает и мастеру,
        // но обратно он её уже не включит — поэтому кнопку мастеру не показываем вовсе.
        bool CanToggle(PaymentTypeKind kind) => !kind.IsOnline() || currentUserAccessor.IsAdmin;

        FinancePaymentTypeItemViewModel Existing(PaymentTypeInfo pt)
            // Мастер — в своём столбце, поэтому название без него: «Наличными», а не «Наличными — мастер»
            => new(pt.PaymentTypeId, pt.TypeKind, ((PaymentTypeKindViewModel)pt.TypeKind).GetDisplayName(user: null, pt.Name),
                pt.Enabled, pt.IsDefault, new UserLinkViewModel(pt.User), CanToggle(pt.TypeKind));

        // Онлайн-оплата есть у каждого проекта: пока её не включали, показываем строку без id.
        var paymentsUser = new UserLinkViewModel(virtualUsersService.PaymentsUser.ToUserInfoHeader());
        var online = new[] { PaymentTypeKind.Online, PaymentTypeKind.OnlineSubscription }
            .Select(kind => paymentTypes.SingleOrDefault(pt => pt.TypeKind == kind) is { } pt
                ? Existing(pt)
                : new FinancePaymentTypeItemViewModel(null, kind, ((PaymentTypeKindViewModel)kind).GetDisplayName(), false, false, paymentsUser, CanToggle(kind)));

        // Наличные есть у каждого мастера: у кого своего типа ещё нет — строка без id.
        var potentialCash = project.Masters
            .Where(master => !paymentTypes.Any(pt => pt.TypeKind == PaymentTypeKind.Cash && pt.User.UserId == master.UserId))
            .Select(master => new FinancePaymentTypeItemViewModel(null, PaymentTypeKind.Cash, PaymentTypeKindViewModel.Cash.GetDisplayName(),
                false, false, new UserLinkViewModel(master.UserInfo), CanToggle(PaymentTypeKind.Cash)));

        var offline = paymentTypes.Where(pt => !pt.TypeKind.IsOnline()).Select(Existing).Concat(potentialCash)
            .OrderBy(pt => !pt.IsActive)
            .ThenBy(pt => !pt.IsDefault)
            .ThenBy(pt => pt.Kind != PaymentTypeKind.Custom)
            .ThenBy(pt => pt.Name);

        return new FinancePaymentTypesViewModel()
        {
            ProjectId = projectId,
            ProjectStatus = project.ProjectStatus,
            HasEditAccess = HasEditAccess(project),
            HasFeeSettings = project.ProjectFinanceSettings.FeeSchedule.Any(),
            PaymentTypes = [.. online, .. offline],
        };
    }

    async Task IFinanceSettingsClient.CreatePaymentType(ProjectIdentification projectId, NewPaymentTypeViewModel model)
        => await financeSettingsService.CreatePaymentType(new CreatePaymentTypeRequest
        {
            ProjectId = projectId,
            TargetMasterId = model.MasterId,
            TypeKind = model.Kind,
            // Своё имя только у произвольного метода, у остальных оно по виду
            Name = model.Kind == PaymentTypeKind.Custom ? model.Name : null,
        });

    async Task IFinanceSettingsClient.TogglePaymentType(PaymentTypeIdentification paymentTypeId)
        => await financeSettingsService.TogglePaymentActiveness(paymentTypeId);

    async Task<FinanceGoogleDocsExportViewModel> IFinanceSettingsClient.GetGoogleDocsExport(ProjectIdentification projectId)
    {
        // Токен личный: ссылка выгружает деньги проекта без входа на сайт, поэтому отдаём только свой
        var token = await projectRepository.GetMasterExportToken(projectId, currentUserAccessor.UserIdentification)
            ?? throw new NoAccessToProjectException(await projectMetadataRepository.GetProjectMetadata(projectId), currentUserAccessor.UserId);
        return new FinanceGoogleDocsExportViewModel(uriLocator.GetMasterSummaryExportUri(projectId, token.ToHexString()));
    }
}
