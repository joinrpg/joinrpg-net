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

namespace JoinRpg.Web.Models
{
    public class FinanceViewModelBase : AddCommentViewModel
    {
        [Display(Name = "Дата внесения"), Required, DateShouldBeInPast]
        public DateTime OperationDate { get; set; }

        [ReadOnly(true)]
        public bool ClaimApproved { get; set; }

        public int ClaimId { get; set; }
    }

    /// <summary>
    /// Used in preferential fee request
    /// </summary>
    public class MarkMeAsPreferentialViewModel : FinanceViewModelBase
    {
        public JoinHtmlString PreferentialFeeConditions { get; set; }

        public MarkMeAsPreferentialViewModel()
        {
            ShowLabel = false;
        }

        public MarkMeAsPreferentialViewModel(ClaimViewModel claim) : this()
        {
            ProjectId = claim.ProjectId;
            ClaimId = claim.ClaimId;
            OperationDate = DateTime.UtcNow;
            CommentDiscussionId = claim.CommentDiscussionId;
            PreferentialFeeConditions = claim.ClaimFee.PreferentialFeeConditions;
        }
    }

    public class PaymentViewModelBase : FinanceViewModelBase
    {
        [Display(Name = "Внесено денег"), Required]
        public int Money { get; set; }

        public int FeeChange { get; set; }

        [Display(Name = "Кому и как оплачено"), Required]
        public int PaymentTypeId { get; set; }

        [ReadOnly(true)]
        public bool HasUnApprovedPayments { get; set; }

        public PaymentViewModelBase() { }

        public PaymentViewModelBase(ClaimViewModel claim)
        {
            CommentText = "";
            ProjectId = claim.ProjectId;
            CommentDiscussionId = claim.ClaimId;
            ClaimId = claim.ClaimId;
            ParentCommentId = null;
            EnableHideFromUser = false;
            HideFromUser = false;
            OperationDate = DateTime.Today;
            Money = Math.Max(claim.ClaimFee.CurrentFee - claim.ClaimFee.CurrentBalance, 0);
            ClaimApproved = claim.Status.IsAlreadyApproved();

            // TODO: Probably must get this info from the list of finance operations?
            HasUnApprovedPayments = claim.HasMasterAccess &&
                claim.RootComments.Any(c => c.ShowFinanceModeration);
        }
    }

    public class PaymentViewModel : PaymentViewModelBase
    {

        [Required]
        public bool AcceptContract { get; set; }

        public PaymentViewModel() { }

        public PaymentViewModel(ClaimViewModel claim) : base(claim)
        {
            ActionName = "Оплатить";
        }
    }

    public class OnlinePaymentViewModel : AddCommentViewModel
    {

    }

    public class SubmitPaymentViewModel : PaymentViewModelBase
    {
        [ReadOnly(true)]
        public IEnumerable<PaymentTypeViewModel> PaymentTypes { get; set; }

        public SubmitPaymentViewModel() { }

        public SubmitPaymentViewModel(ClaimViewModel claim) : base(claim)
        {
            ActionName = "Отметить взнос";
            PaymentTypes = claim.PaymentTypes.GetUserSelectablePaymentTypes();
            CommentText = "Сдан взнос";
        }
    }

  public abstract class PaymentTypeViewModelBase
  {
    public int ProjectId { get; set; }
    [Display(Name = "Название метода оплаты"), Required]
    public string Name { get; set; }
  }

    public class PaymentTypeViewModel : PaymentTypeViewModelBase
    {
        public int PaymentTypeId { get; set; }
        public bool IsDefault { get; set; }
        public PaymentTypeKind Kind { get; set; }
        public int UserId { get; set; }

        public PaymentTypeViewModel() { }

        public PaymentTypeViewModel(PaymentType source)
        {
            PaymentTypeId = source.PaymentTypeId;
            IsDefault = source.IsDefault;
            Kind = source.Kind;
            Name = source.GetDisplayName();
            ProjectId = source.ProjectId;
            UserId = source.UserId;
        }
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
                          .Where(pt => pt.Kind == PaymentTypeKind.Cash)
                          .All(pt => pt.UserId != acl.UserId))
                  .Select(acl => new PaymentTypeListItemViewModel(acl));

          var existedPaymentTypes =
              project.PaymentTypes
                  .Where(pt => pt.Kind != PaymentTypeKind.Online)
                  .Select(pt => new PaymentTypeListItemViewModel(pt));

          var onlinePaymentTypes = new []
          {
              project.PaymentTypes
                  .Where(pt => pt.Kind == PaymentTypeKind.Online)
                  .Select(pt => new PaymentTypeListItemViewModel(pt))
                  .DefaultIfEmpty(new PaymentTypeListItemViewModel(PaymentTypeKind.Online, virtualPaymentsUser, project.ProjectId))
                  .Single()
          };

          PaymentTypes =
              onlinePaymentTypes.Union(
                  existedPaymentTypes.Union(potentialCashPaymentTypes)
                      .OrderBy(li => !li.IsActive)
                      .ThenBy(li => !li.IsDefault)
                      .ThenBy(li => li.Kind != PaymentTypeKind.Custom)
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

  public class TogglePaymentTypeViewModel : IValidatableObject
  {
      public int ProjectId { get; set; }

      public int? PaymentTypeId { get; set; }

      public PaymentTypeKind? Kind { get; set; }

      public int? MasterId { get; set; }

      /// <inheritdoc />
      public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
      {
          if (PaymentTypeId == null && (Kind == null || Kind != PaymentTypeKind.Online && MasterId == null))
                yield return new ValidationResult("Для новых методов оплаты должен быть указан тип и пользователь",
                    new []{ nameof(PaymentTypeId), nameof(Kind), nameof(MasterId) });
      }
  }


  public class PaymentTypeListItemViewModel
  {
    public int? PaymentTypeId { get;  }
    public int ProjectId { get; }

    [Display(Name="Название")]
    public string Name { get; }

    public PaymentTypeKind Kind { get; }

    public bool IsActive { get; }

    [Display(Name="Основной")]
    public bool IsDefault { get; }

    public bool CanBePermanentlyDeleted { get; }

    [Display(Name="Ответственный")]
    public User Master { get; }

    public PaymentTypeListItemViewModel(PaymentType paymentType)
    {
      PaymentTypeId = paymentType.PaymentTypeId;
      ProjectId = paymentType.ProjectId;
      Kind = paymentType.Kind;
      Master = paymentType.User;
      Name = Kind.GetDisplayName(null, paymentType.Name);
      IsActive = paymentType.IsActive;
      IsDefault = paymentType.IsDefault;
      CanBePermanentlyDeleted = IsActive
          && Kind == PaymentTypeKind.Custom
          && paymentType.CanBePermanentlyDeleted;
    }

    public PaymentTypeListItemViewModel(ProjectAcl acl)
    {
      PaymentTypeId = null;
      ProjectId = acl.ProjectId;
      Name = PaymentTypeKind.Cash.GetDisplayName();
      Kind = PaymentTypeKind.Cash;
      Master = acl.User;
      IsActive = false;
      IsDefault = false;
      CanBePermanentlyDeleted = false;
    }

    public PaymentTypeListItemViewModel(PaymentTypeKind kind, User user, int projectId)
    {
        Name = kind.GetDisplayName(user);
        PaymentTypeId = null;
        ProjectId = projectId;
        Master = user;
        Kind = kind;
        CanBePermanentlyDeleted = false;
        IsDefault = false;
        IsActive = false;
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
