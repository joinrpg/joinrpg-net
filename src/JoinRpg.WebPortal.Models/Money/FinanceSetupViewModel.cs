using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Helpers;

namespace JoinRpg.Web.Models;

/// <summary>
/// Used for project finance configuration
/// </summary>
public class FinanceSetupViewModel
{
    public string ProjectName { get; }

    public IReadOnlyList<PaymentTypeListItemViewModel> PaymentTypes { get; }

    public IReadOnlyList<ProjectFeeSettingListItemViewModel> FeeSettings { get; }

    public bool HasEditAccess { get; }

    public int ProjectId { get; }

    public string CurrentUserToken { get; }

    public FinanceGlobalSettingsViewModel GlobalSettings { get; }

    [ReadOnly(true)]
    public bool IsAdmin { get; }

    public FinanceSetupViewModel(Project project, ProjectInfo projectInfo, int currentUserId, bool isAdmin, User virtualPaymentsUser)
    {
        IsAdmin = isAdmin;
        ProjectName = project.ProjectName;
        ProjectId = project.ProjectId;
        HasEditAccess = project.HasMasterAccess(new UserIdentification(currentUserId), Permission.CanManageMoney);

        // Типы оплаты и мастера берём из метаданных проекта: там уже есть данные пользователей
        // (UserInfoHeader), поэтому не приходится ходить по EF-навигациям PaymentType.User
        // и ProjectAcl.User — каждая из них тянула отдельный запрос к Users (#4998).
        var financeSettings = projectInfo.ProjectFinanceSettings;
        var virtualPaymentsUserInfo = virtualPaymentsUser.ToUserInfoHeader();

        var potentialCashPaymentTypes =
            projectInfo.Masters
                .Where(
                    master => financeSettings.PaymentTypes
                        .Where(pt => pt.TypeKind == PaymentTypeKind.Cash)
                        .All(pt => pt.User.UserId != master.UserId))
                .Select(master => new PaymentTypeListItemViewModel(master, projectInfo.ProjectId));

        var existedPaymentTypes =
            financeSettings.PaymentTypes
                .Where(pt => !pt.TypeKind.IsOnline())
                .Select(pt => new PaymentTypeListItemViewModel(pt));

        var onlinePaymentTypes = new[]
        {
            financeSettings.PaymentTypes.Where(pt => pt.TypeKind == PaymentTypeKind.Online)
                .Select(pt => new PaymentTypeListItemViewModel(pt))
                .SingleOrDefault() ?? new PaymentTypeListItemViewModel(PaymentTypeKind.Online, virtualPaymentsUserInfo, projectInfo.ProjectId),
            financeSettings.PaymentTypes.Where(pt => pt.TypeKind == PaymentTypeKind.OnlineSubscription)
                .Select(pt => new PaymentTypeListItemViewModel(pt))
                .SingleOrDefault() ?? new PaymentTypeListItemViewModel(PaymentTypeKind.OnlineSubscription, virtualPaymentsUserInfo, projectInfo.ProjectId),
        };

        PaymentTypes =
            onlinePaymentTypes.Union(
                existedPaymentTypes.Union(potentialCashPaymentTypes)
                    .OrderBy(li => !li.IsActive)
                    .ThenBy(li => !li.IsDefault)
                    .ThenBy(li => li.TypeKind != PaymentTypeKindViewModel.Custom)
                    .ThenBy(li => li.Name))
                .ToList();

        FeeSettings = [.. projectInfo.ProjectFinanceSettings.FeeScheduleOrdered
            .Select(fs => new ProjectFeeSettingListItemViewModel(fs, projectInfo.ProjectId))];

        CurrentUserToken = project.ProjectAcls.Single(acl => acl.UserId == currentUserId)
            .Token.ToHexString();

        GlobalSettings = new FinanceGlobalSettingsViewModel
        {
            ProjectId = ProjectId,
            WarnOnOverPayment = financeSettings.WarnOnOverPayment,
            PreferentialFeeEnabled = financeSettings.PreferentialFeeEnabled,
            PreferentialFeeConditions = project.Details.PreferentialFeeConditions.Contents,
        };
    }
}
