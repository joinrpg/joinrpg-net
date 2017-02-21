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
          viewModel.HideFromUser,
          viewModel.EmailEverybody);
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
                     (await ClaimsRepository.GetClaimsForPlayer(projectid, ClaimStatusSpec.Approved, CurrentUserId)).Any(
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

        var claim = discussion.GetClaim();
        if (claim != null)
        {

          await ClaimService.AddComment(discussion.ProjectId, claim.ClaimId, CurrentUserId, viewModel.ParentCommentId,
            !viewModel.HideFromUser, viewModel.CommentText, viewModel.FinanceAction);
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
      var claim = discussion.GetClaim();
      if (claim != null)
      {
        var actionLink = Url.Action("Edit", "Claim", new {claim.ClaimId, discussion.ProjectId});
        return Redirect(actionLink + extra);
      }
      var forumThread = discussion.GetForumThread();
      if (forumThread != null)
      {
        var actionLink = Url.Action("ViewThread", new { discussion.ProjectId, forumThread.ForumThreadId});
        return Redirect(actionLink + extra);
      }
      return HttpNotFound();
    }

    public async Task<ActionResult> RedirectToDiscussion(int projectid, int? commentid, int? commentDiscussionId)
    {
      CommentDiscussion discussion;
      if (commentid != null)
      {
        discussion = await ForumRepository.GetDiscussionByComment(projectid, (int) commentid);
      }
      else if (commentDiscussionId != null)
      {
        discussion = await ForumRepository.GetDiscussion(projectid, (int) commentDiscussionId);
      }
      else
      {
        return HttpNotFound();
      }
      discussion.RequestAnyAccess(CurrentUserId);
      return ReturnToParent(discussion, commentid!= null ?  $"comment{commentid}" : null);
    }

    [HttpGet]
    public async Task<ActionResult> ListThreads(int projectid)
    {
      var project = await ProjectRepository.GetProjectAsync(projectid);
      var isMaster = project.HasMasterAccess(CurrentUserIdOrDefault);
      var claims = await ClaimsRepository.GetClaimsForPlayer(projectid, ClaimStatusSpec.Approved, CurrentUserId);
      var groupIds = claims.SelectMany(claim => claim.Character.GetGroupsPartOf());
      var threads = await ForumRepository.GetThreads(projectid, isMaster, groupIds);
      var viewModel = new ForumThreadListViewModel(project, threads, CurrentUserId);
      return View(viewModel);
    }
  }
}