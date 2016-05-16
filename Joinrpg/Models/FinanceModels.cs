using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers.Validation;

namespace JoinRpg.Web.Models
{
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
      PaymentTypeName = fo.PaymentType.GetDisplayName();
      PaymentMaster = fo.PaymentType.User;
      Claim = fo.Claim.Name;
      FeeChange = fo.FeeChange;
      Money = fo.MoneyAmount;
      OperationDate = fo.OperationDate;
      FinanceOperationId = fo.CommentId;
      MarkingMaster = fo.Comment.Author;
      Player = fo.Claim.Player;
      ClaimLink = url.Action("Edit", "Claim", new {fo.ProjectId, fo.ClaimId},
        url.RequestContext.HttpContext.Request.Url.Scheme);
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
}
