using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
  public abstract class ClaimImplBase : DbServiceImplBase
  {
    protected IEmailService EmailService { get; }
    protected IFieldDefaultValueGenerator FieldDefaultValueGenerator { get; }

    protected ClaimImplBase(IUnitOfWork unitOfWork, IEmailService emailService,
      IFieldDefaultValueGenerator fieldDefaultValueGenerator) : base(unitOfWork)
    {
      EmailService = emailService;
      FieldDefaultValueGenerator = fieldDefaultValueGenerator;
    }

    protected Comment AddCommentImpl(Claim claim, Comment parentComment, string commentText,
      bool isVisibleToPlayer, CommentExtraAction? extraAction)
    {
      if (!isVisibleToPlayer)
      {
        claim.RequestMasterAccess(CurrentUserId);
      }

      var comment = new Comment
      {
        CommentId = -1,
        ProjectId = claim.ProjectId,
        AuthorUserId = CurrentUserId,
        CommentDiscussionId = claim.CommentDiscussion.CommentDiscussionId,
        CommentText = new CommentText()
        {
          CommentId = -1,
          Text = new MarkdownString(commentText)
        },
        IsCommentByPlayer = claim.PlayerUserId == CurrentUserId,
        IsVisibleToPlayer = isVisibleToPlayer,
        Parent = parentComment,
        ExtraAction = extraAction,
        CreatedAt = Now,
        LastEditTime = Now
      };
      claim.CommentDiscussion.Comments.Add(comment);

      claim.LastUpdateDateTime = Now;

      return comment;
    }
  }
}
