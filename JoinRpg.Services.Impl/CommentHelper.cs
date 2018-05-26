using System;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Services.Impl
{
    internal static class CommentHelper
    {
        public static Comment CreateCommentForDiscussion([NotNull]
            CommentDiscussion commentDiscussion,
            int currentUserId,
            DateTime createdAt,
            [NotNull]
            string commentText,
            bool isVisibleToPlayer,
            [CanBeNull]
            Comment parentComment,
            CommentExtraAction? extraAction = null)
        {
            if (commentDiscussion == null) throw new ArgumentNullException(nameof(commentDiscussion));
            if (commentText == null) throw new ArgumentNullException(nameof(commentText));
            var comment = new Comment
            {
                CommentId = -1,
                ProjectId = commentDiscussion.ProjectId,
                AuthorUserId = currentUserId,
                CommentDiscussionId = commentDiscussion.CommentDiscussionId,
                CommentText = new CommentText()
                {
                    CommentId = -1,
                    Text = new MarkdownString(commentText),
                },
                IsCommentByPlayer = !commentDiscussion.HasMasterAccess(currentUserId),
                IsVisibleToPlayer = isVisibleToPlayer,
                Parent = parentComment,
                ExtraAction = extraAction,
                CreatedAt = createdAt,
                LastEditTime = createdAt,
            };
            commentDiscussion.Comments.Add(comment);
            if (!isVisibleToPlayer)
            {
                commentDiscussion.RequestMasterAccess(currentUserId);
            }
            //TODO: check access for discussion for players (claims & forums)
            return comment;
        }
    }
}
