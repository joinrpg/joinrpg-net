using System.ComponentModel;
using JoinRpg.DataModel;
using JoinRpg.Domain;
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

    public FinanceSetupViewModel(Project project, int currentUserId, bool isAdmin, User virtualPaymentsUser)
    {
        IsAdmin = isAdmin;
        ProjectName = project.ProjectName;
        ProjectId = project.ProjectId;
        HasEditAccess = project.HasMasterAccess(currentUserId, acl => acl.CanManageMoney);

        var potentialCashPaymentTypes =
            project.ProjectAcls
                .Where(
                    acl => project.PaymentTypes
                        .Where(pt => pt.TypeKind == PaymentTypeKind.Cash)
                        .All(pt => pt.UserId != acl.UserId))
                .Select(acl => new PaymentTypeListItemViewModel(acl));

        var existedPaymentTypes =
            project.PaymentTypes
                .Where(pt => !pt.TypeKind.IsOnline())
                .Select(pt => new PaymentTypeListItemViewModel(pt));

        var onlinePaymentTypes = new[]
        {
            project.PaymentTypes.Where(pt => pt.TypeKind == PaymentTypeKind.Online)
                .Select(pt => new PaymentTypeListItemViewModel(pt))
                .SingleOrDefault() ?? new PaymentTypeListItemViewModel(PaymentTypeKind.Online, virtualPaymentsUser, project.ProjectId),
            project.PaymentTypes.Where(pt => pt.TypeKind == PaymentTypeKind.OnlineSubscription)
                .Select(pt => new PaymentTypeListItemViewModel(pt))
                .SingleOrDefault() ?? new PaymentTypeListItemViewModel(PaymentTypeKind.OnlineSubscription, virtualPaymentsUser, project.ProjectId),
        };

        PaymentTypes =
            onlinePaymentTypes.Union(
                existedPaymentTypes.Union(potentialCashPaymentTypes)
                    .OrderBy(li => !li.IsActive)
                    .ThenBy(li => !li.IsDefault)
                    .ThenBy(li => li.TypeKind != PaymentTypeKindViewModel.Custom)
                    .ThenBy(li => li.Name))
                .ToList();

        FeeSettings = project.ProjectFeeSettings.OrderBy(pfs => pfs.StartDate.Date)
            .Select(fs => new ProjectFeeSettingListItemViewModel(fs))
            .ToList();

        CurrentUserToken = project.ProjectAcls.Single(acl => acl.UserId == currentUserId)
            .Token.ToHexString();

        GlobalSettings = new FinanceGlobalSettingsViewModel
        {
            ProjectId = ProjectId,
            WarnOnOverPayment = project.Details.FinanceWarnOnOverPayment,
            PreferentialFeeEnabled = project.Details.PreferentialFeeEnabled,
            PreferentialFeeConditions = project.Details.PreferentialFeeConditions.Contents,
        };
    }
}
