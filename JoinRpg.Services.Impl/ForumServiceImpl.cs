using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
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

    public async Task<int> CreateThread(int projectId, int characterGroupId, string header, string commentText, bool hideFromUser)
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
        Discussion = new CommentDiscussion()
      };
      
      forumThread.Discussion.Comments.Add(new Comment()
      {
        ProjectId = projectId,
        AuthorUserId = CurrentUserId,
        IsVisibleToPlayer = !hideFromUser,
        CommentText = new CommentText()
        {
          Text =  new MarkdownString(commentText)
        },
        CreatedAt = utcNow,
      });

      group.ForumThreads.Add(forumThread);
      await UnitOfWork.SaveChangesAsync();
      return forumThread.ForumThreadId;
    }

    private async Task<ForumThread> GetForumThread(int projectid, int forumThreadId)
    {
      var forumThread = await ForumRepository.GetThread(projectid, forumThreadId);
      var isMaster = forumThread.HasMasterAccess(CurrentUserId);
      var isPlayer = forumThread.IsVisibleToPlayer &&
                     (await ClaimsRepository.GetMyClaimsForProject(CurrentUserId, projectid)).Any(
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

      var parentComment = forumThread.Discussion.Comments.SingleOrDefault(c => c.CommentId == parentCommentId);

      var email = await AddCommentWithEmail<AddCommentEmail>(commentText, forumThread, isVisibleToPlayer, parentComment);

      await UnitOfWork.SaveChangesAsync();

      await EmailService.Email(email);

    }

    private async Task<ForumEmail> AddCommentWithEmail<T>(string commentText, ForumThread forumThread,
      bool isVisibleToPlayer, Comment parentComment, IEnumerable<User> extraSubscriptions = null) where T : ClaimEmailModel, new()
    {
      var visibleToPlayerUpdated = isVisibleToPlayer && parentComment?.IsVisibleToPlayer != false;
      if (!isVisibleToPlayer)
      {
        forumThread.RequestMasterAccess(CurrentUserId);
      }

      var comment = new Comment
      {
        ProjectId = forumThread.ProjectId,
        AuthorUserId = CurrentUserId,
        CommentText = new CommentText() { Text = new MarkdownString(commentText) },
        IsCommentByPlayer = !forumThread.HasMasterAccess(CurrentUserId),
        IsVisibleToPlayer = isVisibleToPlayer,
        Parent = parentComment,
      };
      forumThread.Discussion.Comments.Add(comment);

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
