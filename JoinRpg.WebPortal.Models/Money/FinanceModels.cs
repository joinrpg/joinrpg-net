using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using JoinRpg.CommonUI.Models;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Helpers.Validation;
using JoinRpg.Helpers.Web;
using JoinRpg.Web.Models.CharacterGroups;
using JoinRpg.Web.Models.Money;

namespace JoinRpg.Web.Models
{

    public static class FinanceOperationStateViewExtension
    {

        /// <summary>
        /// Returns title of operation state
        /// </summary>
        public static string ToTitleString(this FinanceOperationState self)
        {
            return ((FinanceOperationStateViewModel)self).GetDisplayName();
        }

        /// <summary>
        /// Returns row class for detailed payments view
        /// </summary>
        public static string ToRowClass(this FinanceOperationState self)
        {
            switch (self)
            {
                case FinanceOperationState.Proposed:
                    return "unapprovedPayment";
                case FinanceOperationState.Declined:
                    return "unapprovedPayment danger";
                case FinanceOperationState.Approved:
                    return "";
                default:
                    throw new ArgumentOutOfRangeException(nameof(self));
            }
        }
    }


    public class FinanceViewModelBase : AddCommentViewModel
    {
        [Display(Name = "Дата внесения"), Required, DateShouldBeInPast]
        public DateTime OperationDate { get; set; }

        [ReadOnly(true)]
        public bool ClaimApproved { get; set; }

        public int ClaimId { get; set; }
    }

    public class MarkMeAsPreferentialViewModel : FinanceViewModelBase
    {
        public JoinHtmlString PreferentialFeeConditions { get; set; }
    }

  public class FeeAcceptanceViewModel : FinanceViewModelBase
  {
    [Display(Name = "Внесено денег"), Required]
    public int Money
    { get; set; }
    public int FeeChange
    { get; set; }

    [Display(Name = "Кому и как оплачено"), Required]
    public int PaymentTypeId
    { get; set; }

    [ReadOnly(true)]
    public IEnumerable<PaymentType> PaymentTypes { get; set; }

    [ReadOnly(true)]
    public bool HasUnApprovedPayments { get; set; }
  }

  public abstract class PaymentTypeViewModelBase
  {
    public int ProjectId { get; set; }
    [Display(Name = "Название метода оплаты"), Required]
    public string Name { get; set; }
  }

  public class EditPaymentTypeViewModel : PaymentTypeViewModelBase
  {
    [Display(Name = "Предлагать по умолчанию")]
    public bool IsDefault { get; set; }
    public int PaymentTypeId { get; set; }
  }

  public class CreatePaymentTypeViewModel : PaymentTypeViewModelBase
  {
    
    [Display(Name = "Мастер", Description = "Укажите здесь мастера, которому принадлежит карточка, на которую будут переводить деньги")]
    public int UserId { get; set; }
    [ReadOnly(true)]
    public IEnumerable<MasterListItemViewModel> Masters { get; set; }
  }

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

