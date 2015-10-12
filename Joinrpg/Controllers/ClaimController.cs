using System;
using System.Data.Entity.Validation;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Plot;

namespace JoinRpg.Web.Controllers
{
  public class ClaimController : ControllerGameBase
  {
    private readonly IClaimService _claimService;
    private readonly IPlotRepository _plotRepository;
    private readonly IClaimsRepository _claimsRepository;

    [HttpGet]
    [Authorize]
    public async Task<ActionResult> AddForCharacter(int projectid, int characterid)
    {
      var field = await ProjectRepository.GetCharacterAsync(projectid, characterid);
      return WithEntity(field) ?? View("Add", AddClaimViewModel.Create(field, GetCurrentUser()));
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult> AddForGroup(int projectid, int characterGroupId)
    {
      var field = await ProjectRepository.LoadGroupAsync(projectid, characterGroupId);
      return WithProject(field.Project) ?? View("Add", AddClaimViewModel.Create(field, GetCurrentUser()));
    }

    public ClaimController(ApplicationUserManager userManager, IProjectRepository projectRepository,
      IProjectService projectService, IClaimService claimService, IPlotRepository plotRepository,
      IClaimsRepository claimsRepository) : base(userManager, projectRepository, projectService)
    {
      _claimService = claimService;
      _plotRepository = plotRepository;
      _claimsRepository = claimsRepository;
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Add(AddClaimViewModel viewModel)
    {
      var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
      var error = WithProject(project);
      if (error != null)
      {
        return error;
      }

      try
      {
        await _claimService.AddClaimFromUser(viewModel.ProjectId, viewModel.CharacterGroupId, viewModel.CharacterId,
          CurrentUserId, viewModel.ClaimText.Contents);

        return RedirectToAction("My", "Claim");
      }
      catch
      {
        //TODO: Отображать ошибки верно
        return View(viewModel);
      }
    }

    [HttpGet, Authorize]
    public ActionResult My() => View(GetCurrentUser().Claims.Select(ClaimListItemViewModel.FromClaim));

    [HttpGet, Authorize]
    public Task<ActionResult> ForPlayer(int projectId, int userId)
      => MasterList(projectId, cl => cl.IsActive & cl.PlayerUserId == userId, "ForPlayer");

    [HttpGet, Authorize]
    public Task<ActionResult> ListForGroupDirect(int projectId, int characterGroupId)
    {
      ViewBag.CharacterGroupId = characterGroupId;
      ViewBag.ProjectId = projectId;
      return MasterList(projectId, cl => cl.CharacterGroupId == characterGroupId, "ListForGroupDirect");
    }

    public Task<ActionResult> ListForGroup(int projectId, int characterGroupId)
    {
      ViewBag.CharacterGroupId = characterGroupId;
      ViewBag.ProjectId = projectId;
      return MasterList(projectId, cl => cl.PartOfGroup(characterGroupId), "ListForGroup");
    }

    private async Task<ActionResult> MasterList(int projectId, Func<Claim, bool> predicate, [AspMvcView] string viewName)
    {
      var project = await _claimsRepository.GetClaims(projectId);
      return AsMaster(project) ??
             View(viewName, project.Claims.Where(predicate).Select(ClaimListItemViewModel.FromClaim));
    }

    [HttpGet, Authorize]
    public Task<ActionResult> Discussing(int projectid)
      => MasterList(projectid, claim => claim.IsInDiscussion, "Discussing");

    [HttpGet, Authorize]
    public Task<ActionResult> Responsible(int projectid, int responsibleMasterId)
      =>
        MasterList(projectid, claim => claim.ResponsibleMasterUserId == responsibleMasterId && claim.IsActive,
          "Responsible");

    [HttpGet, Authorize]
    public async Task<ActionResult> Problems(int projectId)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      return AsMaster(project) ??
             View(
               (await _claimService.GetProblemClaims(projectId)).Select(ClaimProblemListItemViewModel.FromClaimProblem));
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> Edit(int projectId, int claimId)
    {
      var claim = await ProjectRepository.GetClaim(projectId, claimId);
      var error = WithClaim(claim);
      if (error != null)
      {
        return error;
      }
      var hasMasterAccess = claim.Project.HasAccess(CurrentUserId);
      var isMyClaim = claim.PlayerUserId == CurrentUserId;
      var claimViewModel = new ClaimViewModel()
      {
        ClaimId = claim.ClaimId,
        ClaimName = claim.Name,
        Comments = claim.Comments.Where(comment => comment.ParentCommentId == null),
        HasMasterAccess = hasMasterAccess,
        HasPlayerAccessToCharacter = hasMasterAccess || (isMyClaim && claim.IsApproved),
        CharacterFields = claim.Character?.Fields().Select(pair => pair.Value) ?? new CharacterFieldValue[] {},
        HasApproveRejectClaim = claim.Project.HasSpecificAccess(CurrentUserId, acl => acl.CanApproveClaims),
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
        Description = claim.Character?.Description,
        Masters =
          MasterListItemViewModel.FromProject(claim.Project)
            .Union(new MasterListItemViewModel() {Id = "-1", Name = "Нет"}),
        ResponsibleMasterId = claim.ResponsibleMasterUserId ?? -1
      };

      if ((hasMasterAccess || isMyClaim) && claim.IsApproved)
      {
        claimViewModel.Plot =
          (await _plotRepository.GetPlotsForCharacter(claim.Character)).Select(
            p => PlotElementViewModel.FromPlotElement(p, hasMasterAccess));
      }
      return View(claimViewModel);
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(int projectId, int claimId, string characterName, MarkdownString description,
      FormCollection formCollection)
    {
      var claim = await ProjectRepository.GetClaim(projectId, claimId);
      var error = WithClaim(claim);
      if (error != null)
      {
        return error;
      }
      if ((!claim.IsApproved && !claim.Project.HasAccess(CurrentUserId)) || claim.CharacterId == null)
      {
        return await Edit(projectId, claimId);
      }
      try
      {
        await
          ProjectService.SaveCharacterFields(projectId, (int) claim.CharacterId, CurrentUserId, characterName,
            description.Contents,
            GetCharacterFieldValuesFromPost(formCollection.ToDictionary()));
        return RedirectToAction("Edit", "Claim", new {projectId, claimId});
      }
      catch
      {
        return await Edit(projectId, claimId);
      }
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<ActionResult> ApproveByMaster(AddCommentViewModel viewModel)
    {
      var claim = await ProjectRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId);
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
        await
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
    public async Task<ActionResult> DeclineByMaster(AddCommentViewModel viewModel)
    {
      var claim = await ProjectRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId);
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
        await
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
    public async Task<ActionResult> DeclineByPlayer(AddCommentViewModel viewModel)
    {
      var claim = await ProjectRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId);
      var error = WithMyClaim(claim);
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
        await
          _claimService.DeclineByPlayer(claim.ProjectId, claim.ClaimId, CurrentUserId, viewModel.CommentText.Contents);

        return RedirectToAction("Edit", "Claim", new {viewModel.ClaimId, viewModel.ProjectId});
      }
      catch
      {
        //TODO: Message that comment is not added
        return RedirectToAction("Edit", "Claim", new {viewModel.ClaimId, viewModel.ProjectId});
      }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> ChangeResponsible(int projectId, int claimId, int responsibleMasterId)
    {
      var claim = await ProjectRepository.GetClaim(projectId, claimId);
      var error = AsMaster(claim);
      if (error != null)
      {
        return error;
      }
      try
      {
        await _claimService.SetResponsible(projectId, claimId, CurrentUserId, responsibleMasterId);
      }
      catch
      {
        //TODO: Message 
        return RedirectToAction("Edit", "Claim", new {claimId, projectId});
      }
      return RedirectToAction("Edit", "Claim", new {claimId, projectId});
    }
  }
}