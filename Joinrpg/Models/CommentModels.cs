using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using JetBrains.Annotations;
using Joinrpg.Markdown;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using CommentExtraAction = JoinRpg.CommonUI.Models.CommentExtraAction;

namespace JoinRpg.Web.Models
{
  public interface IEntityWithCommentsViewModel
  {
    int ProjectId { get; }
    bool HasMasterAccess { get; }
    IReadOnlyCollection<CommentViewModel> RootComments { get; }
    int CommentDiscussionId { get; }
  }

  public static  class CommentViewModelExtensions
  {
    public static List<CommentViewModel> ToCommentTreeViewModel(this CommentDiscussion discussion, int currentUserId)
    {
      return discussion.Comments.Where(comment => comment.ParentCommentId == null)
        .Select(comment => new CommentViewModel(discussion, comment, currentUserId)).OrderBy(c => c.CreatedTime).ToList();
    }
  }

  public class CommentViewModel
  {
    public CommentViewModel(CommentDiscussion parent, Comment comment, int currentUserId)
    {
      IsVisibleToPlayer = comment.IsVisibleToPlayer;
      HasMasterAccess = comment.Project.HasMasterAccess(currentUserId);
      CanModerateFinance = comment.Project.HasMasterAccess(currentUserId, acl => acl.CanManageMoney) ||
                           comment.Finance?.PaymentType?.UserId == currentUserId;
      IsCommentByPlayer = comment.IsCommentByPlayer;
      Author = comment.Author;
      CreatedTime = comment.CreatedAt;
      Finance = comment.Finance;
      CommentText = comment.CommentText.Text.ToHtmlString();
      CommentId = comment.CommentId;
      ProjectId = comment.ProjectId;
      CommentDiscussionId = comment.CommentDiscussionId;
      IsRead = comment.IsReadByUser(currentUserId);
      ChildComments =
        parent.Comments.Where(c => c.ParentCommentId == comment.CommentId)
          .Select(c => new CommentViewModel(parent, c, currentUserId))
          .OrderBy(c => c.CreatedTime);
      ExtraAction = comment.ExtraAction == null ? null : (CommentExtraAction?) comment.ExtraAction.Value;
      IsVisible = comment.IsVisibleTo(currentUserId);
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
    public int CommentDiscussionId { get; }
    public CommentExtraAction? ExtraAction { get; set; }

    public bool ShowFinanceModeration => Finance != null && Finance.RequireModeration && CanModerateFinance;

    public bool IsVisible { get; }
  }

    public enum FinanceOperationActionView
    {
        [Display(Name = "Ничего не делать"), UsedImplicitly]
        None,
        [Display(Name = "Подтвердить операцию"), UsedImplicitly]
        Approve,
        [Display(Name = "Отменить операцию"), UsedImplicitly]
        Decline,
    }

    public class AddCommentViewModel : IValidatableObject
  {
    public int ProjectId { get; set; }
    public int CommentDiscussionId { get; set; }
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

    [Display(Name = "С финансовой операцией...")]
    public FinanceOperationActionView FinanceAction { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
      if (HideFromUser && !EnableHideFromUser)
      {
        yield return new ValidationResult("Нельзя скрыть данный комментарий от игрока");
      }
    }
  }

  public class ClaimOperationViewModel 
  {
    public int ProjectId { get; set; }
    public int ClaimId { get; set; }

    [Required(ErrorMessage = "Заполните текст комментария"), DisplayName("Текст комментария"), UIHint("MarkdownString")]
    public string CommentText { get; set; }

    public int DenialStatus { get; set; }

    public string ActionName { get; set; }
  }
}

