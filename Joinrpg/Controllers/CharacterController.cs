using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Plot;

namespace JoinRpg.Web.Controllers
{
  public class CharacterController : Common.ControllerGameBase
  {
    private readonly IPlotRepository _plotRepository;

    public CharacterController(ApplicationUserManager userManager, IProjectRepository projectRepository,
      IProjectService projectService, IPlotRepository plotRepository) : base(userManager, projectRepository, projectService)
    {
      _plotRepository = plotRepository;
    }

    [HttpGet]
    public async Task<ActionResult> Details(int projectid, int characterid)
    {
      var field = await ProjectRepository.GetCharacterAsync(projectid, characterid);
      return WithEntity(field) ?? await ShowCharacter(field.Project, field);
    }

    private async Task<ActionResult> ShowCharacter(Project project, Character character)
    {
      var hasMasterAccess = project.HasMasterAccess(CurrentUserIdOrDefault);
      var approvedClaimUser = character.ApprovedClaim?.Player;
      var hasAnyAccess = hasMasterAccess || (User.Identity.IsAuthenticated && approvedClaimUser?.UserId == CurrentUserId);
      var viewModel = new CharacterDetailsViewModel
      {
        CharacterName = character.CharacterName,
        Description = character.Description,
        ApprovedClaimId = character.ApprovedClaim?.ClaimId,
        ApprovedClaimUser = approvedClaimUser,
        CanAddClaim = character.IsAvailable,
        DiscussedClaims = LoadIfMaster(project, () => character.Claims.Where(claim => claim.IsInDiscussion)).Select(ClaimListItemViewModel.FromClaim),
        RejectedClaims = LoadIfMaster(project, () => character.Claims.Where(claim => !claim.IsActive)).Select(ClaimListItemViewModel.FromClaim),
        CharacterFields = character.Fields().Select(pair => pair.Value),
        ProjectName = project.ProjectName,
        ProjectId = project.ProjectId,
        CharacterId = character.CharacterId,
        HasPlayerAccessToCharacter = hasAnyAccess,
        HasMasterAccess = hasMasterAccess,
        ParentGroups = character.Groups.Select(g => new CharacterGroupLinkViewModel(g)).ToList(),
      };
      if (hasAnyAccess)
      {
        viewModel.Plot = (await _plotRepository.GetPlotsForCharacter(character)).Select(p => PlotElementViewModel.FromPlotElement(p, hasMasterAccess));
      }
      return View("Details", viewModel);
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<ActionResult> Details(int projectId, int characterId, string characterName, MarkdownString description,
      FormCollection formCollection)
    {
      try
      {
        await ProjectService.SaveCharacterFields(projectId, characterId, CurrentUserId, characterName, description.Contents,
          GetCharacterFieldValuesFromPost(formCollection.ToDictionary()));
        return RedirectToAction("Details", new {projectId, characterId});
      }
      catch
      {
        return await Details(projectId, characterId);
      }
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> Edit(int projectId, int characterId)
    {
      var field = await ProjectRepository.GetCharacterAsync(projectId, characterId);
      return AsMaster(field) ?? View(new EditCharacterViewModel()
      {
        ProjectId = field.ProjectId,
        CharacterId = field.CharacterId,
        Data = CharacterGroupListViewModel.FromProjectAsMaster(field.Project),
        Description = field.Description,
        IsPublic = field.IsAcceptingClaims,
        ProjectName = field.Project.ProjectName,
        IsAcceptingClaims = field.IsAcceptingClaims,
        HidePlayerForCharacter = field.HidePlayerForCharacter,
        Name = field.CharacterName,
        ParentCharacterGroupIds = field.Groups.Select(pg => pg.CharacterGroupId).ToList(),
      });
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(EditCharacterViewModel viewModel)
    {
      var project1 = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
      var error = AsMaster(project1);
      if (error != null)
      {
        return error;
      }
      try
      {
        if (!ModelState.IsValid)
        {
          return View(viewModel);
        }
        await ProjectService.EditCharacter(
          CurrentUserId,
          viewModel.CharacterId,
          viewModel.ProjectId,
          viewModel.Name, viewModel.IsPublic, viewModel.ParentCharacterGroupIds, viewModel.IsAcceptingClaims,
          viewModel.Description.Contents, viewModel.HidePlayerForCharacter);

        return RedirectToAction("Details", new {viewModel.ProjectId, viewModel.CharacterId});
      }
      catch
      {
        return View(viewModel);
      }
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult> Create(int projectid, int charactergroupid)
    {
      var field = await ProjectRepository.LoadGroupAsync(projectid, charactergroupid);

      return AsMaster(field, pa => pa.CanChangeFields) ?? View(new AddCharacterViewModel()
      {
        Data = CharacterGroupListViewModel.FromProjectAsMaster(field.Project),
        ProjectId = projectid,
        ParentCharacterGroupIds = new List<int> {charactergroupid}
      });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(AddCharacterViewModel viewModel)
    {
      var project1 = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
      var error = AsMaster(project1);
      if (error != null)
      {
        return error;
      }
      try
      {
        await ProjectService.AddCharacter(
          viewModel.ProjectId,
          viewModel.Name, viewModel.IsPublic, viewModel.ParentCharacterGroupIds, viewModel.IsAcceptingClaims,
          viewModel.Description.Contents, viewModel.HidePlayerForCharacter);

        return RedirectToIndex(project1);
      }
      catch
      {
        return View(viewModel);
      }
    }

    [HttpGet,Authorize]
    public async Task<ActionResult> Delete(int projectid, int characterid)
    {
      var field = await ProjectRepository.GetCharacterAsync(projectid, characterid);
      return AsMaster(field) ?? View(field);
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<ActionResult> Delete(int projectId, int characterId, FormCollection form)
    {
      var field = await ProjectRepository.GetCharacterAsync(projectId, characterId);
      var error = AsMaster(field);
      if (error != null)
      {
        return error;
      }
      try
      {
        await ProjectService.DeleteCharacter(projectId, characterId);

        return RedirectToIndex(field.Project);
      }
      catch
      {
        return View(field);
      }
    }
  }
}