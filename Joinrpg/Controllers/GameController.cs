using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;
using Microsoft.AspNet.Identity;

namespace JoinRpg.Web.Controllers
{
    public class GameController : ControllerBase
    {
      private readonly IProjectService _projectService;
      private readonly IProjectRepository _projectRepository;

      public GameController(IProjectService projectService, ApplicationUserManager userManager, IProjectRepository projectRepository) : base (userManager)
      {
        _projectService = projectService;
        _projectRepository = projectRepository;
      }

      // GET: Game
        public ActionResult Index()
        {
            return View();
        }

        // GET: Game/Details/5
        public ActionResult Details(int projectId)
        {
            return View(_projectRepository.GetProject(projectId));
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
             var project = _projectService.AddProject(model.ProjectName, GetCurrentUser());

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
