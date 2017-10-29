using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using JoinRpg.CommonUI.Models;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Helpers.Validation;
using JoinRpg.Web.Models.CharacterGroups;

namespace JoinRpg.Web.Models
{

    public static class FinanceOperationStateViewExtension
    {

        /// <summary>
        /// Returns title of operation state
        /// </summary>
        public static string ToTitleString(this FinanceOperationState self)
        {
            switch (self)
            {
                case FinanceOperationState.Approved:
                    return "Подтверждено";
                case FinanceOperationState.Proposed:
                    return "Ожидает подтверждения";
                case FinanceOperationState.Declined:
                    return "Отклонено";
                default:
                    return "";
            }
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
                default:
                    return "";
            }
        }
    }


    public class FinanceViewModelBase : AddCommentViewModel
  {
    [Display(Name = "Дата внесения"), Required, DateShouldBeInPast]
    public DateTime OperationDate
    { get; set; }

    [ReadOnly(true)]
    public bool ClaimApproved { get; set; }
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

    public int ClaimId { get; set; }
  }

  public class FinOperationListViewModel : IOperationsAwareView
  {
    public IReadOnlyCollection<FinOperationListItemViewModel> Items { get; }

    public int? ProjectId { get; }

    public IReadOnlyCollection<int> ClaimIds { get; }
    public IReadOnlyCollection<int> CharacterIds => new int[] {};

    public FinOperationListViewModel(Project project, UrlHelper urlHelper, IReadOnlyCollection<FinanceOperation> operations)
    {
      Items = operations
        .OrderBy(f => f.CommentId)
        .Select(f => new FinOperationListItemViewModel(f, urlHelper)).ToArray();
      ProjectId = project.ProjectId;
      ClaimIds = operations.Select(c => c.ClaimId).Distinct().ToArray();
    }
  }

  public class FinOperationListItemViewModel
  {
    [Display(Name="# операции")]
    public int FinanceOperationId { get; }

    [Display(Name = "Внесено денег"), Required]
    public int Money { get; }

    [Display(Name = "Изменение взноса"), Required]
    public int FeeChange { get;  }

    [Display(Name = "Оплачено мастеру")]
    public User PaymentMaster { get;  }

    [Display(Name = "Способ оплаты"), Required]
    public string PaymentTypeName { get; }

    [Display(Name = "Отметил"), Required]
    public User MarkingMaster { get; }

    [Display(Name = "Дата внесения"), Required, DateShouldBeInPast]
    public DateTime OperationDate { get;  }

    [Display(Name = "Заявка"), Required]
    public string Claim { get; }

    [Url,Display(Name="Ссылка на заявку")]
    public string ClaimLink { get; }

    [Display(Name = "Игрок"), Required]
    public User Player { get; }

    public FinOperationListItemViewModel (FinanceOperation fo, UrlHelper url)
    {
      PaymentTypeName = fo.PaymentType?.GetDisplayName();
      PaymentMaster = fo.PaymentType?.User;
      Claim = fo.Claim.Name;
      FeeChange = fo.FeeChange;
      Money = fo.MoneyAmount;
      OperationDate = fo.OperationDate;
      FinanceOperationId = fo.CommentId;
      MarkingMaster = fo.Comment.Author;
      Player = fo.Claim.Player;
      ClaimLink = url.Action("Edit", "Claim", new {fo.ProjectId, fo.ClaimId},
        url.RequestContext.HttpContext.Request.Url?.Scheme ?? "http");
    }
  }

  public class PaymentTypeSummaryViewModel
  {
    [Display(Name="Способ приема оплаты")]
    public string Name { get; set; }
    [Display(Name = "Мастер")]
    public User Master { get; set; }
    [Display(Name = "Итого")]
    public int Total { get; set; }
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
        project.ProjectAcls.Where(acl => project.PaymentTypes.All(pt => pt.UserId != acl.UserId))
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
        WarnOnOverPayment = project.Details.FinanceWarnOnOverPayment
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
      IsCash = paymentType.IsCash;
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
        [Display(Name = "Взнос")]
        public int Fee { get; set; }
        [Display(Name = "Действует с")]
        public DateTime StartDate { get; set; }

        public int ProjectId { get; set; }
    }

    public class CreateProjectFeeSettingViewModel : ProjectFeeSettingViewModelBase
    {
        public CreateProjectFeeSettingViewModel()
        {
            StartDate = ProjectFeeSetting.MinDate;
        }
    }

  public class ProjectFeeSettingListItemViewModel : ProjectFeeSettingViewModelBase
  {
    public bool IsActual { get; }
    public int ProjectFeeSettingId { get; }

    public ProjectFeeSettingListItemViewModel(ProjectFeeSetting fs)
    {
      Fee = fs.Fee;
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
      Name="Предупреждать о переплате в заявках", 
      Description = "Показывать предупреждение, если в заявке заплачено больше установленного взноса. Выключите, если вы собираете пожертвования и готовы принять любую сумму.")]
    public bool WarnOnOverPayment { get; set; }
  }
}
