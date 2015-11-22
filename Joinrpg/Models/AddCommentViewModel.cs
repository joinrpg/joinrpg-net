using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers.Validation;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models
{
  public class FinOperationViewModel : AddCommentViewModel
  {
    [Display(Name="Внесено денег"), Required]
    public int Money { get; set; }
    public int FeeChange
    { get; set; }

    [Display(Name="Дата внесения"), Required, DateShouldBeInPast]
    public DateTime OperationDate { get; set; }

    [Display(Name="Кому и как оплачено"), Required]
    public int PaymentTypeId { get; set; }

    [ReadOnly(true)]

    public IEnumerable<PaymentType> PaymentTypes { get; set; }
  }

  public class AddCommentViewModel : IValidatableObject
  {
    public int ProjectId { get; set; }
    public int ClaimId { get; set; }
    /// <summary>
    /// Parent comment id
    /// </summary>
    public int? ParentCommentId { get; set; }

    [Required (ErrorMessage="Заполните текст комментария"), DisplayName("Текст комментария")] 
    public MarkdownViewModel CommentText { get; set; }

    [DisplayName("Только другим мастерам")]
    public bool HideFromUser
    { get; set; }

    public bool EnableHideFromUser { get; set; }

    public string ActionName { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
      if (HideFromUser && !EnableHideFromUser)
      {
        yield return new ValidationResult("Нельзя скрыть данный комментарий от игрока");
      }
    }
  }
}
