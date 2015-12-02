using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers.Validation;

namespace JoinRpg.Web.Models
{
  public class FinOperationViewModel : AddCommentViewModel
  {
    [Display(Name = "Внесено денег"), Required]
    public int Money
    { get; set; }
    public int FeeChange
    { get; set; }

    [Display(Name = "Дата внесения"), Required, DateShouldBeInPast]
    public DateTime OperationDate
    { get; set; }

    [Display(Name = "Кому и как оплачено"), Required]
    public int PaymentTypeId
    { get; set; }

    [ReadOnly(true)]

    public IEnumerable<PaymentType> PaymentTypes
    { get; set; }
  }

  public class FinOperationListItemViewModel
  {
    [Display(Name="# операции")]
    public int FinanceOperationId { get; set; }

    [Display(Name = "Внесено денег"), Required]
    public int Money { get; set; }

    [Display(Name = "Изменение взноса"), Required]
    public int FeeChange { get; set; }

    [Display(Name = "Оплачено мастеру")]
    public User PaymentMaster { get; set; }

    [Display(Name = "Способ оплаты"), Required]
    public string PaymentTypeName { get; set; }

    [Display(Name = "Отметил"), Required]
    public User MarkingMaster { get; set; }

    [Display(Name = "Дата внесения"), Required, DateShouldBeInPast]
    public DateTime OperationDate
    { get; set; }

    [Display(Name = "Заявка"), Required]
    public string Claim { get; set; }

    [Url,Display(Name="Ссылка на заявку")]
    public string ClaimLink { get; set; }

    [Display(Name = "Игрок"), Required]
    public User Player
    { get; set; }

    public static FinOperationListItemViewModel Create(FinanceOperation fo, UrlHelper url)
    {
      return new FinOperationListItemViewModel()
      {
        PaymentTypeName = fo.PaymentType.GetDisplayName(),
        PaymentMaster = fo.PaymentType.User,
        Claim = fo.Claim.Name,
        FeeChange = fo.FeeChange,
        Money = fo.MoneyAmount,
        OperationDate = fo.OperationDate,
        FinanceOperationId = fo.CommentId,
        MarkingMaster = fo.Comment.Author,
        Player = fo.Claim.Player,
        ClaimLink = url.Action("Edit", "Claim", new {fo.ProjectId, fo.ClaimId}, url.RequestContext.HttpContext.Request.Url.Scheme)
      };
    }
  }
}