    public FinanceSetupViewModel(Project project, int currentUserId)
    {
      ProjectName = project.ProjectName;
      ProjectId = project.ProjectId;
      HasEditAccess = project.HasMasterAccess(currentUserId, acl => acl.CanManageMoney);

      var potentialCashPaymentTypes =
        project.ProjectAcls
            .Where(acl => project.PaymentTypes.Where(pt => pt.TypeKind == PaymentTypeKind.Cash).All(pt => pt.UserId != acl.UserId))
            .Select(acl => new PaymentTypeListItemViewModel(acl));

      var createdPaymentTypes = project.PaymentTypes.Select(p => new PaymentTypeListItemViewModel(p));

      PaymentTypes =
        createdPaymentTypes.Union(potentialCashPaymentTypes)
          .OrderBy(li => !li.IsActive)
          .ThenBy(li => !li.IsDefault)
          .ThenBy(li => li.IsCash)
          .ThenBy(li => li.Name)
          .ToList();
      Masters = project.GetMasterListViewModel();

      FeeSettings = project.ProjectFeeSettings.Select(fs => new ProjectFeeSettingListItemViewModel(fs)).ToList();

      CurrentUserToken = project.ProjectAcls.Single(acl => acl.UserId == currentUserId).Token.ToHexString();

        GlobalSettings = new FinanceGlobalSettingsViewModel
        {
            ProjectId = ProjectId,
            WarnOnOverPayment = project.Details.FinanceWarnOnOverPayment,
            PreferentialFeeEnabled = project.Details.PreferentialFeeEnabled,
            PreferentialFeeConditions = project.Details.PreferentialFeeConditions.Contents,
        };
    }
  }

  public class PaymentTypeListItemViewModel
  {
    public int? PaymentTypeId { get;  }
    public int ProjectId { get; }

    [Display(Name="Название")]
    public string Name { get; }

    public bool IsCash { get; }

    public bool IsActive { get; }

    public bool IsDefault { get; }

    public bool CanBePermanentlyDeleted { get; }
    public User Master { get; }

    public PaymentTypeListItemViewModel(PaymentType paymentType)
    {
      PaymentTypeId = paymentType.PaymentTypeId;
      ProjectId = paymentType.ProjectId;
      Name = paymentType.GetDisplayName();
      Master = paymentType.User;
      IsCash = paymentType.TypeKind == PaymentTypeKind.Cash;
      IsActive = paymentType.IsActive;
      IsDefault = paymentType.IsDefault;
      CanBePermanentlyDeleted = IsActive && !IsCash && paymentType.CanBePermanentlyDeleted;
    }

    public PaymentTypeListItemViewModel(ProjectAcl acl)
    {
      PaymentTypeId = null;
      ProjectId = acl.ProjectId;
      Name = acl.User.GetCashName();
      Master = acl.User;
      IsCash = true;
      IsActive = false;
      IsDefault = false;
      CanBePermanentlyDeleted = false;
    }
  }

    public abstract class ProjectFeeSettingViewModelBase
    {
        [Display(Name = "Размер взноса")]
        public int Fee { get; set; }

        [Display(Name = "Размер льготного взноса")]
        public int? PreferentialFee { get; set; }

        [Display(Name = "Действует с")]
        public DateTime StartDate { get; set; }

        public int ProjectId { get; set; }
    }

    public class CreateProjectFeeSettingViewModel : ProjectFeeSettingViewModelBase
    {
        public bool PreferentialFeeEnabled { get; set; }
    }

    public class ProjectFeeSettingListItemViewModel : ProjectFeeSettingViewModelBase
    {
        public bool IsActual { get; }
        public int ProjectFeeSettingId { get; }

        public ProjectFeeSettingListItemViewModel(ProjectFeeSetting fs)
        {
            Fee = fs.Fee;
            PreferentialFee = fs.PreferentialFee;
            StartDate = fs.StartDate;
            IsActual = fs.StartDate > DateTime.UtcNow;
            ProjectFeeSettingId = fs.ProjectFeeSettingId;
            ProjectId = fs.ProjectId;
        }
    }

    public class FinanceGlobalSettingsViewModel
    {
        public int ProjectId { get; set; }

        [Display(
            Name = "Предупреждать о переплате в заявках",
            Description =
                "Показывать предупреждение, если в заявке заплачено больше установленного взноса. Выключите, если вы собираете пожертвования и готовы принять любую сумму.")]
        public bool WarnOnOverPayment { get; set; }

        [Display(Name = "Включить льготный взнос",
            Description = "Включить возможность настройки пониженного взноса для некоторых категорий игроков. Мы рекомендуем предоставлять скидку студентам дневных отделений и школьникам.")]
        public bool PreferentialFeeEnabled { get; set; }

        [Display(Name = "Условия льготного взноса",
             Description = " Будет показываться игрокам, претендующим на льготный взнос."),
         UIHint("MarkdownString")]
        public string PreferentialFeeConditions { get; set; }
    }
}
