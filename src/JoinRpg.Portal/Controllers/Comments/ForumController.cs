using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Comments;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[Authorize]
[Route("{projectId}/forums/{forumThreadId}/[action]")]
public class ForumController(
    IProjectRepository projectRepository,
    IProjectMetadataRepository projectMetadataRepository,
    IForumService forumService,
    IForumRepository forumRepository,
    IClaimsRepository claimsRepository,
    ICurrentUserAccessor currentUserAccessor,
    IClaimService claimService) : JoinControllerGameBase
{
    [HttpGet("~/{projectId}/roles/{charactergroupid}/create-thread")]
    [ProjectShouldBeActive]
    [MasterAuthorize]
    public async Task<ActionResult> CreateThread(ProjectIdentification projectId, int charactergroupid)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        var group = projectInfo.GetGroupById(charactergroupid);
        if (group is null)
        {
            return NotFound();
        }
        return View(new CreateForumThreadViewModel(projectInfo, group));
    }

    [HttpPost("~/{projectId}/roles/{charactergroupid}/create-thread")]
    [ProjectShouldBeActive]
    [MasterAuthorize, ValidateAntiForgeryToken]
    public async Task<ActionResult> CreateThread(CreateForumThreadViewModel viewModel)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new ProjectIdentification(viewModel.ProjectId));
        var group = projectInfo.GetGroupById(viewModel.CharacterGroupId);

        viewModel.CharacterGroupName = group.Name;
        viewModel.ProjectName = projectInfo.ProjectName.Value;

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }
        try
        {
            var forumThreadId = await forumService.CreateThread(
              new(viewModel.ProjectId, viewModel.CharacterGroupId),
              viewModel.Header,
              viewModel.CommentText,
              viewModel.HideFromUser,
              viewModel.EmailEverybody);
            return RedirectToAction("ViewThread", new { viewModel.ProjectId, forumThreadId });
        }
        catch (Exception exception)
        {
            AddModelException(exception);
            return View(viewModel);
        }
    }

    [HttpGet]
    public async Task<ActionResult> ViewThread(ProjectIdentification projectid, int forumThreadId)
    {
        var forumThread = await GetForumThread(projectid, forumThreadId);
        if (forumThread == null)
        {
            return NotFound();
        }

        var viewModel = new ForumThreadViewModel(forumThread, currentUserAccessor.UserIdentification);
        return View(viewModel);
    }

    private async Task<ForumThread> GetForumThread(ProjectIdentification projectid, int forumThreadId)
    {
        var forumThread = await forumRepository.GetThread(new(projectid, forumThreadId));
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectid);
        var isMaster = projectInfo.HasMasterAccess(currentUserAccessor);
        var isPlayer = forumThread.IsVisibleToPlayer &&
                       (await claimsRepository.GetClaimsForPlayer(projectid, currentUserAccessor.UserIdentification, ClaimStatusSpec.Approved)).Any(
                         claim => claim.Character.IsPartOfGroup(forumThread.CharacterGroupId));

        if (!isMaster && !isPlayer)
        {
            throw new NoAccessToProjectException(projectInfo, currentUserAccessor.UserIdentification);
        }
        return forumThread;
    }

    [HttpPost("~/{projectId}/forums/createcomment")]
    public async Task<ActionResult> CreateComment(AddCommentViewModel viewModel)
    {
        CommentDiscussion discussion = await forumRepository.GetDiscussion(viewModel.ProjectId, viewModel.CommentDiscussionId);
        discussion.RequestAnyAccess(currentUserAccessor.UserIdentification);

        if (discussion == null)
        {
            return NotFound();
        }

        try

        {
            if (viewModel.HideFromUser)
            {
                _ = discussion.RequestMasterAccess(currentUserAccessor.UserIdentification);
            }

            var claim = discussion.GetClaim();
            if (claim != null)
            {

                await claimService.AddComment(
                    claim.GetId(),
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
                    await forumService.AddComment(forumThread.GetId(), viewModel.ParentCommentId,
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
    public async Task<ActionResult> ListThreads(ProjectIdentification projectid)
    {
        var project = await projectMetadataRepository.GetProjectMetadata(projectid);
        var isMaster = project.HasMasterAccess(currentUserAccessor);
        IEnumerable<int>? groupIds;
        if (isMaster)
        {
            groupIds = null;
        }
        else
        {
            var claims = await claimsRepository.GetClaimsForPlayer(projectid, currentUserAccessor.UserIdentification, ClaimStatusSpec.Approved);

            groupIds = claims.SelectMany(claim => claim.Character.GetParentGroupIdsToTop(project).Select(g => g.CharacterGroupId));
        }
        var threads = await forumRepository.GetThreads(projectid.Value, isMaster, groupIds);
        var viewModel = new ForumThreadListViewModel(project, threads, currentUserAccessor.UserIdentification);
        return View(viewModel);
    }

    [HttpGet("~/{projectId}/roles/{characterGroupId}/forums")]
    public async Task<ActionResult> ListThreadsByGroup(ProjectIdentification projectid, int characterGroupId)
    {
        var group = await projectRepository.GetGroupAsync(new CharacterGroupIdentification(projectid, characterGroupId));
        if (group == null)
        {
            return NotFound();
        }
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectid);
        var isMaster = projectInfo.HasMasterAccess(currentUserAccessor);
        var threads = await forumRepository.GetThreads(projectid.Value, isMaster, new[] { characterGroupId });
        var viewModel = new ForumThreadListForGroupViewModel(projectInfo, group, threads.Where(t => t.HasAnyAccess(currentUserAccessor.UserIdentification)), currentUserAccessor.UserIdentification);
        return View(viewModel);
    }

    [HttpPost("~/{projectId}/forums/concealcomment")]
    public async Task<ActionResult> ConcealComment(ProjectIdentification projectid, int commentid, int commentDiscussionId)
    {
        await claimService.ConcealComment(projectid, commentid, commentDiscussionId);
        var discussion =
               await forumRepository.GetDiscussion(projectid, commentDiscussionId);
        return CommentRedirectHelper.RedirectToDiscussion(Url, discussion);
    }
}
