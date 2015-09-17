using System;
using System.Data.Entity.Validation;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
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
      var field = ProjectRepository.GetCharacter(projectid, characterid);
      return WithEntity(field) ?? View("Add", AddClaimViewModel.Create(field, GetCurrentUser()));
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
      var project1 = ProjectRepository.GetProject(viewModel.ProjectId);
      return WithProject(project1) ?? ((Func<Project, ActionResult>) (project =>
      {
        try
        {
          _claimService.AddClaimFromUser(viewModel.ProjectId, viewModel.CharacterGroupId, viewModel.CharacterId,
            CurrentUserId, viewModel.ClaimText.Contents);

          return RedirectToAction("My", "Claim");
        }
        catch
        {
          //TODO: Отображать ошибки верно
          return View(viewModel);
        }
      }))(project1);
    }

    [HttpGet, Authorize]
    public ActionResult My() => View(GetCurrentUser().Claims.Select(ClaimListItemViewModel.FromClaim));

    [HttpGet, Authorize]
    public ActionResult ForPlayer(int projectId, int userId)
      => MasterList(projectId, cl => cl.IsActive & cl.PlayerUserId == userId);

    private ActionResult MasterList(int projectId, Func<Claim, bool> predicate)
    {
      //TODO: Eager load claims
      var project = ProjectRepository.GetProject(projectId);
      return AsMaster(project) ?? View(project.Claims.Where(predicate).Select(ClaimListItemViewModel.FromClaim));
    }

    [HttpGet, Authorize]
    public ActionResult Discussing(int projectid) => MasterList(projectid, claim => claim.IsInDiscussion);

    [HttpGet, Authorize]
    public ActionResult Edit(int projectId, int claimId)
    {
      var claim = ProjectRepository.GetClaim(projectId, claimId);
      var error = WithClaim(claim);
      if (error != null)
      {
        return error;
      }
      var hasMasterAccess = claim.Project.HasAccess(CurrentUserId);
      var isMyClaim = claim.PlayerUserId == CurrentUserId;
      return View(new ClaimViewModel()
      {
        ClaimId = claim.ClaimId,
        ClaimName = claim.Name,
        Comments = claim.Comments.Where(comment => comment.ParentCommentId == null),
        HasMasterAccess = hasMasterAccess,
        HasPlayerAccessToCharacter = hasMasterAccess || (isMyClaim && claim.IsApproved),
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
        OtherClaimsFromThisPlayerCount = claim.IsApproved ? 0 : claim.OtherClaimsForThisPlayer().Count(),
        Description = claim.Character?.Description
      });

    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(int projectId, int claimId, string characterName, MarkdownString description, FormCollection formCollection)
    {
      var claim = ProjectRepository.GetClaim(projectId, claimId);
      var error = WithClaim(claim);
      if (error != null)
      {
        return error;
      }
      if ((!claim.IsApproved && !claim.Project.HasAccess(CurrentUserId)) || claim.CharacterId == null)
      {
        return Edit(projectId, claimId);
      }
      try
      {
        await ProjectService.SaveCharacterFields(projectId, (int) claim.CharacterId, CurrentUserId, characterName, description.Contents,
          GetCharacterFieldValuesFromPost(formCollection.ToDictionary()));
        return RedirectToAction("Edit", "Claim", new {projectId, claimId});
      }
      catch
      {
        return Edit(projectId, claimId);
      }
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public ActionResult ApproveByMaster(AddCommentViewModel viewModel)
    {
      var claim = ProjectRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId);
      var error = AsMaster(claim);
      if (error != null)
      {
        return error;
      }

      try
      {
        if (viewModel.HideFromUser)
        {
          throw new DbEntityValidationException();
        }
        _claimService.AppoveByMaster(claim.ProjectId, claim.ClaimId, CurrentUserId, viewModel.CommentText.Contents);

        return RedirectToAction("Edit", "Claim", new {viewModel.ClaimId, viewModel.ProjectId});
      }
      catch
      {
        //TODO: Message that comment is not added
        return RedirectToAction("Edit", "Claim", new {viewModel.ClaimId, viewModel.ProjectId});
      }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public ActionResult DeclineByMaster(AddCommentViewModel viewModel)
    {
      var claim = ProjectRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId);
      var error = AsMaster(claim);
      if (error != null)
      {
        return error;
      }

      try
      {
        if (viewModel.HideFromUser)
        {
          throw new DbEntityValidationException();
        }
        _claimService.DeclineByMaster(claim.ProjectId, claim.ClaimId, CurrentUserId, viewModel.CommentText.Contents);

        return RedirectToAction("Edit", "Claim", new {viewModel.ClaimId, viewModel.ProjectId});
      }
      catch
      {
        //TODO: Message that comment is not added
        return RedirectToAction("Edit", "Claim", new {viewModel.ClaimId, viewModel.ProjectId});
      }

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
          _claimService.DeclineByPlayer(project.ProjectId, claim.ClaimId, CurrentUserId, viewModel.CommentText.Contents);

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