using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.CommonTypes;
using JoinRpg.Web.Models.Plot;

namespace JoinRpg.Web.Controllers
{
  public class CharacterController : Common.ControllerGameBase
  {
    private readonly IPlotRepository _plotRepository;

    public CharacterController(ApplicationUserManager userManager, IProjectRepository projectRepository,
      IProjectService projectService, IPlotRepository plotRepository, IExportDataService exportDataService)
      : base(userManager, projectRepository, projectService, exportDataService)
    {
      _plotRepository = plotRepository;
    }

    [HttpGet]
    public async Task<ActionResult> Details(int projectid, int characterid)
    {
      var field = await ProjectRepository.GetCharacterWithGroups(projectid, characterid);
      return WithEntity(field) ?? await ShowCharacter(field);
    }

    private async Task<ActionResult> ShowCharacter(Character character)
    {
      var hasMasterAccess = character.HasMasterAccess(CurrentUserIdOrDefault);
      var approvedClaimUser = character.ApprovedClaim?.Player;
      var hasPlayerAccess = User.Identity.IsAuthenticated && approvedClaimUser?.UserId == CurrentUserId;
      var hasAnyAccess = hasMasterAccess || hasPlayerAccess;
      var viewModel = new CharacterDetailsViewModel
      {
        Description = new MarkdownViewModel(character.Description),
        Player = approvedClaimUser,
        HasAccess = hasAnyAccess,
        ParentGroups = CharacterParentGroupsViewModel.FromCharacter(character, hasMasterAccess),
        HidePlayer = character.HidePlayerForCharacter,
        Navigation = CharacterNavigationViewModel.FromCharacter(character, CharacterNavigationPage.Character, CurrentUserIdOrDefault),
        Fields = new CharacterFieldsViewModel()
        {
          CharacterFields = character.GetPresentFields(),
          HasMasterAccess = hasMasterAccess,
          EditAllowed = false,
          HasPlayerAccessToCharacter = hasPlayerAccess
        },
        Plot =
          hasAnyAccess
            ? character.GetOrderedPlots(await _plotRepository.GetPlotsForCharacter(character)).ToViewModels(hasMasterAccess)
            : Enumerable.Empty<PlotElementViewModel>()
      };
      return View("Details", viewModel);
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> Edit(int projectId, int characterId)
    {
      var field = await ProjectRepository.GetCharacterAsync(projectId, characterId);
      return AsMaster(field, s => s.CanEditRoles) ?? View(new EditCharacterViewModel()
      {
        ProjectId = field.ProjectId,
        CharacterId = field.CharacterId,
        Description = new MarkdownViewModel(field.Description),
        IsPublic = field.IsPublic,
        ProjectName = field.Project.ProjectName,
        IsAcceptingClaims = field.IsAcceptingClaims,
        HidePlayerForCharacter = field.HidePlayerForCharacter,
        Name = field.CharacterName,
        ParentCharacterGroupIds = field.Groups.Select(pg => pg.CharacterGroupId).ToList(),
        IsHot = field.IsHot,
      }.Fill(field, CurrentUserId));
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(EditCharacterViewModel viewModel)
    {
      var field = await ProjectRepository.GetCharacterAsync(viewModel.ProjectId, viewModel.CharacterId);
      var error = AsMaster(field, s => s.CanEditRoles);
      if (error != null)
      {
        return error;
      }
      try
      {
        if (!ModelState.IsValid)
        {
          return View(viewModel.Fill(field, CurrentUserId));
        }
        await ProjectService.EditCharacter(
          CurrentUserId,
          viewModel.CharacterId,
          viewModel.ProjectId,
          viewModel.Name, viewModel.IsPublic, viewModel.ParentCharacterGroupIds, viewModel.IsAcceptingClaims,
          viewModel.Description.Contents, viewModel.HidePlayerForCharacter, GetCharacterFieldValuesFromPost(), viewModel.IsHot);

        return RedirectToAction("Details", new {viewModel.ProjectId, viewModel.CharacterId});
      }
      catch (Exception exception)
      {
        ModelState.AddException(exception);
        return View(viewModel.Fill(field, CurrentUserId));
      }
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult> Create(int projectid, int charactergroupid)
    {
      var field = await ProjectRepository.LoadGroupAsync(projectid, charactergroupid);

      return AsMaster(field, pa => pa.CanEditRoles) ?? View(new AddCharacterViewModel()
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
          viewModel.Description.Contents, viewModel.HidePlayerForCharacter, viewModel.IsHot);

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
    // ReSharper disable once UnusedParameter.Global
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

    [HttpGet, Authorize]
    public Task<ActionResult> MoveUp(int projectid, int characterid, int parentcharactergroupid, int currentrootgroupid)
    {
      return MoveImpl(projectid, characterid, parentcharactergroupid, currentrootgroupid, -1);
    }

    [HttpGet, Authorize]
    public Task<ActionResult> MoveDown(int projectid, int characterid, int parentcharactergroupid, int currentrootgroupid)
    {
      return MoveImpl(projectid, characterid, parentcharactergroupid, currentrootgroupid, +1);
    }

    private async Task<ActionResult> MoveImpl(int projectId, int characterId, int parentCharacterGroupId, int currentRootGroupId, short direction)
    {
      var group = await ProjectRepository.GetCharacterAsync(projectId, characterId);
      var error = AsMaster(@group, acl => acl.CanEditRoles);
      if (error != null)
      {
        return error;
      }

      try
      {
        await ProjectService.MoveCharacter(CurrentUserId, projectId, characterId, parentCharacterGroupId, direction);


        return RedirectToIndex(projectId, currentRootGroupId);
      }
      catch
      {
        return RedirectToIndex(projectId, currentRootGroupId);
      }
    }
  }
}