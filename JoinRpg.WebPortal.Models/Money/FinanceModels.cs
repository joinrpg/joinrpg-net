using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
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
        /// <summary>
        /// For online payments, comment text is not actually required
        /// </summary>
        [DisplayName("Комментарий к платежу")]
        public new string CommentText
        {
            get { return base.CommentText; }
            set { base.CommentText = value; }
        }

        [Range(1, 100000, ErrorMessage = "Сумма оплаты должна быть от 1 до 100000")]
        [Required]
        [DisplayName("Сумма к оплате")]
        public new int Money
        {
            get { return base.Money; }
            set { base.Money = value; }
        }

        public bool AcceptContract { get; set; }

        public PaymentViewModel() { }

        public PaymentViewModel(ClaimViewModel claim) : base(claim)
        {
            ActionName = "Оплатить";
        }

        /// <inheritdoc />
        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            => base.Validate(validationContext)
                .AppendIf(
                    !AcceptContract,
                    () => new ValidationResult(
                        "Необходимо принять соглашение для проведения оплаты",
                        new []{ nameof(AcceptContract) }));
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

    public class PaymentTransferViewModel : AddCommentViewModel
    {
        [Display(Name = "Дата внесения"), Required, DateShouldBeInPast]
        public DateTime OperationDate { get; set; }

        [Required]
        public int ClaimId { get; set; }
        
        [Display(Name = "Исходная заявка")]
        public string ClaimName { get; }

        /// <summary>
        /// Id of a claim to transfer money to
        /// </summary>
        [Required]
        [Display(Name = "Заявка для перевода взноса")]
        public int RecipientClaimId { get; set; }

        /// <summary>
        /// Money to transfer
        /// </summary>
        [Required(ErrorMessage = "Необходимо указать сумму перевода")]
        [Range(1, 50000, ErrorMessage = "Сумма перевода должна быть от 1 до 50000")]
        [Display(Name = "Перевести средства")]
        public int Money { get; set; }

        /// <summary>
        /// Max value allowed to transfer from this claim
        /// </summary>
        [Display(Name = "Доступно средств")]
        public int MaxMoney { get; }

        /// <summary>
        /// Comment text
        /// </summary>
        [Required(ErrorMessage = "Заполните текст комментария")]
        [DisplayName("Причина перевода")]
        [Description("Опишите вкратце причину перевода — например, оплата была сделана за несколько людей, или перезачет взноса")]
        [UIHint("MarkdownString")]
        public new string CommentText
        {
            get { return base.CommentText; }
            set { base.CommentText = value; }
        }

        /// <summary>
        /// List of claims to select recipient claim from
        /// </summary>
        public IReadOnlyCollection<RecipientClaimViewModel> Claims { get; set; }

        public PaymentTransferViewModel() {}

        public PaymentTransferViewModel(Claim claim, IEnumerable<Claim> claims) : base()
        {
            OperationDate = DateTime.UtcNow;
            ActionName = "Перевести";
            ClaimId = claim.ClaimId;
            ClaimName = claim.Name;
            ProjectId = claim.ProjectId;
            CommentDiscussionId = claim.CommentDiscussionId;
            MaxMoney = claim.GetPaymentSum();
            Claims = claims
                .Where(c => c.ClaimId != ClaimId)
                .Select(c => new RecipientClaimViewModel(c))
                .OrderBy(c => c.Text)
                .ToArray();
        }
    }
    


    public class RecipientClaimViewModel : SelectListItem
    {
        public int ClaimId { get; }

        public string Name { get; }

        public User Player { get; }

        public string Status { get; }

        public RecipientClaimViewModel(Claim source)
        {
            ClaimId = source.ClaimId;
            Name = source.Name;
            Player = source.Player;
            Status = ((ClaimStatusView) source.ClaimStatus).GetDisplayName();

            Text = $"{Name} ({Status}" + (Player != null ? $", {Player.GetDisplayName()}" : "") + ")";
            Value = ClaimId.ToString();
        }
    }


    /// <summary>
    /// Map of <see cref="PaymentTypeKind"/>
    /// </summary>
    public enum PaymentTypeKindViewModel
    {
        Custom = PaymentTypeKind.Custom,

        [Display(Name = "Наличными")]
        Cash = PaymentTypeKind.Cash,

        [Display(Name = "Онлайн")]
        Online = PaymentTypeKind.Online,
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
        public PaymentTypeKindViewModel TypeKind { get; set; }
        public int UserId { get; set; }

        [ReadOnly(true)]
        public User User { get; }

        public PaymentTypeViewModel() { }

        public PaymentTypeViewModel(PaymentType source)
        {
            PaymentTypeId = source.PaymentTypeId;
            IsDefault = source.IsDefault;
            TypeKind = (PaymentTypeKindViewModel) source.TypeKind;
            Name = source.GetDisplayName();
            ProjectId = source.ProjectId;
            UserId = source.UserId;
            User = source.User;
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
                          .Where(pt => pt.TypeKind == PaymentTypeKind.Cash)
                          .All(pt => pt.UserId != acl.UserId))
                  .Select(acl => new PaymentTypeListItemViewModel(acl));

          var existedPaymentTypes =
              project.PaymentTypes
                  .Where(pt => pt.TypeKind != PaymentTypeKind.Online)
                  .Select(pt => new PaymentTypeListItemViewModel(pt));

          var onlinePaymentTypes = new []
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

  public class TogglePaymentTypeViewModel : IValidatableObject
  {
      public int ProjectId { get; set; }

      public int? PaymentTypeId { get; set; }

      public PaymentTypeKindViewModel? TypeKind { get; set; }

      public int? MasterId { get; set; }

      /// <inheritdoc />
      public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
      {
          if (PaymentTypeId == null && (TypeKind == null || TypeKind != PaymentTypeKindViewModel.Online && MasterId == null))
                yield return new ValidationResult("Для новых методов оплаты должен быть указан тип и пользователь",
                    new []{ nameof(PaymentTypeId), nameof(TypeKind), nameof(MasterId) });
      }
  }


  public class PaymentTypeListItemViewModel
  {
    public int? PaymentTypeId { get;  }
    public int ProjectId { get; }

    [Display(Name="Название")]
    public string Name { get; }

    public PaymentTypeKindViewModel TypeKind { get; }

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
      TypeKind = (PaymentTypeKindViewModel) paymentType.TypeKind;
      Master = paymentType.User;
      Name = TypeKind.GetDisplayName(null, paymentType.Name);
      IsActive = paymentType.IsActive;
      IsDefault = paymentType.IsDefault;
      CanBePermanentlyDeleted = IsActive
          && TypeKind == PaymentTypeKindViewModel.Custom
          && paymentType.CanBePermanentlyDeleted;
    }

    public PaymentTypeListItemViewModel(ProjectAcl acl)
    {
      PaymentTypeId = null;
      ProjectId = acl.ProjectId;
      Name = PaymentTypeKindViewModel.Cash.GetDisplayName();
      TypeKind = PaymentTypeKindViewModel.Cash;
      Master = acl.User;
      IsActive = false;
      IsDefault = false;
      CanBePermanentlyDeleted = false;
    }

    public PaymentTypeListItemViewModel(PaymentTypeKind typeKind, User user, int projectId)
    {
        Name = typeKind.GetDisplayName(user);
        PaymentTypeId = null;
        ProjectId = projectId;
        Master = user;
        TypeKind = (PaymentTypeKindViewModel) typeKind;
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
