using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models
{
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

        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (HideFromUser && !EnableHideFromUser)
            {
                yield return new ValidationResult("Нельзя скрыть данный комментарий от игрока");
            }
        }
    }
}

