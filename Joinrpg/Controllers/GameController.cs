using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Models;
using Microsoft.AspNet.Identity;

namespace JoinRpg.Web.Controllers
{
    public class GameController : ControllerGameBase
  {
      public GameController(IProjectService projectService, ApplicationUserManager userManager, IProjectRepository projectRepository) : base (userManager, projectRepository, projectService)
      {
      }

      // GET: Game
        public ActionResult Index()
        {
            return View();
        }

        // GET: Game/Details/5
        public ActionResult Details(int projectId)
        {
            return View(ProjectRepository.GetProject(projectId));
        }

        // GET: Game/Create
        [Authorize]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Game/Create
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ProjectCreateViewModel model)
        {
            try
            {
             var project = ProjectService.AddProject(model.ProjectName, GetCurrentUser());

                return RedirectToAction("Details", new {id = project.ProjectId});
            }
            catch
            {
                return View();
            }
        }

      // GET: Game/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Game/Edit/5
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
   
    }
}
