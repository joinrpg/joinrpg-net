using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models
{
  public class AddCommentViewModel : IValidatableObject
  {
    public int ProjectId { get; set; }
    public int ClaimId { get; set; }
    /// <summary>
    /// Parent comment id
    /// </summary>
    public int? ParentCommentId { get; set; }

    [ReadOnly(true)]
    public Comment ParentComment { get; set; }

    [Required (ErrorMessage="Заполните текст комментария"), DisplayName("Текст комментария")] 
    public MarkdownViewModel CommentText { get; set; }

    [DisplayName("Только другим мастерам")]
    public bool HideFromUser
    { get; set; }

    public bool EnableHideFromUser { get; set; }

    public bool EnableFinanceAction { get; set; }

    public string ActionName { get; set; }

    public FinanceOperationAction FinanceAction { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
      if (HideFromUser && !EnableHideFromUser)
      {
        yield return new ValidationResult("Нельзя скрыть данный комментарий от игрока");
      }
    }
  }
}
