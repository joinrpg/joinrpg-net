using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl
{
    [UsedImplicitly(ImplicitUseTargetFlags.Itself)]
    internal class ForumServiceImpl : DbServiceImplBase, IForumService
    {
        private IEmailService EmailService { get; }
        public ForumServiceImpl(IUnitOfWork unitOfWork, IEmailService emailService, ICurrentUserAccessor currentUserAccessor) : base(unitOfWork, currentUserAccessor) => EmailService = emailService;

        private int[] GetChildrenGroupIds(CharacterGroup group) => group.GetChildrenGroups().Select(g => g.CharacterGroupId).Union(group.CharacterGroupId).ToArray();

        public async Task<int> CreateThread(int projectId, int characterGroupId, string header, string commentText, bool hideFromUser, bool emailEverybody)
        {
            var group = await LoadProjectSubEntityAsync<CharacterGroup>(projectId, characterGroupId);
            _ = group.RequestMasterAccess(CurrentUserId);
            var forumThread = new ForumThread()

            {
                CharacterGroupId = characterGroupId,
                ProjectId = projectId,
                Header = Required(header),
                CreatedAt = Now,
                ModifiedAt = Now,
                AuthorUserId = CurrentUserId,
                IsVisibleToPlayer = !hideFromUser,
                CommentDiscussion = new CommentDiscussion() { ProjectId = projectId, Project = group.Project, },
            };

            _ = CommentHelper.CreateCommentForDiscussion(forumThread.CommentDiscussion,
                CurrentUserId,
                Now,
                commentText,
                !hideFromUser,
                parentComment: null);


            group.ForumThreads.Add(forumThread);
            await UnitOfWork.SaveChangesAsync();

            if (emailEverybody)
            {
                var groups = GetChildrenGroupIds(group);
                var players = hideFromUser ? System.Array.Empty<User>() :
                  (await ClaimsRepository.GetClaimsForGroups(projectId, ClaimStatusSpec.Approved, groups)).Select(
                    claim => claim.Player);
                var masters = forumThread.Project.ProjectAcls.Select(acl => acl.User);

                var fe = new ForumEmail()
                {
                    ForumThread = forumThread,
                    ProjectName = forumThread.Project.ProjectName,
                    Initiator = await UserRepository.GetById(CurrentUserId),
                    Recipients = players.Union(masters).ToList(),
                    Text = new MarkdownString(commentText),
                };

                await EmailService.Email(fe);
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
          bool isVisibleToPlayer, Comment? parentComment)
        {
            var visibleToPlayerUpdated = isVisibleToPlayer && parentComment?.IsVisibleToPlayer != false;

            _ = CommentHelper.CreateCommentForDiscussion(forumThread.CommentDiscussion,
                CurrentUserId,
                Now,
                commentText,
                isVisibleToPlayer,
                parentComment);

            var extraRecipients =
              new[] { parentComment?.Author, parentComment?.Finance?.PaymentType?.User };
            var subscriptions =
              forumThread.GetSubscriptions(extraRecipients, visibleToPlayerUpdated).ToList();
            return new ForumEmail()
            {
                ForumThread = forumThread,
                ProjectName = forumThread.Project.ProjectName,
                Initiator = await UserRepository.GetById(CurrentUserId),
                Recipients = subscriptions.ToList(),
                Text = new MarkdownString(commentText),
            };
        }
    }
}
