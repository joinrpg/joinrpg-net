using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Portal.Controllers.Comments;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers
{
    [Authorize]
    [Route("{projectId}/forums/{forumThreadId}/[action]")]
    public class ForumController : ControllerGameBase
    {
        #region Constructor & Services
        private IForumService ForumService { get; }
        private IForumRepository ForumRepository { get; }
        private IClaimsRepository ClaimsRepository { get; }

        private IClaimService ClaimService { get; }

        public ForumController(
            IProjectRepository projectRepository,
            IProjectService projectService,
            IForumService forumService,
            IForumRepository forumRepository,
            IClaimsRepository claimsRepository,
            IClaimService claimService,
            IUserRepository userRepository
            )
          : base(projectRepository, projectService, userRepository)
        {
            ForumService = forumService;
            ForumRepository = forumRepository;
            ClaimsRepository = claimsRepository;
            ClaimService = claimService;
        }
        #endregion

        [HttpGet("~/{projectId}/roles/{charactergroupid}/create-thread")]
        [MasterAuthorize]
        public async Task<ActionResult> CreateThread(int projectId, int charactergroupid)
        {
            var characterGroup = await ProjectRepository.GetGroupAsync(projectId, charactergroupid);
            if (characterGroup == null)
            {
                return NotFound();
            }
            return View(new CreateForumThreadViewModel(characterGroup.EnsureActive()));
        }

        [HttpPost("~/{projectId}/roles/{charactergroupid}/create-thread")]
        [MasterAuthorize, ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateThread([NotNull] CreateForumThreadViewModel viewModel)
        {
            var group = (await ProjectRepository.GetGroupAsync(viewModel.ProjectId, viewModel.CharacterGroupId)).EnsureActive();

            viewModel.CharacterGroupName = group.CharacterGroupName;
            viewModel.ProjectName = group.Project.ProjectName;

            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }
            try
            {
                var forumThreadId = await ForumService.CreateThread(
                  viewModel.ProjectId,
                  viewModel.CharacterGroupId,
                  viewModel.Header,
                  viewModel.CommentText,
                  viewModel.HideFromUser,
                  viewModel.EmailEverybody);
                return RedirectToAction("ViewThread", new { viewModel.ProjectId, forumThreadId });
            }
            catch (Exception exception)
            {
                ModelState.AddException(exception);
                return View(viewModel);
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult> ViewThread(int projectid, int forumThreadId)
        {
            var forumThread = await GetForumThread(projectid, forumThreadId);
            if (forumThread == null)
            {
                return NotFound();
            }

            var viewModel = new ForumThreadViewModel(forumThread, CurrentUserId);
            return View(viewModel);
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

        [HttpPost("~/{projectId}/forums/createcomment")]
        public async Task<ActionResult> CreateComment(AddCommentViewModel viewModel)
        {
            CommentDiscussion discussion = await ForumRepository.GetDiscussion(viewModel.ProjectId, viewModel.CommentDiscussionId);
            discussion.RequestAnyAccess(CurrentUserId);

            if (discussion == null)
            {
                return NotFound();
            }

            try

            {
                if (viewModel.HideFromUser)
                {
                    discussion.RequestMasterAccess(CurrentUserId);
                }

                var claim = discussion.GetClaim();
                if (claim != null)
                {

                    await ClaimService.AddComment(discussion.ProjectId,
                        claim.ClaimId,
                        viewModel.ParentCommentId,
                        !viewModel.HideFromUser,
                        viewModel.CommentText,
                        (FinanceOperationAction)viewModel.FinanceAction);
                }
                else
                {
                    var forumThread = discussion.GetForumThread();
                    if (forumThread != null)
                    {
                        await ForumService.AddComment(discussion.ProjectId, forumThread.ForumThreadId, viewModel.ParentCommentId,
                          !viewModel.HideFromUser, viewModel.CommentText);
                    }
                }

                return CommentRedirectHelper.RedirectToDiscussion(Url, discussion);
            }
            catch
            {
                //TODO: Message that comment is not added
                return CommentRedirectHelper.RedirectToDiscussion(Url, discussion);
            }

        }

        [HttpGet("~/{projectId}/forums")]
        public async Task<ActionResult> ListThreads(int projectid)
        {
            var project = await ProjectRepository.GetProjectAsync(projectid);
            if (project == null)
            {
                return NotFound();
            }
            var isMaster = project.HasMasterAccess(CurrentUserIdOrDefault);
            IEnumerable<int>? groupIds;
            if (isMaster)
            {
                groupIds = null;
            }
            else
            {
                var claims = await ClaimsRepository.GetClaimsForPlayer(projectid, ClaimStatusSpec.Approved, CurrentUserId);

                groupIds = claims.SelectMany(claim => claim.Character.GetGroupsPartOf().Select(g => g.CharacterGroupId));
            }
            var threads = await ForumRepository.GetThreads(projectid, isMaster, groupIds);
            var viewModel = new ForumThreadListViewModel(project, threads, CurrentUserId);
            return View(viewModel);
        }

        [HttpGet("~/{projectId}/roles/{characterGroupId}/forums")]
        public async Task<ActionResult> ListThreadsByGroup(int projectid, int characterGroupId)
        {
            var group = await ProjectRepository.GetGroupAsync(projectid, characterGroupId);
            if (group == null)
            {
                return NotFound();
            }
            var isMaster = group.HasMasterAccess(CurrentUserIdOrDefault);
            var threads = await ForumRepository.GetThreads(projectid, isMaster, new[] { characterGroupId });
            var viewModel = new ForumThreadListForGroupViewModel(group, threads.Where(t => t.HasAnyAccess(CurrentUserIdOrDefault)), CurrentUserId);
            return View(viewModel);
        }

        [HttpPost("~/{projectId}/forums/concealcomment")]
        public async Task<ActionResult> ConcealComment(int projectid, int commentid, int commentDiscussionId)
        {
            await ClaimService.ConcealComment(projectid, commentid, commentDiscussionId, CurrentUserId);
            var discussion =
                   await ForumRepository.GetDiscussion(projectid, commentDiscussionId);
            return CommentRedirectHelper.RedirectToDiscussion(Url, discussion);
        }
    }
}
