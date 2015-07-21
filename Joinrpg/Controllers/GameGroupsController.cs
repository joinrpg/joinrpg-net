using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
  public class GameGroupsController : ControllerGameBase
  {

    private ActionResult InsideGroup(int projectId, int groupId, Func<Project, CharacterGroup, ActionResult> action)
    {
      return InsideProjectSubentity(projectId, groupId, project => project.CharacterGroups,
        subentity => subentity.CharacterGroupId, action);
    }

    [Authorize]
    // GET: GameGroups
    public ActionResult Index(int projectId, int? rootCharacterGroupId)
    {
      return InsideProject(projectId, project =>
      {
        if (rootCharacterGroupId == null)
        {
          return RedirectToIndex(project);
        }
        return InsideGroup(projectId, (int) rootCharacterGroupId,
          (project1, @group) => View(CharacterGroupListViewModel.FromGroup(@group)));

      });
    }

    private ActionResult RedirectToIndex(Project project)
    {
      return RedirectToAction("Index",
        new {project.ProjectId, rootCharacterGroupId = project.CharacterGroups.Single(cg => cg.IsRoot).CharacterGroupId});
    }

    // GET: GameGroups/Details/5
    public ActionResult Details(int id)
    {
      return View();
    }

    // GET: GameGroups/Create
    public ActionResult Create()
    {
      return View();
    }

    // POST: GameGroups/Create
    [HttpPost]
    public ActionResult Create(FormCollection collection)
    {
      try
      {
        // TODO: Add insert logic here

        return RedirectToAction("Index");
      }
      catch
      {
        return View();
      }
    }

    // GET: GameGroups/Edit/5
    public ActionResult Edit(int id)
    {
      return View();
    }

    // POST: GameGroups/Edit/5
    [HttpPost]
    public ActionResult Edit(int id, FormCollection collection)
    {
      try
      {
        // TODO: Add update logic here

        return RedirectToAction("Index");
      }
      catch
      {
        return View();
      }
    }

    // GET: GameGroups/Delete/5
    public ActionResult Delete(int id)
    {
      return View();
    }

    // POST: GameGroups/Delete/5
    [HttpPost]
    public ActionResult Delete(int id, FormCollection collection)
    {
      try
      {
        // TODO: Add delete logic here

        return RedirectToAction("Index");
      }
      catch
      {
        return View();
      }
    }

    public GameGroupsController(ApplicationUserManager userManager, IProjectRepository projectRepository, IProjectService projectService)
      : base(userManager, projectRepository, projectService)
    {
    }

    public ActionResult AddCharacter(int projectid, int charactergroupid)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [Authorize]
    public ActionResult AddGroup(int projectid, int charactergroupid)
    {
      return InsideGroup(projectid, charactergroupid,
        (project, @group) => View(new AddCharacterGroupViewModel()
        {
          Data = CharacterGroupListViewModel.FromGroup(project.RootGroup),
          ProjectId = projectid,
          ParentCharacterGroupIds = new List<int> {charactergroupid}
        }));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public ActionResult AddGroup(AddCharacterGroupViewModel viewModel)
    {
      return InsideProject(viewModel.ProjectId, project =>
      {
        try
        {
          ProjectService.AddCharacterGroup( 
            viewModel.ProjectId, viewModel.Name, viewModel.IsPublic,
            viewModel.ParentCharacterGroupIds);

          return RedirectToIndex(project);
        }
        catch
        {
          return View(viewModel);
        }
      });
    }


  }
}