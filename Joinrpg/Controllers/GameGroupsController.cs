using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
  public class GameGroupsController : ControllerGameBase
  {
    // GET: GameGroups
    public ActionResult Index(int projectId, int? characterGroupId)
    {
      return WithProject(projectId, (project, acl) =>
      {
        if (characterGroupId == null)
        {
          return RedirectToIndex(project);
        }
        
        return WithGroup(projectId, (int)characterGroupId,
          (project1, @group) => View(
            new GameRolesViewModel
            {
              ProjectId = project.ProjectId,
              ProjectName = project.ProjectName,
              ShowEditControls = acl!=null,
              Data = CharacterGroupListViewModel.FromGroup(group)
            }));

      });
    }

    // GET: GameGroups/Edit/5
    [HttpGet, Authorize]
    public ActionResult Edit(int projectId, int characterGroupId)
    {
      return WithGroupAsMaster(projectId, characterGroupId,
        (project, @group) => View(new EditCharacterGroupViewModel
        {
          Data = CharacterGroupListViewModel.FromGroupAsMaster(project.RootGroup),
          ProjectId = project.ProjectId,
          ParentCharacterGroupIds = @group.ParentGroups.Select(pg => pg.CharacterGroupId).ToList(),
          Description = @group.Description.Contents,
          IsPublic = @group.IsPublic,
          Name = @group.CharacterGroupName,
          HaveDirectSlots = GetDirectClaimSettings(group),
          DirectSlots = Math.Max(group.AvaiableDirectSlots, 0),
          CharacterGroupId = group.CharacterGroupId,
          IsRoot = group.IsRoot
        }));
    }

    private static DirectClaimSettings GetDirectClaimSettings(CharacterGroup group)

    {
      if (!@group.HaveDirectSlots)
        return DirectClaimSettings.NoDirectClaims;
      return @group.DirectSlotsUnlimited ? DirectClaimSettings.DirectClaimsUnlimited : DirectClaimSettings.DirectClaimsLimited;
    }

    // POST: GameGroups/Edit/5
    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<ActionResult> Edit(EditCharacterGroupViewModel viewModel)
    {
      CharacterGroup group = await ProjectRepository.LoadGroupAsync(viewModel.ProjectId, viewModel.CharacterGroupId);
      var error = AsMaster(group);
      if (error != null)
      {
        return error;
      }

      try
      {
        var haveDirectSlots = viewModel.HaveDirectSlots != DirectClaimSettings.NoDirectClaims;
        var directSlots = viewModel.HaveDirectSlots == DirectClaimSettings.DirectClaimsUnlimited
          ? -1
          : viewModel.DirectSlots;


        await ProjectService.EditCharacterGroup(
          group.ProjectId, @group.CharacterGroupId, viewModel.Name, viewModel.IsPublic,
          viewModel.ParentCharacterGroupIds, viewModel.Description, haveDirectSlots, directSlots);


        return RedirectToIndex(group.Project);
      }
      catch
      {
        return View(viewModel);
      }

    }

    // GET: GameGroups/Delete/5
    [HttpGet,Authorize]
    public ActionResult Delete(int projectId, int characterGroupId)
    {
      return WithGroupAsMaster(projectId, characterGroupId, (project, @group) => View(@group));
    }

    // POST: GameGroups/Delete/5
    [HttpPost,Authorize,ValidateAntiForgeryToken]
    public ActionResult Delete(int projectId, int characterGroupId, FormCollection collection)
    {

      return WithGroupAsMaster(projectId, characterGroupId, (project, @group) =>
      {
        try
        {
          ProjectService.DeleteCharacterGroup(
            projectId, characterGroupId);

          return RedirectToIndex(project);
        }
        catch
        {
          return View(group);
        }
      });
    }

    public GameGroupsController(ApplicationUserManager userManager, IProjectRepository projectRepository, IProjectService projectService)
      : base(userManager, projectRepository, projectService)
    {
    }

    [HttpGet]
    [Authorize]
    public ActionResult AddGroup(int projectid, int charactergroupid)
    {
      return WithGroupAsMaster(projectid, charactergroupid,
        (project, @group) => View(new AddCharacterGroupViewModel()
        {
          Data = CharacterGroupListViewModel.FromGroupAsMaster(project.RootGroup),
          ProjectId = projectid,
          ParentCharacterGroupIds = new List<int> {charactergroupid}
        }));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public ActionResult AddGroup(AddCharacterGroupViewModel viewModel)
    {
      var project1 = ProjectRepository.GetProject(viewModel.ProjectId);
      return AsMaster(project1, acl => true) ?? ((Func<Project, ActionResult>) (project =>
      {
        try
        {
          ProjectService.AddCharacterGroup( 
            viewModel.ProjectId, viewModel.Name, viewModel.IsPublic,
            viewModel.ParentCharacterGroupIds, viewModel.Description);

          return RedirectToIndex(project);
        }
        catch
        {
          return View(viewModel);
        }
      }))(project1);
    }
  }
}