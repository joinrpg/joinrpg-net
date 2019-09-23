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
using JoinRpg.Helpers.Web;
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
    public JoinHtmlString CommentText { get; }
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

    public class CommentTextViewModel
    {

        internal static int LastFormIndex = 0;

        public CommentTextViewModel()
        {
            FormIndex = LastFormIndex;
            LastFormIndex++;
        }

        [Required(ErrorMessage="Заполните текст комментария")]
        [DisplayName("Текст комментария")]
        [UIHint("MarkdownString")]
        public string CommentText { get; set; }

        public bool ShowLabel { get; set; } = true;

        public int FormIndex { get; set; }
    }

    public class AddCommentViewModel : CommentTextViewModel, IValidatableObject
    {

        public static readonly string AddButtonIdTemplate = @"addCommentBtn";

        public static readonly string FormIdTemplate = @"addCommentForm";

        public AddCommentViewModel()
        {
            AddButtonId = AddButtonIdTemplate + FormIndex;
            FormId = FormIdTemplate + FormIndex;
        }

        public AddCommentViewModel(IEntityWithCommentsViewModel source) : this()
        {
            ProjectId = source.ProjectId;
            CommentDiscussionId = source.CommentDiscussionId;
            EnableHideFromUser = source.HasMasterAccess;
            ParentCommentId = null;
            HideFromUser = false;
        }

        public AddCommentViewModel(CommentViewModel source) : this()
        {
            ProjectId = source.ProjectId;
            CommentDiscussionId = source.CommentDiscussionId;
            ParentCommentId = source.CommentId;
            EnableFinanceAction = source.ShowFinanceModeration;
            EnableHideFromUser = source.HasMasterAccess;
            HideFromUser = !source.IsVisibleToPlayer;
        }


        public int ProjectId { get; set; }

        public int CommentDiscussionId { get; set; }
        /// <summary>
        /// Parent comment id
        /// </summary>
        public int? ParentCommentId { get; set; }

        [DisplayName("Только другим мастерам")]
        public bool HideFromUser { get; set; }

        public bool EnableHideFromUser { get; set; }

        public bool EnableFinanceAction { get; set; }

        public string ActionName { get; set; }

        public bool AllowCancel { get; set; } = true;

        [Display(Name = "С финансовой операцией...")]
        public FinanceOperationActionView FinanceAction { get; set; }

        public string AddButtonId { get; }

        public string FormId { get; }

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
        public ClaimStatusView ClaimStatus { get; set; }
        public bool CharacterAutoCreated { get; set; }

        [Required(ErrorMessage = "Заполните текст комментария")]
        [DisplayName("Текст комментария")]
        [UIHint("MarkdownString")]
        public string CommentText { get; set; }

        public string ActionName { get; set; }
    }

    public class MasterDenialOperationViewModel : ClaimOperationViewModel
    {
        [Required(ErrorMessage = "Надо указать причину отказа"),
            Display(Name ="Причина отказа", Description = "Причины отклонения заявки будут видны только мастерам")]
        public ClaimDenialStatusView DenialStatus { get; set; }
        [Display(Name = "Персонажа...")]
        public MasterDenialExtraActionViewModel DeleteCharacter { get; set; }
    }

    public enum MasterDenialExtraActionViewModel
    {
        [Display(Name="...сохранить", Description ="Заявка будет отклонена, но персонаж останется в сетке ролей и другие игроки смогут на него заявиться.")]
        KeepCharacter,
        [Display(Name = "...удалить", Description = "Заявка будет отклонена и персонаж удален и недоступен для заявок. В случае необходимости его можно будет восстановить.")]
        DeleteCharacter,
    }
}

