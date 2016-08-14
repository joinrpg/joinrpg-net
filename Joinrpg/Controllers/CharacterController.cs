using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.CommonTypes;
using JoinRpg.Web.Models.Plot;

namespace JoinRpg.Web.Controllers
{
  public class CharacterController : Common.ControllerGameBase
  {
    private IPlotRepository PlotRepository { get; }

    public CharacterController(ApplicationUserManager userManager, IProjectRepository projectRepository,
      IProjectService projectService, IPlotRepository plotRepository, IExportDataService exportDataService)
      : base(userManager, projectRepository, projectService, exportDataService)
    {
      PlotRepository = plotRepository;
    }

    [HttpGet]
    public async Task<ActionResult> Details(int projectid, int characterid)
    {
      var field = await ProjectRepository.GetCharacterWithGroups(projectid, characterid);
      return WithEntity(field) ?? await ShowCharacter(field);
    }

    private async Task<ActionResult> ShowCharacter(Character character)
    {
      var viewModel = new CharacterDetailsViewModel
      {
        Description = new MarkdownViewModel(character.Description),
        Player = character.ApprovedClaim?.Player,
        HasAccess = character.HasAnyAccess(CurrentUserIdOrDefault),
        ParentGroups = new CharacterParentGroupsViewModel(character, character.HasMasterAccess(CurrentUserIdOrDefault)),
        HidePlayer = character.HidePlayerForCharacter && !character.Project.IsPlotPublished(),
        Navigation = CharacterNavigationViewModel.FromCharacter(character, CharacterNavigationPage.Character, CurrentUserIdOrDefault),
        Fields = new CustomFieldsViewModel(CurrentUserIdOrDefault, character, disableEdit: true),
        Plot =
          character.HasPlotViewAccess(CurrentUserIdOrDefault)
            ? await ShowPlotsForCharacter(character)
            : Enumerable.Empty<PlotElementViewModel>()
      };
      return View("Details", viewModel);
    }

    private async Task<IEnumerable<PlotElementViewModel>> ShowPlotsForCharacter(Character character)
    {
      return character.GetOrderedPlots(await PlotRepository.GetPlotsForCharacter(character)).ToViewModels(CurrentUserIdOrDefault, character);
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> Edit(int projectId, int characterId)
    {
      var field = await ProjectRepository.GetCharacterWithDetails(projectId, characterId);
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
        ParentCharacterGroupIds = field.GetParentGroupsForEdit(),
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
          viewModel.Name, viewModel.IsPublic, viewModel.ParentCharacterGroupIds.GetUnprefixedGroups(), viewModel.IsAcceptingClaims,
          viewModel.Description.Contents, 
          viewModel.HidePlayerForCharacter,
          GetCustomFieldValuesFromPost(), 
          viewModel.IsHot);

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
      var field = await ProjectRepository.GetGroupAsync(projectid, charactergroupid);

      return AsMaster(field, pa => pa.CanEditRoles) ?? View(new AddCharacterViewModel()
      {
        ProjectId = projectid,
        ProjectName = field.Project.ProjectName,
        ParentCharacterGroupIds = field.AsPossibleParentForEdit()
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
          viewModel.ProjectId, CurrentUserId, viewModel.Name, viewModel.IsPublic, viewModel.ParentCharacterGroupIds.GetUnprefixedGroups(), viewModel.IsAcceptingClaims, viewModel.Description.Contents, viewModel.HidePlayerForCharacter, viewModel.IsHot);

        return RedirectToIndex(viewModel.ProjectId, viewModel.ParentCharacterGroupIds.GetUnprefixedGroups().First());
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
    public async Task<ActionResult> Delete(int projectId, int characterId, [UsedImplicitly] FormCollection form)
    {
      var field = await ProjectRepository.GetCharacterAsync(projectId, characterId);
      var error = AsMaster(field);
      if (error != null)
      {
        return error;
      }
      try
      {
        await ProjectService.DeleteCharacter(projectId, characterId, CurrentUserId);

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
        //TODO Show Error
        return RedirectToIndex(projectId, currentRootGroupId);
      }
    }
  }
}