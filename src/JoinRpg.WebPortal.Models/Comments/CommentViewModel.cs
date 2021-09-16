using System;
using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers.Web;
using JoinRpg.Markdown;
using JoinRpg.PrimitiveTypes;
using CommentExtraAction = JoinRpg.CommonUI.Models.CommentExtraAction;

namespace JoinRpg.Web.Models
{
    public class CommentViewModel
    {
        public CommentViewModel(CommentDiscussion parent, Comment comment, int currentUserId, int deepLevel)
        {
            DeepLevel = deepLevel;
            IsVisibleToPlayer = comment.IsVisibleToPlayer;
            HasMasterAccess = comment.Project.HasMasterAccess(currentUserId);
            CanModerateFinance = comment.Project.HasMasterAccess(currentUserId, acl => acl.CanManageMoney) ||
                                 comment.Finance?.PaymentType?.UserId == currentUserId;
            IsCommentByPlayer = comment.IsCommentByPlayer;
            Author = comment.Author;
            AuthorAvatar = AvatarIdentification.FromOptional(comment.Author.SelectedAvatarId);
            AuthorEmail = comment.Author.Email;
            CreatedTime = comment.CreatedAt;
            Finance = comment.Finance;
            CommentText = comment.CommentText.Text.ToHtmlString();
            CommentId = comment.CommentId;
            ProjectId = comment.ProjectId;
            CommentDiscussionId = comment.CommentDiscussionId;
            IsRead = comment.IsReadByUser(currentUserId);
            ChildComments =
              parent.Comments.Where(c => c.ParentCommentId == comment.CommentId)
                .Select(c => new CommentViewModel(parent, c, currentUserId, deepLevel + 1))
                .OrderBy(c => c.CreatedTime);
            ExtraAction = comment.ExtraAction == null ? null : (CommentExtraAction?)comment.ExtraAction.Value;
            IsVisible = comment.IsVisibleTo(currentUserId);
        }

        public bool IsRead { get; }
        public bool IsVisibleToPlayer { get; }
        public bool HasMasterAccess { get; }
        private bool CanModerateFinance { get; }
        public bool IsCommentByPlayer { get; }

        public AvatarIdentification? AuthorAvatar { get; }
        public string AuthorEmail { get; }
        public User Author { get; }
        public DateTime CreatedTime { get; }
        public FinanceOperation? Finance { get; }
        public JoinHtmlString CommentText { get; }
        public int CommentId { get; }
        public IEnumerable<CommentViewModel> ChildComments { get; }
        public int ProjectId { get; }
        public int CommentDiscussionId { get; }
        public CommentExtraAction? ExtraAction { get; set; }

        public bool ShowFinanceModeration => Finance != null && Finance.RequireModeration && CanModerateFinance;

        public bool IsVisible { get; }
        public int DeepLevel { get; }
    }
}

