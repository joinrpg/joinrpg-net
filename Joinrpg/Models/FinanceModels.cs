using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JoinRpg.DataModel;
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

    [Display(Name = "Кому и как оплачено"), Required]
    public PaymentType PaymentType { get; set; }

    [Display(Name = "Отметил"), Required]
    public User Master { get; set; }

    [Display(Name = "Дата внесения"), Required, DateShouldBeInPast]
    public DateTime OperationDate
    { get; set; }

    [Display(Name = "Заявка"), Required]
    public ClaimListItemViewModel Claim { get; set; }

    public static FinOperationListItemViewModel Create(FinanceOperation fo)
    {
      return new FinOperationListItemViewModel()
      {
        PaymentType = fo.PaymentType,
        Claim = ClaimListItemViewModel.FromClaim(fo.Claim),
        FeeChange = fo.FeeChange,
        Money = fo.MoneyAmount,
        OperationDate = fo.OperationDate,
        FinanceOperationId = fo.CommentId,
        Master = fo.Comment.Author
      };
    }
  }
}
