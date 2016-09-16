using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Joinrpg.Markdown;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using CommentExtraAction = JoinRpg.CommonUI.Models.CommentExtraAction;

namespace JoinRpg.Web.Models
{
  public class CommentViewModel
  {
    public CommentViewModel(Comment comment, int currentUserId)
    {
      IsVisibleToPlayer = comment.IsVisibleToPlayer;
      HasMasterAccess = comment.Claim.HasMasterAccess(currentUserId);
      CanModerateFinance = comment.Claim.HasMasterAccess(currentUserId, acl => acl.CanManageMoney) ||
                           comment.Finance?.PaymentType?.UserId == currentUserId;
      IsCommentByPlayer = comment.IsCommentByPlayer;
      Author = comment.Author;
      CreatedTime = comment.CreatedTime;
      Finance = comment.Finance;
      CommentText = comment.CommentText.Text.ToHtmlString();
      CommentId = comment.CommentId;
      ProjectId = comment.ProjectId;
      ClaimId = comment.ClaimId;
      IsRead = comment.IsReadByUser(currentUserId);
      ChildComments = comment.ChildsComments.Select(c => new CommentViewModel(c, currentUserId)).OrderBy(c => c.CreatedTime);
      ExtraAction = comment.ExtraAction == null ? null : (CommentExtraAction?) comment.ExtraAction.Value;
    }

    public bool IsRead { get; }
    public bool IsVisibleToPlayer { get; }
    public bool HasMasterAccess { get;}
    private bool CanModerateFinance { get; }
    public bool IsCommentByPlayer { get; }
    public User Author { get; }
    public DateTime CreatedTime { get; }
    public FinanceOperation Finance { get; }
    public IHtmlString CommentText { get; }
    public int CommentId { get; }
    public IEnumerable<CommentViewModel> ChildComments { get; }
    public int ProjectId { get;  }
    public int ClaimId { get; }
    public CommentExtraAction? ExtraAction { get; set; }

    public bool ShowFinanceModeration => Finance != null && Finance.RequireModeration && CanModerateFinance;

    public bool IsVisible => IsVisibleToPlayer || HasMasterAccess;
  }

  public class AddCommentViewModel : IValidatableObject
  {
    public int ProjectId { get; set; }
    public int ClaimId { get; set; }
    /// <summary>
    /// Parent comment id
    /// </summary>
    public int? ParentCommentId { get; set; }

    [Required (ErrorMessage="Заполните текст комментария"), DisplayName("Текст комментария"),UIHint("MarkdownString")] 
    public string CommentText { get; set; }

    [DisplayName("Только другим мастерам")]
    public bool HideFromUser { get; set; }

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

