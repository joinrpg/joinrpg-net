using System;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
  public class ClaimController : ControllerGameBase
  {
    private readonly IClaimService _claimService;

    [HttpGet]
    [Authorize]
    public ActionResult AddForCharacter(int projectid, int characterid)
    {
      return WithCharacter(projectid, characterid, (project, character) => View("Add", AddClaimViewModel.Create(character, GetCurrentUser())));
    }

    [HttpGet]
    [Authorize]
    public ActionResult AddForGroup(int projectid, int characterGroupId)
    {
      return WithGroup(projectid, characterGroupId, (project, group) => View("Add", AddClaimViewModel.Create(group, GetCurrentUser())));
    }

    public ClaimController(ApplicationUserManager userManager, IProjectRepository projectRepository, IProjectService projectService, IClaimService claimService) : base(userManager, projectRepository, projectService)
    {
      _claimService = claimService;
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public ActionResult Add(AddClaimViewModel viewModel)
    {
      return WithProject(viewModel.ProjectId, project =>
      {
        try
        {
          _claimService.AddClaimFromUser(viewModel.ProjectId, viewModel.CharacterGroupId, viewModel.CharacterId,
            CurrentUserId, viewModel.ClaimText);

          return RedirectToAction("My", "Claim");
        }
        catch
        {
          //TODO: Отображать ошибки верно
          return View(viewModel);
        }
      });
    }

    [HttpGet, Authorize]
    public ActionResult My() => View(GetCurrentUser().Claims);

    [HttpGet, Authorize]
    public ActionResult ForPlayer(int projectId, int userId)
      => MasterList(projectId, cl => cl.IsActive & cl.PlayerUserId == userId);

    private ActionResult MasterList(int projectId, Func<Claim, bool> predicate)
    {
      return WithProjectAsMaster(projectId, project => View(project.Claims.Where(predicate)));
    }

    [HttpGet, Authorize]
    public ActionResult Discussing(int projectid) => MasterList(projectid, claim => claim.IsInDiscussion);

    [HttpGet, Authorize]
    public ActionResult Edit(int projectId, int claimId)
    {
      return WithClaim(projectId, claimId, (project, claim, hasMasterAccess, isMyClaim) => View(new ClaimViewModel()
      {
        ClaimId = claim.ClaimId,
        ClaimName = claim.Name,
        Comments = claim.Comments.Where(comment => comment.ParentCommentId == null),
        HasMasterAccess = hasMasterAccess,
        HasPlayerAccessToCharacter = hasMasterAccess || (claim.PlayerUserId == CurrentUserId && claim.IsApproved),
        CharacterFields = claim.Character?.Fields().Select(pair => pair.Value) ?? new CharacterFieldValue[] {},
        IsMyClaim = isMyClaim,
        Player = claim.Player,
        ProjectId = claim.ProjectId,
        ProjectName = claim.Project.ProjectName,
        Status = claim.ClaimStatus,
        CharacterGroupId = claim.CharacterGroupId,
        GroupName = claim.Group?.CharacterGroupName,
        CharacterId = claim.CharacterId,
        CharacterName = claim.Character?.CharacterName,
        OtherClaimsForThisCharacterCount = claim.IsApproved ? 0 : claim.OtherClaimsForThisCharacter().Count(),
        OtherClaimsFromThisPlayerCount = claim.IsApproved ? 0 : claim.OtherClaimsForThisPlayer().Count()
      }));
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public ActionResult ApproveByMaster(AddCommentViewModel viewModel)
    {
      return WithClaimAsMaster(viewModel.ProjectId, viewModel.ClaimId, (project, claim) =>
      {
        try
        {
          if (viewModel.HideFromUser)
          {
            throw new DbEntityValidationException();
          }
          _claimService.AppoveByMaster(project.ProjectId, claim.ClaimId, CurrentUserId, viewModel.CommentText);

          return RedirectToAction("Edit", "Claim", new { viewModel.ClaimId, viewModel.ProjectId });
        }
        catch
        {
          //TODO: Message that comment is not added
          return RedirectToAction("Edit", "Claim", new { viewModel.ClaimId, viewModel.ProjectId });
        }
      });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public ActionResult DeclineByMaster(AddCommentViewModel viewModel)
    {
      return WithClaimAsMaster(viewModel.ProjectId, viewModel.ClaimId, (project, claim) =>
      {
        try
        {
          if (viewModel.HideFromUser)
          {
            throw new DbEntityValidationException();
          }
          _claimService.DeclineByMaster(project.ProjectId, claim.ClaimId, CurrentUserId, viewModel.CommentText);

          return RedirectToAction("Edit", "Claim", new { viewModel.ClaimId, viewModel.ProjectId });
        }
        catch
        {
          //TODO: Message that comment is not added
          return RedirectToAction("Edit", "Claim", new { viewModel.ClaimId, viewModel.ProjectId });
        }
      });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public ActionResult DeclineByPlayer(AddCommentViewModel viewModel)
    {
      return WithMyClaim(viewModel.ProjectId, viewModel.ClaimId, (project, claim) =>
      {
        try
        {
          if (viewModel.HideFromUser)
          {
            throw new DbEntityValidationException();
          }
          _claimService.DeclineByPlayer(project.ProjectId, claim.ClaimId, CurrentUserId, viewModel.CommentText);

          return RedirectToAction("Edit", "Claim", new { viewModel.ClaimId, viewModel.ProjectId });
        }
        catch
        {
          //TODO: Message that comment is not added
          return RedirectToAction("Edit", "Claim", new { viewModel.ClaimId, viewModel.ProjectId });
        }
      });
    }
  }
}