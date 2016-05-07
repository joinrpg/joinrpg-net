using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.CommonTypes;
using CommentExtraAction = JoinRpg.CommonUI.Models.CommentExtraAction;

namespace JoinRpg.Web.Models
{
  public class CommentViewModel
  {
    public CommentViewModel(Comment comment, int currentUserId)
    {
      IsVisibleToPlayer = comment.IsVisibleToPlayer;
      HasMasterAccess = comment.Claim.HasMasterAccess(currentUserId);
      HasManageMoney = comment.Claim.HasMasterAccess(currentUserId, acl => acl.CanManageMoney);
      CanModerateFinance = HasManageMoney || comment.Finance?.PaymentType?.UserId == currentUserId;
      IsCommentByPlayer = comment.IsCommentByPlayer;
      Author = comment.Author;
      CreatedTime = comment.CreatedTime;
      Finance = comment.Finance;
      CommentText = new MarkdownViewModel(comment.CommentText.Text);
      CommentId = comment.CommentId;
      ProjectId = comment.ProjectId;
      ClaimId = comment.ClaimId;
      IsRead = comment.IsReadByUser(currentUserId);
      ChildComments = comment.ChildsComments.Select(c => new CommentViewModel(c, currentUserId));
      ExtraAction = comment.ExtraAction == null ? null : (CommentExtraAction?) comment.ExtraAction.Value;
    }

    public bool IsRead { get; set; }
    public bool IsVisibleToPlayer { get; set; }
    public bool HasMasterAccess { get; set; }
    public bool HasManageMoney { get; set; }
    public bool CanModerateFinance { get; set; }
    public bool IsCommentByPlayer
    { get; set; }
    public User Author
    { get; set; }
    public DateTime CreatedTime
    { get; set; }
    public FinanceOperation Finance
    { get; set; }
    public MarkdownViewModel CommentText
    { get; set; }
    public int CommentId
    { get; set; }
    public IEnumerable<CommentViewModel> ChildComments
    { get; set; }
    public int ProjectId
    { get; set; }
    public int ClaimId
    { get; set; }
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

    [ReadOnly(true)]
    public CommentViewModel ParentComment { get; set; }

    [Required (ErrorMessage="Заполните текст комментария"), DisplayName("Текст комментария")] 
    public MarkdownViewModel CommentText { get; set; }

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
