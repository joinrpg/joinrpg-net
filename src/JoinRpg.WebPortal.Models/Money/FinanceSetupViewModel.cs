using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Web.Models.CharacterGroups;

namespace JoinRpg.Web.Models
{
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

        [ReadOnly(true)]
        public IEnumerable<MasterListItemViewModel> Masters { get; }

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
                    .Where(pt => pt.TypeKind != PaymentTypeKind.Online)
                    .Select(pt => new PaymentTypeListItemViewModel(pt));

            var onlinePaymentTypes = new[]
            {
              project.PaymentTypes
                  .Where(pt => pt.TypeKind == PaymentTypeKind.Online)
                  .Select(pt => new PaymentTypeListItemViewModel(pt))
                  .DefaultIfEmpty(new PaymentTypeListItemViewModel(PaymentTypeKind.Online, virtualPaymentsUser, project.ProjectId))
                  .Single()
          };

            PaymentTypes =
                onlinePaymentTypes.Union(
                    existedPaymentTypes.Union(potentialCashPaymentTypes)
                        .OrderBy(li => !li.IsActive)
                        .ThenBy(li => !li.IsDefault)
                        .ThenBy(li => li.TypeKind != PaymentTypeKindViewModel.Custom)
                        .ThenBy(li => li.Name))
                    .ToList();

            Masters = project.GetMasterListViewModel();

            FeeSettings = project.ProjectFeeSettings
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
}
