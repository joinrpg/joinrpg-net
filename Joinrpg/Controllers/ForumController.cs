using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Filter;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
  public class ForumController : ControllerGameBase
  {
    #region Constructor & Services
    private IForumService ForumService { get; }
    private IForumRepository ForumRepository { get; }
    private IClaimsRepository ClaimsRepository { get; }

    private IClaimService ClaimService { get; }

    public ForumController(ApplicationUserManager userManager, IProjectRepository projectRepository,
      IProjectService projectService, IExportDataService exportDataService, IForumService forumService, IForumRepository forumRepository, IClaimsRepository claimsRepository, IClaimService claimService)
      : base(userManager, projectRepository, projectService, exportDataService)
    {
      ForumService = forumService;
      ForumRepository = forumRepository;
      ClaimsRepository = claimsRepository;
      ClaimService = claimService;
    }
    #endregion

    [MasterAuthorize, HttpGet]
    public async Task<ActionResult> CreateThread(int projectId, int charactergroupid)
    {
      return View(new CreateForumThreadViewModel((await ProjectRepository.GetGroupAsync(projectId, charactergroupid)).EnsureActive()));
    }

    [MasterAuthorize, HttpPost, ValidateAntiForgeryToken]
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
          viewModel.HideFromUser);
        return RedirectToAction("ViewThread", new {viewModel.ProjectId, forumThreadId });
      }
      catch (Exception exception)
      {
        ModelState.AddException(exception);
        return View(viewModel);
      }
    }

    [Authorize]
    public async Task<ActionResult> ViewThread(int projectid, int forumThreadId)
    {
      var forumThread = await GetForumThread(projectid, forumThreadId);
      var viewModel = new ForumThreadViewModel(forumThread, CurrentUserId);
      return View(viewModel);
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> CreateComment(AddCommentViewModel viewModel)
    {
      CommentDiscussion discussion = await ForumRepository.GetDiscussion(viewModel.ProjectId, viewModel.CommentDiscussionId);
      discussion.RequestAnyAccess(CurrentUserId);

      var error = WithEntity(discussion);

      if (error != null) return error;

      try

      {
        if (viewModel.HideFromUser)
        {
          discussion.RequestMasterAccess(CurrentUserId);
        }

        if (discussion.Claim != null)
        {

          await ClaimService.AddComment(discussion.ProjectId, discussion.Claim.ClaimId, CurrentUserId, viewModel.ParentCommentId,
            !viewModel.HideFromUser, viewModel.CommentText, viewModel.FinanceAction);
        }
        else if (discussion.ForumThread != null)
        {
          await ForumService.AddComment(discussion.ProjectId, discussion.ForumThread.ForumThreadId, viewModel.ParentCommentId,
            !viewModel.HideFromUser, viewModel.CommentText);
        }

        return ReturnToParent(discussion);
      }
      catch
      {
        //TODO: Message that comment is not added
        return ReturnToParent(discussion);
      }

    }

    private ActionResult ReturnToParent(CommentDiscussion discussion, string extra = null)
    {
      if (extra == null)
      {
        extra = "";
      }
      else
      {
        extra = "#" + extra;
      }
      if (discussion.Claim != null)
      {
        var actionLink = Url.Action("Edit", "Claim", new {discussion.Claim.ClaimId, discussion.ProjectId});
        return Redirect(actionLink + extra);
      }
      if (discussion.ForumThread != null)
      {
        var actionLink = Url.Action("ViewThread", new { discussion.ProjectId, discussion.ForumThread.ForumThreadId});
        return Redirect(actionLink + extra);
      }
      return HttpNotFound();
    }

    public async Task<ActionResult> RedirectToDiscussion(int projectid, int commentid)
    {
      CommentDiscussion discussion = await ForumRepository.GetDiscussionByComment(projectid, commentid);
      discussion.RequestAnyAccess(CurrentUserId);
      return ReturnToParent(discussion, $"comment{commentid}");
    }
  }
}