using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
  [UsedImplicitly(ImplicitUseTargetFlags.Itself)]
  internal class ForumServiceImpl : DbServiceImplBase, IForumService
  {
    private IEmailService EmailService { get; }
    public ForumServiceImpl(IUnitOfWork unitOfWork, IEmailService emailService) : base(unitOfWork)
    {
      EmailService = emailService;
    }

    public async Task<int> CreateThread(int projectId, int characterGroupId, string header, string commentText, bool hideFromUser, bool emailEverybody)
    {
      var group = await LoadProjectSubEntityAsync<CharacterGroup>(projectId, characterGroupId);
      group.RequestMasterAccess(CurrentUserId);
      var utcNow = DateTime.UtcNow;
      var forumThread = new ForumThread()

      {
        CharacterGroupId = characterGroupId,
        ProjectId = projectId,
        Header = Required(header),
        CreatedAt = utcNow,
        ModifiedAt = utcNow,
        AuthorUserId = CurrentUserId,
        IsVisibleToPlayer = !hideFromUser,
        CommentDiscussion = new CommentDiscussion()
      };
      
      forumThread.CommentDiscussion.Comments.Add(new Comment()
      {
        CommentId = -1,
        ProjectId = projectId,
        AuthorUserId = CurrentUserId,
        IsVisibleToPlayer = !hideFromUser,
        CommentText = new CommentText()
        {
          CommentId = -1,
          Text =  new MarkdownString(commentText)
        },
        CreatedAt = utcNow,
      });

      group.ForumThreads.Add(forumThread);
      await UnitOfWork.SaveChangesAsync();

      if (emailEverybody)
      {
        //TODO implement this
      }
      return forumThread.ForumThreadId;
    }

    private async Task<ForumThread> GetForumThread(int projectid, int forumThreadId)
    {
      var forumThread = await ForumRepository.GetThread(projectid, forumThreadId);
      var isMaster = forumThread.HasMasterAccess(CurrentUserId);
      var isPlayer = forumThread.IsVisibleToPlayer &&
                     (await ClaimsRepository.GetClaimsForPlayer(projectid, ClaimStatusSpec.Approved, CurrentUserId)).Any(
                       claim => claim.IsPartOfGroup(forumThread.CharacterGroupId));

      if (!isMaster && !isPlayer)
      {
        throw new NoAccessToProjectException(forumThread, CurrentUserId);
      }
      return forumThread;
    }


    public async Task AddComment(int projectId, int forumThreadId, int? parentCommentId, bool isVisibleToPlayer, string commentText)
    {
      var forumThread = await GetForumThread(projectId, forumThreadId);

      var parentComment = forumThread.CommentDiscussion.Comments.SingleOrDefault(c => c.CommentId == parentCommentId);

      var email = await AddCommentWithEmail(commentText, forumThread, isVisibleToPlayer, parentComment);

      await UnitOfWork.SaveChangesAsync();

      await EmailService.Email(email);

    }

    private async Task<ForumEmail> AddCommentWithEmail(string commentText, ForumThread forumThread,
      bool isVisibleToPlayer, Comment parentComment, IEnumerable<User> extraSubscriptions = null)
    {
      var visibleToPlayerUpdated = isVisibleToPlayer && parentComment?.IsVisibleToPlayer != false;
      if (!isVisibleToPlayer)
      {
        forumThread.RequestMasterAccess(CurrentUserId);
      }

      var comment = new Comment
      {
        CommentId = -1,
        ProjectId = forumThread.ProjectId,
        AuthorUserId = CurrentUserId,
        CommentText = new CommentText()
        {
          Text = new MarkdownString(commentText),
          CommentId = -1
        },
        IsCommentByPlayer = !forumThread.HasMasterAccess(CurrentUserId),
        IsVisibleToPlayer = isVisibleToPlayer,
        Parent = parentComment,
      };
      forumThread.CommentDiscussion.Comments.Add(comment);

      var extraRecepients =
        new[] { parentComment?.Author, parentComment?.Finance?.PaymentType?.User }.
        Union(extraSubscriptions ?? Enumerable.Empty<User>());
      var subscriptions =
        forumThread.GetSubscriptions(CurrentUserId, extraRecepients, visibleToPlayerUpdated).ToList();
      return new ForumEmail ()
      {
        ForumThread = forumThread,
        ProjectName = forumThread.Project.ProjectName,
        Initiator = await UserRepository.GetById(CurrentUserId),
        Recepients = subscriptions.ToList(),
        Text = new MarkdownString(commentText),
      };
    }
  }
}
